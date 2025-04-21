using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Attributes;
using Zenject;


namespace WorldPosOffset.Menu;

internal class GameplayMenu : IInitializable, IDisposable
{
    private const string MenuName = "World Pos Offset";
    private const string ResourcePath = "WorldPosOffset.Menu.gameplayMenu.bsml";
    
    private readonly PluginConfig _pluginConfig;

    public GameplayMenu(PluginConfig pluginConfig)
    {
        _pluginConfig = pluginConfig;
    }
    
    // Zenject will call IInitializable.Initialize for any menu bindings when the main menu loads for the first
    // time or when the game restarts internally, such as when settings are applied.
    public void Initialize()
    {
        GameplaySetup.Instance.AddTab(MenuName, ResourcePath, this);
    }

    public void Dispose()
    {
        if (GameplaySetup.Instance != null)
        {
            _pluginConfig.Changed();
            GameplaySetup.Instance.RemoveTab(MenuName);
        }
    }
    
    [UIValue("list-options")]
    private List<object> options = new object[] { "A", "B", "C", "D", "E" }.ToList();
}