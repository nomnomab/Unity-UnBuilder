using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;

namespace Nomnom;

public static class FixUnityNGO {
    private static readonly string[] _methodsToRemove = [
        "__getTypeName",
        "__initializeVariables",
        "InitializeRPCS_",
        "__rpc_handler_"
    ];
    
    private static readonly string[] _bannedAttributes = [
        "MonoPInvokeCallback"
    ];
    
    private readonly static string[] _ifConditions = [
        "__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost)",
        "__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost) || NetworkManager.Singleton == null",
        "__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost)",
        "__rpc_exec_stage != __RpcExecStage.Server || (!networkManager.IsServer && !networkManager.IsHost)",
        "__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost)",
        "__rpc_exec_stage == __RpcExecStage.Client && !networkManager.IsClient && networkManager.IsHost"
    ];
    
    public static void RevertGeneratedCode(ToolSettings settings) {
        var projectPath = settings.ExtractData.GetProjectPath();
        var scriptsPath = Path.Combine(projectPath, "Assets", "Scripts");
        
        var files       = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++) {
            var file = files[i];
            AnsiConsole.WriteLine($"[{i}/{files.Length}] Checking \"{Utility.ClampPathFolders(file)}\" for NGO code...");
            RevertFile(file);
        }
    }
    
    private static void RevertFile(string path) {
        if (!File.Exists(path)) return;
        
        // ignore invalid files
        var fileName = Path.GetFileNameWithoutExtension(path);
        if (fileName.StartsWith("UnitySourceGeneratedAssemblyMonoScriptTypes")) {
            return;
        }
        
        // start reverting
        var text         = File.ReadAllText(path);
        // AnsiConsole.WriteLine($"original text:\n{text}");
        
        var originalText = text;
        if (fileName == "NfgoClient") {
            text = FixNfgoClient(text);
        }
        
        var tree = FixScript(text);
        if (tree == null) {
            // bad tree parsing
            return;
        }
        
        text = tree.ToFullString();
        text = ContentReplacement(tree, text);
        
        if (text == originalText) {
            return;
        }
        
        var finalPath = path;
        if (false) {
            finalPath = Path.ChangeExtension(finalPath, ".copy.cs");
        }
        
        File.WriteAllText(finalPath, text);
    }
    
    private static string ContentReplacement(SyntaxNode root, string text) {
        // todo: remove this from the generic code
        // todo: support per-game replacements
        var startOfRoundClass = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(x => x.Identifier.Text == "StartOfRound");
        if (startOfRoundClass == null) {
            return text;
        }
        
        text = text.Replace(
            @"voiceChatModule.IsMuted = !IngamePlayerSettings.Instance.playerInput.actions.FindAction(""VoiceButton"").IsPressed() && !GameNetworkManager.Instance.localPlayerController.speakingToWalkieTalkie;",
            @"// voiceChatModule.IsMuted = !IngamePlayerSettings.Instance.playerInput.actions.FindAction(""VoiceButton"").IsPressed() && !GameNetworkManager.Instance.localPlayerController.speakingToWalkieTalkie;"
        );

        text = text.Replace(
            @"voiceChatModule.IsMuted = !IngamePlayerSettings.Instance.settings.micEnabled;",
            @"// voiceChatModule.IsMuted = !IngamePlayerSettings.Instance.settings.micEnabled;"
        );

        text = text.Replace(
            @"if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.isPlayerDead || voiceChatModule.IsMuted || !voiceChatModule.enabled || voiceChatModule == null)",
            @"if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.isPlayerDead || voiceChatModule == null)"
        );

        text = text.Replace(
            @"allPlayerScripts[i].gameObject.GetComponentInChildren<NfgoPlayer>().VoiceChatTrackingStart();",
            @"// allPlayerScripts[i].gameObject.GetComponentInChildren<NfgoPlayer>().VoiceChatTrackingStart();"
        );

        text = text.Replace(
            @"playerControllerB.gameObject.GetComponentInChildren<NfgoPlayer>().VoiceChatTrackingStart();",
            @"// playerControllerB.gameObject.GetComponentInChildren<NfgoPlayer>().VoiceChatTrackingStart();"
        );

        return text;
    }

    private static string FixNfgoClient(string text) {
        // go to line that is public NfgoClient([NotNull] NfgoCommsNetwork network)
        var index = text.IndexOf(
            "public NfgoClient([NotNull] NfgoCommsNetwork network)",
            StringComparison.Ordinal
        );
        
        // does it contain base(network)?
        var baseIndex = text.IndexOf("base(network)", index, StringComparison.Ordinal);
        if (baseIndex == -1) {
            baseIndex = text.IndexOf("base((ICommsNetworkState)network)", index, StringComparison.Ordinal);
        }
        
        if (baseIndex == -1) {
            // add base(network)
            var indexOfLastParenthesis = text.IndexOf(')', index) + 1;
            text = text.Insert(indexOfLastParenthesis, " : base(network)");
        }
        
        return text;
    }
    
    private static SyntaxNode? FixScript(string text) {
        var tree         = CSharpSyntaxTree.ParseText(text);
        var root         = tree.GetRoot();
        var originalRoot = root;

        root = Remove__rpcCalls(root);
        if (root == null) return null;
        // AnsiConsole.WriteLine($"Remove__rpcCalls:\n{root.ToFullString()}");
        
        root = FixMethods(root);
        if (root == null) return null;
        // AnsiConsole.WriteLine($"FixMethods:\n{root.ToFullString()}");
        
        // tweaks
        var rewriter = new RemoveCtorMethodCalls();
        root         = rewriter.Visit(root);
        if (root == null) return null;
        // AnsiConsole.WriteLine($"RemoveCtorMethodCalls:\n{root.ToFullString()}");
        
        // todo: why is this needed?
        // var structs = root.DescendantNodes()
        //     .OfType<StructDeclarationSyntax>()
        //     .ToArray();

        // while (structs.Any(x => x.DescendantNodes().OfType<ConstructorDeclarationSyntax>().ToList().Count > 0)) {
        //     foreach (var structDeclaration in structs) {
        //         var constructors = structDeclaration.DescendantNodes()
        //             .OfType<ConstructorDeclarationSyntax>()
        //             .ToArray();

        //         var currentStruct = structDeclaration;
        //         foreach (var constructor in constructors) {
        //             var newStruct = currentStruct.RemoveNode(constructor, SyntaxRemoveOptions.KeepNoTrivia);
        //             if (newStruct == null) continue;
                    
        //             root = root.ReplaceNode(currentStruct, newStruct);

        //             currentStruct = newStruct;
        //         }
        //     }

        //     structs = [.. root.DescendantNodes().OfType<StructDeclarationSyntax>()];
        // }
        
        if (originalRoot == root) {
            return null;
        }
        
        return root;
    }
    
    private static SyntaxNode? FixMethods(SyntaxNode root) {
        // fix methods
        var methods = root.DescendantNodes()
            .OfType<MemberDeclarationSyntax>()
            .ToArray();
        var methodsToReplace = new List<(MethodDeclarationSyntax, MethodDeclarationSyntax)>();
        var nodesToRemove = new List<SyntaxNode>();
        
        foreach (var method in methods) {
            if (method is not MethodDeclarationSyntax methodDeclaration) {
                continue;
            }
            
            var attributes = methodDeclaration.AttributeLists
                .SelectMany(x => x.Attributes)
                .ToArray();

            // remove any nodes that have a banned attribute
            if (attributes.Any(x => _bannedAttributes.Contains(x.Name.ToString()))) {
                nodesToRemove.AddRange(attributes);
                nodesToRemove.Add(methodDeclaration);
                continue;
            }
            
            var serverRpcAttribute = attributes
                .FirstOrDefault(x => x.Name.ToString() == "ServerRpc");
            var clientRpcAttribute = attributes
                .FirstOrDefault(x => x.Name.ToString() == "ClientRpc");

            MethodDeclarationSyntax? newMethod = null;
            if (serverRpcAttribute != null) {
                newMethod = ModifyRpcFunction(methodDeclaration);
            } else if (clientRpcAttribute != null) {
                newMethod = ModifyRpcFunction(methodDeclaration);
            } else {
                continue;
            }

            if (newMethod != null) {
                // replace old method with new method
                methodsToReplace.Add((methodDeclaration, newMethod));
            }
        }
        
        if (methodsToReplace.Count == 0) {
            return root;
        }
        
        // replace old methods with new methods
        var newRoot = root;
        newRoot = newRoot.ReplaceNodes(
            methodsToReplace.Select(x => x.Item1), 
            (x, y) => methodsToReplace.First(z => z.Item1 == x).Item2
        );
        newRoot = newRoot.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
        
        return newRoot;
    }
    
    /// <summary>
    /// Removes specific RPC calls, as these are generally generated.
    /// </summary>
    private static SyntaxNode? Remove__rpcCalls(SyntaxNode root) {
        var methodsToRemove = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => _methodsToRemove.Any(x => m.Identifier.Text.StartsWith(x)));
        var newRoot = root.RemoveNodes(methodsToRemove, SyntaxRemoveOptions.KeepNoTrivia);
        
        return newRoot;
    }
    
    /// <summary>
    /// Modifies an RPC function definition to remove generated code.
    /// </summary>
    private static MethodDeclarationSyntax? ModifyRpcFunction(MethodDeclarationSyntax methodDeclaration) {
        var statements = methodDeclaration.Body?.Statements ?? [];
        
        // un-generate various rpc function definitions :/
        var newMethod = methodDeclaration;
        if (statements.Count == 2) {
            newMethod = ModifyRpcFunctionTwoStatements(methodDeclaration, statements);
        } else {
            newMethod = ModifyRpcFunctionManyStatements(methodDeclaration, statements);
        }
        
        return newMethod;
    }
    
    private static MethodDeclarationSyntax? ModifyRpcFunctionTwoStatements(MethodDeclarationSyntax methodDeclaration, SyntaxList<StatementSyntax> statements) {
        /*
        * Example:
        * 		NetworkManager networkManager = base.NetworkManager;
        *      if ((object)networkManager != null && networkManager.IsListening)
        *      {
        */
        
        var secondStatement = statements[1];
        if (secondStatement is not IfStatementSyntax secondStatementIf) {
            return null;
        }

        var childNodes = secondStatementIf.ChildNodes().ToArray();
        if (childNodes.Length > 1) {
            var secondChildNode = childNodes[1];
            childNodes = [.. secondChildNode.ChildNodes()];

            var nestedNode = childNodes.Length == 1 ? childNodes[0] : childNodes[1];
            if (nestedNode is not IfStatementSyntax nestedNodeIf) {
                return null;
            }
        
            // strip this if statement of the prefix info and keep the rest
            var strippedIfStatement = RemoveIfStatement(nestedNodeIf);
            if (strippedIfStatement != null) {
                var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                    .WithModifiers(methodDeclaration.Modifiers)
                    .WithParameterList(methodDeclaration.ParameterList)
                    .WithAttributeLists(methodDeclaration.AttributeLists)
                    .WithBody(SyntaxFactory.Block(strippedIfStatement));
            
                return newMethod;
            } else {
                var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                    .WithModifiers(methodDeclaration.Modifiers)
                    .WithParameterList(methodDeclaration.ParameterList)
                    .WithAttributeLists(methodDeclaration.AttributeLists)
                    .WithBody(nestedNodeIf.Statement as BlockSyntax);
            
                return newMethod;
            }
        } else {
            var thirdStatement = statements[3];
            if (thirdStatement is not IfStatementSyntax thirdStatementIf) {
                return null;
            }
        
            // strip this if statement of the prefix info and keep the rest
            var strippedIfStatement = RemoveIfStatement(thirdStatementIf);
        }
        
        return methodDeclaration;
    }
    
    private static MethodDeclarationSyntax? ModifyRpcFunctionManyStatements(MethodDeclarationSyntax methodDeclaration, SyntaxList<StatementSyntax> statements) {
        /*
        * Example:
        * 		NetworkManager networkManager = base.NetworkManager;
        *      if ((object)networkManager == null || !networkManager.IsListening)
        *      {
        *          return;
        *      }
        *      if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
        *      {
        *          ClientRpcParams clientRpcParams = default(ClientRpcParams);
        *          FastBufferWriter bufferWriter = __beginSendClientRpc(848048148u, clientRpcParams, RpcDelivery.Reliable);
        *          bufferWriter.WriteValueSafe(in setBool, default(FastBufferWriter.ForPrimitives));
        *          bufferWriter.WriteValueSafe(in playSecondaryAudios, default(FastBufferWriter.ForPrimitives));
        *          BytePacker.WriteValueBitPacked(bufferWriter, playerWhoTriggered);
        *          __endSendClientRpc(ref bufferWriter, 848048148u, clientRpcParams, RpcDelivery.Reliable);
        *      }
        *      if (__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost) || GameNetworkManager.Instance.localPlayerController == null || (playerWhoTriggered != -1 && (int)GameNetworkManager.Instance.localPlayerController.playerClientId == playerWhoTriggered))
        */
        
        var fourthStatement = statements[3];
        if (fourthStatement is not IfStatementSyntax fourthStatementIf) {
            return null;
        }
        
        var strippedIfStatement = RemoveIfStatement(fourthStatementIf);
        if (strippedIfStatement != null) {
            var remainingStatements = statements.Skip(4);
            var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                .WithModifiers(methodDeclaration.Modifiers)
                .WithParameterList(methodDeclaration.ParameterList)
                .WithAttributeLists(methodDeclaration.AttributeLists)
                .WithBody(SyntaxFactory.Block(SyntaxFactory.List(remainingStatements.Prepend(strippedIfStatement))));
            
            return newMethod;
        } else {
            var remainingStatements = statements.Skip(4)
                .ToArray();
            
            // if is empty
            if (fourthStatementIf.Statement.ChildNodes().FirstOrDefault() is ReturnStatementSyntax) {
                var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                    .WithModifiers(methodDeclaration.Modifiers)
                    .WithParameterList(methodDeclaration.ParameterList)
                    .WithAttributeLists(methodDeclaration.AttributeLists)
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.List(remainingStatements)));

                return newMethod;
            } else {
                var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                    .WithModifiers(methodDeclaration.Modifiers)
                    .WithParameterList(methodDeclaration.ParameterList)
                    .WithAttributeLists(methodDeclaration.AttributeLists)
                    .WithBody(SyntaxFactory.Block(SyntaxFactory.List(remainingStatements).Prepend(fourthStatementIf.Statement)));

                return newMethod;
            }
        }
    }
    
    /// <summary>
    /// Removes an if statement from a method body.
    /// </summary>
    private static IfStatementSyntax? RemoveIfStatement(IfStatementSyntax ifStatementSyntax) {
        var conditionString = ifStatementSyntax.Condition.ToString();
        foreach (var c in _ifConditions) {
            conditionString = conditionString.Replace(c, string.Empty).TrimStart();
        }

        if (string.IsNullOrEmpty(conditionString)) {
            // empty if statement
            return null;
        } else if (conditionString.StartsWith("||") || conditionString.StartsWith("&&")) {
            conditionString = conditionString[2..].TrimStart();
            ifStatementSyntax = SyntaxFactory.IfStatement(
                SyntaxFactory.ParseExpression(conditionString),
                ifStatementSyntax.Statement
            );
        }
        
        return ifStatementSyntax;
    }
}

class RemoveCtorMethodCalls : CSharpSyntaxRewriter {
    public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node) {
        // if (node.Expression is InvocationExpressionSyntax invocation) {
        //     AnsiConsole.WriteLine($"invocation: {invocation.Expression}");
            
        //     if (invocation.Expression.ToString().Contains("ctor")) {
        //         AnsiConsole.WriteLine(" - contains \"ctor\"");
        //         // if the expression is a method call containing "ctor", remove it.
        //         return null;
        //     }
        // }
        
        // otherwise, keep the original node.
        // AnsiConsole.WriteLine(" - keeping");
        return base.VisitExpressionStatement(node);
    }
}
