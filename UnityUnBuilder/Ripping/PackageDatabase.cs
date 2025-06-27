namespace Nomnom;

public static class PackageDatabase {
    public static readonly List<PackageInfo> Packages = new Dictionary<string, string[]>() {
        ["com.unity.modules.ui"] = [
            "UnityEngine.UI",
            "UnityEngine.UIModule",
            "UnityEngine.TextCoreModule",
            "UnityEngine.TextRenderingModule"
        ],
        ["com.unity.ugui"] = [
            "UnityEngine.UI",
            "UnityEngine.UIModule",
            "UnityEngine.TextCoreModule",
            "UnityEngine.TextRenderingModule"
        ],
        ["com.unity.modules.uielements"] = [
            "UnityEngine.UIElementsModule"
        ],
        ["com.unity.probuilder"] = [
            "Unity.ProBuilder",
            "Unity.ProBuilder.KdTree",
            "Unity.ProBuilder.Poly2Tri",
            "Unity.ProBuilder.Stl",
            "Unity.ProBuilder.Csg"
        ],
        ["com.unity.textmeshpro"] = [
            "Unity.TextMeshPro"
        ],
        ["com.unity.timeline"] = [
            "Unity.Timeline"
        ],
        ["com.unity.vr"] = [
            "UnityEngine.VRModule"
        ],
        ["com.unity.xr"] = [
            "UnityEngine.XRModule"
        ],
        ["com.unity.wind"] = [
            "UnityEngine.WindModule"
        ],
        ["com.unity.modules.terrain"] = [
            "UnityEngine.TerrainModule"
        ],
        ["com.unity.modules.terrainphysics"] = [
            "UnityEngine.TerrainPhysicsModule"
        ],
        ["com.unity.modules.physics"] = [
            "UnityEngine.PhysicsModule"
        ],
        ["com.unity.modules.physics2d"] = [
            "UnityEngine.Physics2DModule"
        ],
        ["com.unity.modules.imageconversion"] = [
            "UnityEngine.ImageConversionModule"
        ],
        ["com.unity.modules.unitywebrequest"] = [
            "UnityEngine.UnityWebRequestModule"
        ],
        ["com.unity.modules.unitywebrequestwww"] = [
            "UnityEngine.UnityWebRequestWWWModule"
        ],
        ["com.unity.modules.unitywebrequesttexture"] = [
            "UnityEngine.UnityWebRequestTextureModule"
        ],
        ["com.unity.modules.unitywebrequestassetbundle"] = [
            "UnityEngine.UnityWebRequestAssetBundleModule"
        ],
        ["com.unity.modules.unitywebrequestaudio"] = [
            "UnityEngine.UnityWebRequestAudioModule"
        ],
        ["com.unity.modules.umbra"] = [
            "UnityEngine.UmbraModule"
        ],
        ["com.unity.modules.screencapture"] = [
            "UnityEngine.ScreenCaptureModule"
        ],
        ["com.unity.inputsystem"] = [
            "Unity.InputSystem",
            "Unity.InputSystem.ForUI"
        ],
        ["com.unity.progrids"] = [
            "Unity.ProGrids"
        ],
        ["com.unity.modules.cloth"] = [
            "UnityEngine.ClothModule"
        ],
        ["com.unity.modules.ai"] = [
            "UnityEngine.AIModule"
        ],
        ["com.unity.modules.animation"] = [
            "UnityEngine.AnimationModule"
        ],
        ["com.unity.animation.rigging"] = [
            "Unity.Animation.Rigging",
            "Unity.Animation.Rigging.DocCodeExamples"
        ],
        ["com.unity.modules.video"] = [
            "UnityEngine.VideoModule"
        ],
        ["com.unity.modules.audio"] = [
            "UnityEngine.AudioModule"
        ],
        ["com.unity.xr.arcore"] = [
            "UnityEngine.ARModule"
        ],
        ["com.unity.postprocessing"] = [
            "Unity.Postprocessing.Runtime"
        ],
        ["com.unity.modules.assetbundle"] = [
            "UnityEngine.AssetBundleModule"
        ],
        ["com.unity.modules.director"] = [
            "UnityEngine.DirectorModule"
        ],
        ["com.unity.modules.particlesystem"] = [
            "UnityEngine.ParticleSystemModule"
        ],
        ["com.unity.2d.sprite"] = [
            "UnityEngine.SpriteMaskModule"
        ],
        ["com.unity.2d.spriteshape"] = [
            "UnityEngine.SpriteShapeModule"
        ],
        ["com.unity.2d.tilemap"] = [
            "UnityEngine.TilemapModule"
        ],
        ["com.unity.modules.vehicles"] = [
            "UnityEngine.VehiclesModule"
        ],
        ["com.unity.visualeffectgraph"] = [
            "UnityEngine.VFXModule"
        ],
        ["com.unity.modules.jsonserialize"] = [
            "UnityEngine.JSONSerializeModule"
        ],
        ["com.unity.modules.imgui"] = [
            "UnityEngine.IMGUIModule"
        ],
        ["com.unity.multiplayer-hlapi"] = [
            "com.unity.multiplayer-hlapi.Editor",
            "com.unity.multiplayer-hlapi.Runtime",
            "com.unity.multiplayer-weaver.Editor"
        ],
        ["com.unity.render-pipelines.core"] = [
            "Unity.RenderPipelines.Core.Runtime",
            "Unity.RenderPipelines.Core.ShaderLibrary"
        ],
        ["com.unity.render-pipelines.universal"] = [
            "Unity.RenderPipelines.Universal.Config.Runtime",
            "Unity.RenderPipelines.Universal.Runtime",
            "Unity.RenderPipelines.Universal.Shaders",
            "Unity.RenderPipeline.Universal.ShaderLibrary"
        ],
        ["com.unity.render-pipelines.high-definition"] = [
            "Unity.RenderPipelines.HighDefinition.Config.Runtime",
            "Unity.RenderPipelines.HighDefinition.Runtime"
        ],
        ["com.unity.shadergraph"] = [
            "Unity.RenderPipelines.ShaderGraph.ShaderGraphLibrary"
        ],
        ["com.unity.burst"] = [
            "Unity.Burst",
            "Unity.Burst.Unsafe"
        ],
        ["com.unity.mathematics"] = [
            "Unity.Mathematics"
        ],
        ["com.unity.cinemachine"] = [
            "Cinemachine"
        ],
        ["com.unity.visualscripting"] = [
            "Unity.VisualScripting.Core",
            "Unity.VisualScripting.Flow",
            "Unity.VisualScripting.State",
            "Unity.VisualScripting.Antlr3.Runtime"
        ],
        ["com.unity.recorder"] = [
            "Unity.Recorder",
            "Unity.Recorder.Base"
        ],
        ["com.unity.localization"] = [
            "Unity.Localization"
        ],
        ["com.unity.nuget.newtonsoft-json"] = [
            "Newtonsoft.Json"
        ],
        ["com.unity.addressables"] = [
            "Unity.Addressables"
        ],
        ["com.autodesk.fbx"] = [
            "Autodesk.Fbx"
        ],
        ["com.unity.formats.fbx"] = [
            "Unity.Formats.Fbx.Runtime"
        ],
        ["com.unity.ai.navigation"] = [
            "Unity.AI.Navigation"
        ],
        ["com.unity.scriptablebuildpipeline"] = [
            "Unity.ScriptableBuildPipeline"
        ],
        ["com.unity.netcode.gameobjects"] = [
            "Unity.Netcode.Runtime",
            "Unity.Networking.Transport"
        ],
        ["com.unity.2d.animation"] = [
            "Unity.2D.Animation.Runtime",
            "Unity.2D.IK.Runtime"
        ],
        ["com.unity.2d.common"] = [
            "Unity.2D.Common.Runtime"
        ],
        ["com.unity.2d.pixel-perfect"] = [
            "Unity.2D.PixelPerfect"
        ],
        ["com.unity.2d.tilemap.extras"] = [
            "Unity.2D.Tilemap.Extras"
        ],
    }
    .OrderBy(x => x.Key)
    .Select(x => new PackageInfo() {
        Id          = x.Key,
        DllNames    = x.Value,
        ForVersions = []
    })
    .ToList();
    
    /// <summary>
    /// Ignores these dll names from game assembly fetching and package resolution.
    /// </summary>
    public static readonly string[] IgnoreDlls = [
        "mscorlib",
        "netstandard",
        "UnityEngine",
        "UnityEngine.AccessibilityModule",
        "UnityEngine.AndroidJNIModule",
        "UnityEngine.CoreModule",
        "UnityEngine.PerformanceReportingModule",
        "UnityEngine.InputLegacyModule",
        "UnityEngine.ProfilerModule",
        "UnityEngine.UnityAnalyticsModule",
        "UnityEngine.UnityConnectModule",
        "UnityEngine.UnityTestProtocolModule",
        "UnityEngine.SharedInternalsModule",
        "UnityEngine.HotReloadModule",
        "Unity.ResourceManager",
        "Unity.Rider.Editor",
        "Boo.Lang",
        "UnityScript.Lang",
        "dfScriptLite",
        "UnityEngine.NVIDIAModule"
    ];

    /// <summary>
    /// Ignores dlls with these prefixes from game assembly fetching.
    /// </summary>
    public static readonly string[] IgnoreDllPrefixes = [
        "System",
        "Assembly-CSharp",
        "Unity.InternalAPIEngineBridge",
        "FacePunch",
        "Unity.Services.",
    ];
    
    /// <summary>
    /// Packages that are not considered during package resolution.
    /// </summary>
    public static readonly string[] IgnorePackages = [
        // "com.unity.test-framework.performance",
        // "com.unity.test-framework",
        "com.unity.collab-proxy",
        "com.unity.ide.rider",
        "com.unity.ide.visualstudio",
        "com.unity.ide.vscode"
    ];
    
    /// <summary>
    /// Excludes script folders, that start with these prefixes, from being copied into the final project.
    /// </summary>
    public static readonly string[] IgnoreAssemblyPrefixes = [
        "System",
        "Unity.InternalAPIEngineBridge",
        "FacePunch",
        "Mono.",
        "UnityEngine.",
        "Unity.",
        "Newtonsoft.Json",
        "FishNet."
    ];
    
    /// <summary>
    /// Script folders with these names will be obliterated from the project.
    /// </summary>
    public static readonly string[] ExcludeAssemblyFromProject = [
        "Unity.InternalAPIEngineBridge",
        "Unity.ResourceManager",
    ];
    
    /// <summary>
    /// Excludes these script folders from being copied into the final project.
    /// </summary>
    public static readonly string[] ExcludeAssembliesFromProject = [
        .. IgnoreDlls,
        "ClientNetworkTransform",
        "Pathfinding.Ionic.Zip.Reduced",
        "AstarPathfindingProject"
    ];
}
