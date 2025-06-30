using Nomnom;

namespace toree3d;

public static class GameSettingsProvider {
    public static GameSettings GetGameSettings() {
        var settings = GameSettings.Default;
        
        // set settings here!
        settings.General.OpenScenePath = "Scenes/TitleMenu/StartScene";
        
        return settings;
    }
}
