using System.Text.Json;

namespace Nomnom;

public class UnityBuildToProject_PackagesParsing {
    private static readonly string PackagesJson = @"
{
    ""Packages"": [
        {
            ""m_PackageId"": ""com.havok.physics@0.1.1-preview"",
            ""m_IsDirectDependency"": false,
            ""m_Version"": ""0.1.1-preview"",
            ""m_Source"": 1,
            ""m_ResolvedPath"": """",
            ""m_AssetPath"": ""Packages/com.havok.physics"",
            ""m_Name"": ""com.havok.physics"",
            ""m_DisplayName"": ""Havok Physics"",
            ""m_Category"": ""Havok"",
            ""m_Type"": """",
            ""m_Description"": ""Havok's award winning physics engine, applied to the DOTS framework. This augments the Unity.Physics package, allowing physics scenes authored for DOTS to be simulated using the Havok Physics engine.\n\nPLEASE NOTE: This is a trial version, which expires on January 15th 2020.\n\nAfter the trial period, the native plugins will no longer function in the editor. If you want to continue using Havok Physics after the trial period, you will have to upgrade to a newer version of this package when it becomes available.\n\nHavok Physics will continue to be free to use for Unity Personal and Unity Plus users after the trial period has ended. Unity Pro users will have to purchase a Havok subscription from the Unity Asset Store. For more details on licensing, please see the package documentation."",
            ""m_Status"": 1,
            ""m_Errors"": [],
            ""m_Versions"": {
                ""m_All"": [
                    ""0.1.1-preview"",
                    ""0.1.2-preview"",
                    ""0.2.0-preview"",
                    ""0.2.1-preview"",
                    ""0.2.2-preview"",
                    ""0.3.0-preview.1"",
                    ""0.3.1-preview"",
                    ""0.4.0-preview.1"",
                    ""0.4.1-preview.2"",
                    ""0.6.0-preview.3"",
                    ""0.50.0-preview.24"",
                    ""0.50.0-preview.37"",
                    ""0.51.0-preview.32"",
                    ""0.51.1-preview.21"",
                    ""1.0.0-pre.15"",
                    ""1.0.0-pre.44"",
                    ""1.0.0-pre.65"",
                    ""1.0.8"",
                    ""1.0.10"",
                    ""1.0.11"",
                    ""1.0.14"",
                    ""1.0.16"",
                    ""1.1.0-exp.1"",
                    ""1.1.0-pre.3"",
                    ""1.2.0-exp.3"",
                    ""1.2.0-pre.4"",
                    ""1.2.0-pre.6"",
                    ""1.2.0-pre.12"",
                    ""1.2.0"",
                    ""1.2.1"",
                    ""1.2.3"",
                    ""1.2.4"",
                    ""1.3.0-exp.1"",
                    ""1.3.0-pre.4"",
                    ""1.3.2"",
                    ""1.3.5"",
                    ""1.3.8"",
                    ""1.3.9""
                ],
                ""m_Compatible"": [
                    ""0.1.1-preview""
                ],
                ""m_Recommended"": """"
            },
            ""m_Dependencies"": [
                {
                    ""m_Name"": ""com.unity.physics"",
                    ""m_Version"": ""0.2.4-preview""
                }
            ],
            ""m_ResolvedDependencies"": [],
            ""m_Keywords"": [
                ""havok"",
                ""physics""
            ],
            ""m_Author"": {
                ""m_Name"": ""Microsoft"",
                ""m_Email"": """",
                ""m_Url"": """"
            },
            ""m_HasRegistry"": true,
            ""m_Registry"": {
                ""m_Name"": """",
                ""m_Url"": ""https://packages.unity.com"",
                ""m_IsDefault"": true
            },
            ""m_HideInEditor"": true,
            ""m_DatePublishedTicks"": 637046064140000000
        }
    ]
}
";
    
    [Fact]
    public void PackagesFile_String_DoesParse() {
        var packageOutput = JsonSerializer.Deserialize<UnityPackages>(PackagesJson);
        Assert.NotNull(packageOutput);
        Assert.NotNull(packageOutput.Packages);
        Assert.Single (packageOutput.Packages);
        Assert.Equal  ("com.havok.physics@0.1.1-preview", packageOutput.Packages[0].m_PackageId);
    }
    
    [Fact]
    public void PackagesFile_Output_DoesParse() {
        var path          = Path.Combine("..", "..", "..", "packages_output.json");
        var contents      = File.ReadAllText(path);
        var packageOutput = JsonSerializer.Deserialize<UnityPackages>(contents);
        Assert.NotNull(packageOutput);
        Assert.NotNull(packageOutput.Packages);
        Assert.Equal  (140, packageOutput.Packages.Length);
        Assert.Equal  ("com.havok.physics@0.1.1-preview", packageOutput.Packages[0].m_PackageId);
    }
}
