namespace Nomnom;

public class UnityBuildToProject_AssetParsing {
    [Fact]
    public void MetaFile_Guid_DoesParse() {
        var monoFolder   = Path.Combine("..", "..", "..", "..", "UnityBuildToProject", "bin", "Debug", "net9.0", "output", "ExportedProject", "Assets", "MonoBehaviour");
        var metaFilePath = Path.GetFullPath(
            Path.Combine(monoFolder, "Post Processing Profile.asset.meta")
        );
        
        var metaFile = UnityAssetTypes.ParseMetaFile(metaFilePath);
        Assert.NotNull(metaFile);
        Assert.NotEmpty(metaFile.Guid.Value);
    }
    
    [Fact]
    public void AssetFile_Guid_DoesParse() {
        var monoFolder    = Path.Combine("..", "..", "..", "..", "UnityBuildToProject", "bin", "Debug", "net9.0", "output", "ExportedProject", "Assets", "MonoBehaviour");
        var assetFilePath = Path.GetFullPath(
            Path.Combine(monoFolder, "Post Processing Profile.asset")
        );
        
        var assetFile = UnityAssetTypes.ParseAssetFile(assetFilePath);
        Assert.NotNull(assetFile);
        Assert.NotEmpty(assetFile.SubAssets);
        Assert.NotEmpty(assetFile.Assets);
        
        foreach (var subasset in assetFile.SubAssets) {
            Console.WriteLine($"subasset: {subasset}");
        }
        
        foreach (var asset in assetFile.Assets) {
            Console.WriteLine($"asset: {asset}");
        }
    }
}
