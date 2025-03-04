namespace Nomnom;

/// <summary>
/// Simply stores a guid.
/// </summary>
public record MetaFile {
    public required string FilePath;
    public required UnityGuid Guid;
}

/// <summary>
/// Can store many guid references, as well as file id references.
/// </summary>
public record AssetFile {
    /// <summary>
    /// The absolute file path to the asset.
    /// </summary>
    public required string FilePath;
    public required HashSet<UnityObjectDefinition> Objects;
    
    public static AssetFile Default => new() {
        FilePath = "",
        Objects  = [],
    };
}

public record ShaderFile {
    /// <summary>
    /// The absolute file path to the asset.
    /// </summary>
    public required string FilePath;
    
    /// <summary>
    /// The lookup path name. Is basically a path.
    /// </summary>
    public required string Name;
}

/// <summary>
/// An id to an asset.
/// </summary>
public record UnityGuid(string Value) {
    public override string ToString() {
        return Value;
    }
}

/// <summary>
/// This part defines the ID for the object itself, which is used to reference objects between each other.
/// It’s called File ID because it represents the ID of the object in a specific file.
/// </summary>
public record UnityFileId(string Value) {
    public override string ToString() {
        return Value;
    }
}

/// <summary>
/// Type is used to determine whether the file should be loaded from the Assets folder or the Library folder.
/// Note that it only supports the following values, starting at 2 (given that 0 and 1 are deprecated).
/// <br/><br/>
/// Another factor to highlight regarding script serialization is that the YAML Type is the same for every script; just MonoBehaviour.
/// The actual script is referenced in the “m_Script” property, using the GUID of the script’s meta file.
/// </summary>
public record UnityFileType(int Value) {
    /// <summary>
    /// Assets that can be loaded directly from the Assets folder by the Editor,
    /// like Materials and .asset files.
    /// </summary>
    /// <returns></returns>
    public bool IsType2() => Value == 2;
    
    /// <summary>
    /// Assets that have been processed and written in the Library folder, and
    /// loaded from there by the Editor, like Prefabs, textures, and 3D models.
    /// </summary>
    /// <returns></returns>
    public bool IsType3() => Value == 3;
    
    public override string ToString() {
        if (IsType2()) return "Type2";
        if (IsType3()) return "Type3";
        return Value.ToString();
    }
}

/// <summary>
/// A reference to another asset.
/// <br/><br/>
/// In the format of: <code>{fileID: [fileId], guid: [guid], type: [type]}</code>
/// </summary>
public record UnityAssetReference {
    public required UnityFileId FileId;
    public required UnityGuid Guid;
    public required UnityFileType Type;

    public override string ToString() {
        return $"{FileId}:{Guid}:{Type}";
    }
}

/// <summary>
/// An object stored in an asset file.
/// <br/><br/>
/// Starts with a format such as: <code>--- !u![classId] &amp;[fileId]</code>
/// </summary>
public record UnityObjectDefinition {
    public required UnityClassId ClassId;
    public required UnityFileId FileId;
    
    public required List<UnityAssetReference> AssetReferences;
    public required List<UnityFileId> NestedReferences;

    public override string ToString() {
        return $"{ClassId}:{FileId}";
    }
}

/// <summary>
/// This tells Unity which class the object belongs to. 
/// Each Class ID is defined in Unity’s source code, but a full list of them can be found here:
/// https://docs.unity3d.com/Manual/ClassIDReference.html
/// </summary>
public enum UnityClassId {
    Object = 0,
    GameObject = 1,
    Component = 2,
    LevelGameManager = 3,
    Transform = 4,
    TimeManager = 5,
    GlobalGameManager = 6,
    Behaviour = 8,
    GameManager = 9,
    AudioManager = 11,
    InputManager = 13,
    EditorExtension = 18,
    Physics2DSettings = 19,
    Camera = 20,
    Material = 21,
    MeshRenderer = 23,
    Renderer = 25,
    Texture = 27,
    Texture2D = 28,
    OcclusionCullingSettings = 29,
    GraphicsSettings = 30,
    MeshFilter = 33,
    OcclusionPortal = 41,
    Mesh = 43,
    Skybox = 45,
    QualitySettings = 47,
    Shader = 48,
    TextAsset = 49,
    Rigidbody2D = 50,
    Collider2D = 53,
    Rigidbody = 54,
    PhysicsManager = 55,
    Collider = 56,
    Joint = 57,
    CircleCollider2D = 58,
    HingeJoint = 59,
    PolygonCollider2D = 60,
    BoxCollider2D = 61,
    PhysicsMaterial2D = 62,
    MeshCollider = 64,
    BoxCollider = 65,
    CompositeCollider2D = 66,
    EdgeCollider2D = 68,
    CapsuleCollider2D = 70,
    ComputeShader = 72,
    AnimationClip = 74,
    ConstantForce = 75,
    TagManager = 78,
    AudioListener = 81,
    AudioSource = 82,
    AudioClip = 83,
    RenderTexture = 84,
    CustomRenderTexture = 86,
    Cubemap = 89,
    Avatar = 90,
    AnimatorController = 91,
    RuntimeAnimatorController = 93,
    ShaderNameRegistry = 94,
    Animator = 95,
    TrailRenderer = 96,
    DelayedCallManager = 98,
    TextMesh = 102,
    RenderSettings = 104,
    Light = 108,
    ShaderInclude = 109,
    BaseAnimationTrack = 110,
    Animation = 111,
    MonoBehaviour = 114,
    MonoScript = 115,
    MonoManager = 116,
    Texture3D = 117,
    NewAnimationTrack = 118,
    Projector = 119,
    LineRenderer = 120,
    Flare = 121,
    Halo = 122,
    LensFlare = 123,
    FlareLayer = 124,
    NavMeshProjectSettings = 126,
    Font = 128,
    PlayerSettings = 129,
    NamedObject = 130,
    PhysicsMaterial = 134,
    SphereCollider = 135,
    CapsuleCollider = 136,
    SkinnedMeshRenderer = 137,
    FixedJoint = 138,
    BuildSettings = 141,
    AssetBundle = 142,
    CharacterController = 143,
    CharacterJoint = 144,
    SpringJoint = 145,
    WheelCollider = 146,
    ResourceManager = 147,
    PreloadData = 150,
    MovieTexture = 152,
    ConfigurableJoint = 153,
    TerrainCollider = 154,
    TerrainData = 156,
    LightmapSettings = 157,
    WebCamTexture = 158,
    EditorSettings = 159,
    EditorUserSettings = 162,
    AudioReverbFilter = 164,
    AudioHighPassFilter = 165,
    AudioChorusFilter = 167,
    AudioReverbZone = 167,
    AudioEchoFilter = 168,
    AudioLowPassFilter = 169,
    AudioDistortionFilter = 170,
    SparseTexture = 171,
    AudioBehaviour = 180,
    AudioFilter = 181,
    WindZone = 182,
    Cloth = 183,
    SubstanceArchive = 184,
    ProceduralMaterial = 185,
    ProceduralTexture = 186,
    Texture2DArray = 187,
    CubemapArray = 188,
    OffMeshLink = 191,
    OcclusionArea = 192,
    Tree = 193,
    NavMeshAgent = 195,
    NavMeshSettings = 196,
    ParticleSystem = 198,
    ParticleSystemRenderer = 199,
    ShaderVariantCollection = 200,
    LODGroup = 205,
    BlendTree = 206,
    Motion = 207,
    NavMeshObstacle = 208,
    SortingGroup = 210,
    SpriteRenderer = 212,
    Sprite = 213,
    CachedSpriteAtlas = 214,
    ReflectionProbe = 215,
    Terrain = 218,
    LightProbeGroup = 220,
    AnimatorOverrideController = 221,
    CanvasRenderer = 222,
    Canvas = 223,
    RectTransform = 224,
    CanvasGroup = 225,
    BillboardAsset = 226,
    BillboardRenderer = 227,
    SpeedTreeWindAsset = 228,
    AnchoredJoint2D = 229,
    Joint2D = 230,
    SpringJoint2D = 231,
    DistanceJoint2D = 232,
    HingeJoint2D = 233,
    SliderJoint2D = 234,
    WheelJoint2D = 235,
    ClusterInputManager = 236,
    BaseVideoTexture = 237,
    NavMeshData = 238,
    AudioMixer = 240,
    AudioMixerController = 241,
    AudioMixerGroupController = 243,
    AudioMixerEffectController = 244,
    AudioMixerSnapshotController = 245,
    PhysicsUpdateBehaviour2D = 246,
    ConstantForce2D = 247,
    Effector2D = 248,
    AreaEffector2D = 249,
    PointEffector2D = 250,
    PlatformEffector2D = 251,
    SurfaceEffector2D = 252,
    BuoyancyEffector2D = 253,
    RelativeJoint2D = 254,
    FixedJoint2D = 255,
    FrictionJoint2D = 256,
    TargetJoint2D = 257,
    LightProbes = 258,
    LightProbeProxyVolume = 259,
    SampleClip = 271,
    AudioMixerSnapshot = 272,
    AudioMixerGroup = 273,
    AssetBundleManifest = 290,
    RuntimeInitializeOnLoadManager = 300,
    UnityConnectSettings = 310,
    AvatarMask = 319,
    PlayableDirector = 320,
    VideoPlayer = 328,
    VideoClip = 329,
    ParticleSystemForceField = 330,
    SpriteMask = 331,
    OcclusionCullingData = 363,
    PrefabInstance = 1001,
    EditorExtensionImpl = 1002,
    AssetImporter = 1003,
    Mesh3DSImporter = 1005,
    TextureImporter = 1006,
    ShaderImporter = 1007,
    ComputeShaderImporter = 1008,
    AudioImporter = 1020,
    HierarchyState = 1026,
    AssetMetaData = 1028,
    DefaultAsset = 1029,
    DefaultImporter = 1030,
    TextScriptImporter = 1031,
    SceneAsset = 1032,
    NativeFormatImporter = 1034,
    MonoImporter = 1035,
    LibraryAssetImporter = 1038,
    ModelImporter = 1040,
    FBXImporter = 1041,
    TrueTypeFontImporter = 1042,
    EditorBuildSettings = 1045,
    InspectorExpandedState = 1048,
    AnnotationManager = 1049,
    PluginImporter = 1050,
    EditorUserBuildSettings = 1051,
    IHVImageFormatImporter = 1055,
    AnimatorStateTransition = 1101,
    AnimatorState = 1102,
    HumanTemplate = 1105,
    AnimatorStateMachine = 1107,
    PreviewAnimationClip = 1108,
    AnimatorTransition = 1109,
    SpeedTreeImporter = 1110,
    AnimatorTransitionBase = 1111,
    SubstanceImporter = 1112,
    LightmapParameters = 1113,
    LightingDataAsset = 1120,
    SketchUpImporter = 1124,
    BuildReport = 1125,
    PackedAssets = 1126,
    VideoClipImporter = 1127,
    Int = 100000,
    Bool = 100001,
    Float = 100002,
    MonoObject = 100003,
    Collision = 100004,
    Vector3f = 100005,
    RootMotionData = 100006,
    Collision2D = 100007,
    AudioMixerLiveUpdateFloat = 100008,
    AudioMixerLiveUpdateBool = 100009,
    Polygon2D = 100010,
    Void = 100011,
    TilemapCollider2D = 19719996,
    ImportLog = 41386430,
    GraphicsStateCollection = 55640938,
    VFXRenderer = 73398921,
    Grid = 156049354,
    ScenesUsingAssets = 156483287,
    ArticulationBody = 171741748,
    Preset = 181963792,
    IConstraint = 285090594,
    AssemblyDefinitionReferenceImporter = 294290339,
    AudioResource = 355983997,
    AssetImportInProgressProxy = 369655926,
    PluginBuildInfo = 382020655,
    MemorySettings = 387306366,
    BuildMetaDataImporter = 403037116,
    BuildInstructionImporter = 403037117,
    EditorProjectAccess = 426301858,
    PrefabImporter = 468431735,
    TilemapRenderer = 483693784,
    SpriteAtlasAsset = 612988286,
    SpriteAtlasDatabase = 638013454,
    AudioBuildInfo = 641289076,
    CachedSpriteAtlasRuntimeData = 644342135,
    MultiplayerManager = 655991488,
    AssemblyDefinitionReferenceAsset = 662584278,
    BuiltAssetBundleInfoSet = 668709126,
    SpriteAtlas = 687078895,
    RayTracingShaderImporter = 747330370,
    BuildArchiveImporter = 780535461,
    PreviewImporter = 815301076,
    RayTracingShader = 825902497,
    LightingSettings = 850595691,
    PlatformModuleSetup = 877146078,
    VersionControlSettings = 890905787,
    CustomCollider2D = 893571522,
    AimConstraint = 895512359,
    VFXManager = 937362698,
    RoslynAnalyzerConfigAsset = 947337230,
    RuleSetFileAsset = 954905827,
    VisualEffectSubgraph = 994735392,
    VisualEffectSubgraphOperator = 994735403,
    VisualEffectSubgraphBlock = 994735404,
    Prefab = 1001480554,
    LocalizationImporter = 1027052791,
    ReferencesArtifactGenerator = 1114811875,
    AssemblyDefinitionAsset = 1152215463,
    SceneVisibilityState = 1154873562,
    LookAtConstraint = 1183024399,
    SpriteAtlasImporter = 1210832254,
    AudioContainerElement = 1233149941,
    GameObjectRecorder = 1268269756,
    AudioRandomContainer = 1307931743,
    LightingDataAssetParent = 1325145578,
    PresetManager = 1386491679,
    StreamingManager = 1403656975,
    LowerResBlitTexture = 1480428607,
    VideoBuildInfo = 1521398425,
    C4DImporter = 1541671625,
    StreamingController = 1542919678,
    ShaderContainer = 1557264870,
    RoslynAdditionalFileAsset = 1597193336,
    RoslynAdditionalFileImporter = 1642787288,
    MultiplayerRolesData = 1652712579,
    SceneRoots = 1660057539,
    BrokenPrefabAsset = 1731078267,
    AndroidAssetPackImporter = 1736697216,
    GridLayout = 1742807556,
    AssemblyDefinitionImporter = 1766753193,
    ParentConstraint = 1773428102,
    RuleSetFileImporter = 1777034230,
    PositionConstraint = 1818360608,
    RotationConstraint = 1818360609,
    ScaleConstraint = 1818360610,
    Tilemap = 1839735485,
    PackageManifest = 1896753125,
    PackageManifestImporter = 1896753126,
    RoslynAnalyzerConfigImporter = 1903396204,
    UIRenderer = 1931382933,
    TerrainLayer = 1953259897,
    SpriteShapeRenderer = 1971053207,
    VisualEffectAsset = 2058629509,
    VisualEffectImporter = 2058629510,
    VisualEffectResource = 2058629511,
    VisualEffectObject = 2059678085,
    VisualEffect = 2083052967,
    LocalizationAsset = 2083778819,
    ScriptedImporter = 2089858483,
    ShaderIncludeImporter = 2103361453
}
