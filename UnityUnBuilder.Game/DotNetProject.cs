using System.Diagnostics;
using System.Xml.Linq;
using Spectre.Console;

internal static class DotNetProject {
    public static bool Exists(string projectRoot) {
        if (!Directory.Exists(projectRoot)) {
            return false;
        }
        
        var name = Path.GetFileNameWithoutExtension((projectRoot));
        var csprojPath = Path.Combine(projectRoot, $"{name}.csproj");
        if (!File.Exists(csprojPath)) {
            return false;
        }
        
        return true;
    }
    
    public static void New(string settingsProjectFolder, string root, string projectRoot) {
        var name = Path.GetFileNameWithoutExtension(projectRoot);
        if (!Directory.Exists(projectRoot)) {
            Directory.CreateDirectory(projectRoot);
        }
        
        var process = new Process() {
            StartInfo = new ProcessStartInfo() {
                FileName               = "dotnet",
                Arguments              = $"new classlib -n \"{name}\" -o \"{projectRoot}\"",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            }
        };
        
        process.Start();
        
        LogOutputs(process);
        
        AddTemplateFile(projectRoot);
        AddProjectReference(projectRoot, Path.Combine(root, "..", "UnityUnBuilder.Game.dll"));
        
        Directory.CreateDirectory(settingsProjectFolder);

        AnsiConsole.WriteLine($"Project created at '{projectRoot}'");
    }
    
    private static void AddProjectReference(string projectRoot, string otherProjectFile) {
        // Load and edit the .csproj
        var csprojPath = Path.Combine(projectRoot, Path.GetFileNameWithoutExtension(projectRoot) + ".csproj");
        var doc        = XDocument.Load(csprojPath);
        
        AnsiConsole.WriteLine($"projectRoot: {projectRoot}");
        AnsiConsole.WriteLine($"otherProjectFile: {otherProjectFile}");
        var relativePath = Path.GetRelativePath(projectRoot, otherProjectFile);

        var itemGroup = new XElement("ItemGroup");
        var reference = new XElement("Reference",
            new XAttribute("Include", Path.GetFileNameWithoutExtension(otherProjectFile)),
            new XElement("HintPath", relativePath.Replace('\\', '/')),
            new XElement("Private", "true")
        );
        itemGroup.Add(reference);
        
        // Append to the root <Project>
        doc.Root?.Add(itemGroup);
        
        var ignoreFolders = new string[] {
            @"exclude\**",
        };
        
        foreach (var folder in ignoreFolders) {
            itemGroup = new XElement("ItemGroup");
            
            reference = new XElement("Content", new XAttribute("Remove", folder));
            itemGroup.Add(reference);
            
            reference = new XElement("Compile", new XAttribute("Remove", folder));
            itemGroup.Add(reference);
            
            reference = new XElement("EmbeddedResource", new XAttribute("Remove", folder));
            itemGroup.Add(reference);
            
            reference = new XElement("None", new XAttribute("Remove", folder));
            itemGroup.Add(reference);
            
            // Append to the root <Project>
            doc.Root?.Add(itemGroup);
        }

        // Save the modified .csproj
        doc.Save(csprojPath);

        Console.WriteLine("Added reference to local DLL in project: " + csprojPath);
    }
    
    private static void AddTemplateFile(string projectRoot) {
        var text = @"
using Nomnom;

namespace $NAMESPACE$;

public static class GameSettingsProvider {
    public static GameSettings GetGameSettings() {
        var settings = GameSettings.Default;
        
        // set settings here!
        
        return settings;
    }
}
".Trim();
    
        var class1File = Path.Combine(projectRoot, "Class1.cs");
        if (File.Exists(class1File)) {
            File.Delete(class1File);
        }
        
        var gameFile = Path.Combine(projectRoot, "GameSettingsProvider.cs");
        if (File.Exists(gameFile)) {
            return;
        }
        
        var name = Path.GetFileNameWithoutExtension(projectRoot);
        text = text.Replace("$NAMESPACE$", name);
        
        File.WriteAllText(gameFile, text);
    }
    
    public static string Build(string projectRoot) {
        var output  = Path.Combine(projectRoot, "output");
        var process = new Process() {
            StartInfo = new ProcessStartInfo() {
                FileName               = "dotnet",
                Arguments              = $"build \"{projectRoot}\" -c Release -o \"{output}\"",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            }
        };
        
        process.Start();
        
        LogOutputs(process);

        AnsiConsole.WriteLine($"Project built at '{projectRoot}'");
        
        var name = Path.GetFileNameWithoutExtension(projectRoot);
        return Path.Combine(output, $"{name}.dll");
    }
    
    private static bool LogOutputs(Process process) {
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        
        if (!string.IsNullOrEmpty(stdout)) {
            AnsiConsole.WriteLine("Output:");
            AnsiConsole.WriteLine(stdout);
        }
        
        if (!string.IsNullOrWhiteSpace(stderr)) {
            AnsiConsole.WriteLine("Errors:");
            throw new Exception(stderr);
        }
        
        return true;
    }
}
