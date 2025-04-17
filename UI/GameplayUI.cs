using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Attributes;
using Zenject;


namespace WorldPosOffset.UI;

internal class GameplayUI : IInitializable, IDisposable
{
    public void Initialize()
    {
        GameplaySetup.Instance.AddTab(
            "World Pos Offset", 
            "WorldPosOffset.UI.gameplayUI.bsml", 
            this, 
            MenuType.Solo | MenuType.Campaign);
    }

    public void Dispose()
    {
        if (GameplaySetup.Instance != null)
        {
            //PluginConfig.Instance.Changed();
            GameplaySetup.Instance.RemoveTab("World Pos Offset");
        }
    }
    
    [UIValue("list-options")]
    private List<object> options = new object[] { "A", "B", "C", "D", "E" }.ToList();
}