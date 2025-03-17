using BepInEx;
using BepInEx.Logging;

namespace ExtractBuildInfoPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    internal new static ManualLogSource Logger;

    private void Awake() {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        AddressablesTable.Extract();
    }
}
