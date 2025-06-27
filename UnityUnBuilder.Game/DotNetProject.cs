using System.Diagnostics;
using System.Xml.Linq;
using Spectre.Console;

internal static class DotNetProject {
    public static void EnsureContents(string projectRoot) {
        CreateGitIgnore(projectRoot);
        AddTemplateFile(projectRoot);
    }
    
    public static bool Exists(string projectRoot) {
        if (!Directory.Exists(projectRoot)) {
            return false;
        }
        
        var name = Path.GetFileNameWithoutExtension((projectRoot));
        var csprojPath = Path.Combine(projectRoot, $"{name}.csproj");
        if (!File.Exists(csprojPath)) {
            return false;
        }
        
        EnsureContents(projectRoot);
        
        return true;
    }
    
    public static void New(string exeRoot, string projectRoot) {
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
        
        EnsureContents(projectRoot);
        AddProjectReference(projectRoot, Path.Combine(exeRoot, "..", "UnityUnBuilder.Game.dll"));

        AnsiConsole.WriteLine($"Project created at '{projectRoot}'");
    }
    
    private static void CreateGitIgnore(string projectRoot) {
        var gitIgnorePath = Path.Combine(projectRoot, ".gitignore");
        if (File.Exists(gitIgnorePath)) return;
        
        var text = @"
# User-specific files
*.suo
*.user
*.sln.docstates

# Build results
[Dd]ebug/
[Rr]elease/
x64/
[Bb]in/
[Oo]bj/

# NuGet Packages
*.nupkg
# The packages folder can be ignored because of Package Restore
**/packages/*
# except build/, which is used as an MSBuild target.
!**/packages/build/
# Uncomment if necessary however generally it will be regenerated when needed
#!**/packages/repositories.config

# MSTest test Results
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*

*_i.c
*_p.c
*.ilk
*.meta
*.obj
*.pch
*.pdb
*.pgc
*.pgd
*.rsp
*.sbr
*.tlb
*.tli
*.tlh
*.tmp
*.tmp_proj
*.log
*.vspscc
*.vssscc
.builds
*.pidb
*.scc

# Visual C++ cache files
ipch/
*.aps
*.ncb
*.opensdf
*.sdf
*.cachefile

# Visual Studio profiler
*.psess
*.vsp
*.vspx

# Guidance Automation Toolkit
*.gpState

# ReSharper is a .NET coding add-in
_ReSharper*/
*.[Rr]e[Ss]harper

# TeamCity is a build add-in
_TeamCity*

# DotCover is a Code Coverage Tool
*.dotCover

# NCrunch
*.ncrunch*
.*crunch*.local.xml

# Installshield output folder
[Ee]xpress/

# DocProject is a documentation generator add-in
DocProject/buildhelp/
DocProject/Help/*.HxT
DocProject/Help/*.HxC
DocProject/Help/*.hhc
DocProject/Help/*.hhk
DocProject/Help/*.hhp
DocProject/Help/Html2
DocProject/Help/html

# Click-Once directory
publish/

# Publish Web Output
*.Publish.xml

# Windows Azure Build Output
csx
*.build.csdef

# Windows Store app package directory
AppPackages/

# Others
*.Cache
ClientBin/
[Ss]tyle[Cc]op.*
~$*
*~
*.dbmdl
*.[Pp]ublish.xml
*.pfx
*.publishsettings
modulesbin/
tempbin/

# EPiServer Site file (VPP)
AppData/

# RIA/Silverlight projects
Generated_Code/

# Backup & report files from converting an old project file to a newer
# Visual Studio version. Backup files are not needed, because we have git ;-)
_UpgradeReport_Files/
Backup*/
UpgradeLog*.XML
UpgradeLog*.htm

# vim
*.txt~
*.swp
*.swo

# Temp files when opening LibreOffice on ubuntu
.~lock.*
 
# svn
.svn

# CVS - Source Control
**/CVS/

# Remainings from resolving conflicts in Source Control
*.orig

# SQL Server files
**/App_Data/*.mdf
**/App_Data/*.ldf
**/App_Data/*.sdf


#LightSwitch generated files
GeneratedArtifacts/
_Pvt_Extensions/
ModelManifest.xml

# =========================
# Windows detritus
# =========================

# Windows image file caches
Thumbs.db
ehthumbs.db

# Folder config file
Desktop.ini

# Recycle Bin used on file shares
$RECYCLE.BIN/

# OS generated files #
Icon?

# Mac desktop service store files
.DS_Store

# SASS Compiler cache
.sass-cache

# Visual Studio 2014 CTP
**/*.sln.ide

# Visual Studio temp something
.vs/

# dotnet stuff
project.lock.json

# VS 2015+
*.vc.vc.opendb
*.vc.db

# Rider
.idea/

# Visual Studio Code
.vscode/

# Output folder used by Webpack or other FE stuff
**/node_modules/*
**/wwwroot/*

# SpecFlow specific
*.feature.cs
*.feature.xlsx.*
*.Specs_*.html

# UWP Projects
AppPackages/
        
/exclude
/resources
/bin
/obj
";
    
        File.WriteAllText(gitIgnorePath, text);
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
            @"resources\**",
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
        var output  = Path.Combine(projectRoot, "exclude/output");
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
