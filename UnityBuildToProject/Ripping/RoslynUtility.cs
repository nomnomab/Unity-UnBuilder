using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Spectre.Console;

namespace Nomnom;

public static partial class RoslynUtility {
    private static readonly CSharpParseOptions ParseOptions = CSharpParseOptions.Default
        .WithKind(SourceCodeKind.Regular)
        .WithDocumentationMode(DocumentationMode.None);
        // .WithPreprocessorSymbols(null);
        // .WithLanguageVersion(LanguageVersion.CSharp9);
        
    private static readonly Regex RemovePreprocessDefinesRegex = GetRemovePreprocessDefinesRegex();
    
    public static async Task<RoslynDatabase> ExtractTypes(string projectPath) {
        RoslynDatabase? db = null;
        
        try {
            db = await RoslynDatabase.Parse(projectPath);
        } catch {
            Console.WriteLine("Encountered an error");
            throw;
        }
        AnsiConsole.MarkupLine($"Extracted types from project.");
        
        await Task.Delay(1000);
        
        if (db == null) {
            throw new Exception("Failed to parse type database");
        }
        
        return db;
    }
    
    public static async Task ParseTypesFromFile(string filePath, List<string> namespacePartsCache, List<string> types) {
        types.Clear();
        
        var token = new CancellationTokenSource();
        var task = Task.Run(() => {
            var val = ParseTypesFromFileInternal(filePath, namespacePartsCache, token.Token).Distinct();
            types.AddRange(val);
        });
        
        var stopwatch = Stopwatch.StartNew();
        while (!task.IsCompletedSuccessfully) {
            if (stopwatch.ElapsedMilliseconds > 5000) {
                Console.WriteLine("Task failed!");
                token.Cancel();
                break;
            }
            
            if (task.Exception != null) {
                throw task.Exception;
            }
            
            await Task.Yield();
        }
    }
    
    private static IEnumerable<string> ParseTypesFromFileInternal(string filePath, List<string> namespacePartsCache, CancellationToken token) {
        var extension = Path.GetExtension(filePath);
        if (extension != ".cs") {
            yield break;
        }
        
        using var reader = new StreamReader(filePath);
        var text         = reader.ReadToEnd();
        text             = RemovePreprocessDefinesRegex.Replace(text, string.Empty);
        // var sourceText   = SourceText.From(reader.BaseStream);
        var sourceText   = SourceText.From(text);
        var tree         = CSharpSyntaxTree.ParseText(sourceText, ParseOptions, path: filePath);
        var root         = tree.GetRoot();
        
        var types        = root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>();
        
        foreach (var type in types) {
            var name = GetFullyQualifiedName(type, namespacePartsCache);
            // Console.WriteLine($" - name: {name}");
            
            // only cares about Mono-esque types where it inherits
            // and matches the file name
            if (IsPartialType(type)) {
                // Console.WriteLine($" - partial");
                if (type.BaseList == null) continue;
                if (type.BaseList.Types.Count == 0) continue;
                
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var lastDot  = name.LastIndexOf('.');
                var typeName = lastDot == -1 ? name : name[(lastDot + 1)..];
                if (fileName != typeName) {
                    continue;
                }
            }
           
            yield return name;
        }
        
        // if (!types.Any()) {
        //      Console.WriteLine(" - no types found");
        // }
    }
    
    private static bool IsPartialType(BaseTypeDeclarationSyntax type) {
        return type.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }
    
    private static string GetFullyQualifiedName(BaseTypeDeclarationSyntax typeDecl, List<string> namespacePartsCache) {
        // Get the namespace
        var namespaceDecl = typeDecl.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
        var fullNamespace = namespaceDecl == null ? string.Empty : GetFullNamespace(namespaceDecl, namespacePartsCache);

        // Get the containing type (if this is a nested type)
        var containingTypeDecl = typeDecl.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>(a => a != typeDecl);
        var containingTypeName = containingTypeDecl != null ? containingTypeDecl.Identifier.Text : null;

        // Build the fully qualified name
        var typeName = typeDecl.Identifier.Text;
        var fullyQualifiedName = string.IsNullOrEmpty(fullNamespace) ? typeName : $"{fullNamespace}.{typeName}";

        if (!string.IsNullOrEmpty(containingTypeName)) {
            fullyQualifiedName = $"{fullNamespace}.{containingTypeName}.{typeName}";
        }

        return fullyQualifiedName;
    }
    
    private static string? GetFullNamespace(BaseNamespaceDeclarationSyntax namespaceDecl, List<string> namespacePartsCache) {
        if (namespaceDecl == null) {
            return null;
        }

        namespacePartsCache.Clear();
        var currentNamespaceDecl = namespaceDecl;

        while (currentNamespaceDecl != null) {
            namespacePartsCache.Insert(0, currentNamespaceDecl.Name.ToString());
            currentNamespaceDecl = currentNamespaceDecl.Parent as BaseNamespaceDeclarationSyntax;
        }

        return string.Join(".", namespacePartsCache);
    }

    [GeneratedRegex(@"^\s*#.*$", RegexOptions.Multiline)]
    private static partial Regex GetRemovePreprocessDefinesRegex();
}
