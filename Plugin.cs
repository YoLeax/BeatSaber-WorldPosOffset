using System.Reflection;
using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using HarmonyLib;
using SiraUtil.Zenject;
using WorldPosOffset.Installers;
using IpaLogger = IPA.Logging.Logger;
using IpaConfig = IPA.Config.Config;

namespace WorldPosOffset;

[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
internal class Plugin
{
    internal static IpaLogger Log { get; private set; } = null!;
    
    private Harmony harmony;
    private Assembly executingAssembly = Assembly.GetExecutingAssembly();

    // Methods with [Init] are called when the plugin is first loaded by IPA.
    // All the parameters are provided by IPA and are optional.
    // The constructor is called before any method with [Init]. Only use [Init] with one constructor.
    [Init]
    public Plugin(IpaLogger ipaLogger, IpaConfig ipaConfig, Zenjector zenjector, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
        zenjector.UseLogger(Log);

        // Creates an instance of PluginConfig used by IPA to load and store config values
        var pluginConfig = ipaConfig.Generated<PluginConfig>();
        PluginConfig.Instance = pluginConfig;
        pluginConfig.Init();
        
        harmony = new Harmony("com.YoLeax.BeatSaber.WorldPosOffset");

        // Instructs SiraUtil to use this installer during Beat Saber's initialization
        // The PluginConfig is used as a constructor parameter for AppInstaller, so pass it to zenjector.Install()
        zenjector.Install<AppInstaller>(Location.App, pluginConfig);

        // Instructs SiraUtil to use this installer when the main menu initializes
        zenjector.Install<MenuInstaller>(Location.Menu);

        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }
    
    [OnStart]
    public void OnApplicationStart() => harmony.PatchAll(executingAssembly);

    [OnExit]
    public void OnApplicationQuit() => harmony.UnpatchSelf();
}