using AssetRipper.Export.UnityProjects.Configuration;
using AssetRipper.Processing;

namespace Nomnom;

public record ExtractData {
    public required GameData GameData;
    public required LibraryConfiguration Config;
};
