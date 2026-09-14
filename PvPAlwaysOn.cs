using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;

namespace PvPAlwaysOn;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class PvPAlwaysPlugin : BaseUnityPlugin
{
    internal const string ModName = "PvPAlwaysOn";
    internal const string ModVersion = "2.1.4";
    private const string ModGUID = "WackyMole.PvPAlwaysOn";
    public static string ConnectionError = "";
    private static string ConfigFileName = ModGUID + ".cfg";
    private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

    public static readonly ManualLogSource PvPAlwaysLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

    private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    private void Awake()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Harmony harmony = new(ModGUID);
        _serverConfigLocked = config("1 - General", "Lock Configuration",
            Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

        /* General */
        ForcePvP = config("1 - General", "PvPForced", Toggle.On, "Force PvP on the server");

        OffInWards = config("1 - General", "Off In Wards", Toggle.Off, "Toggle this on to disable the enforcement of PvP in wards. WardIsLove & BetterWards compatibility. This just means that this mod will not attempt to enforce PvP in wards. WardIsLove & BetterWards will still enforce PvP in wards if you tell them to.");

        harmony.PatchAll(assembly);

        SetupWatcher();
    }

    private void OnDestroy()
    {
        Config.Save();
    }

    private void SetupWatcher()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath)) return;
        try
        {
            PvPAlwaysLogger.LogDebug("ReadConfigValues called");
            Config.Reload();
        }
        catch
        {
            PvPAlwaysLogger.LogError($"There was an issue loading your {ConfigFileName}");
            PvPAlwaysLogger.LogError("Please check your config entries for spelling and format!");
        }
    }

    #region ConfigOptions

    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    internal static ConfigEntry<Toggle> ForcePvP = null!;
    internal static ConfigEntry<Toggle> OffInWards = null!;

    private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription =
            new(
                description.Description +
                (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    private class ConfigurationManagerAttributes
    {
        public bool? Browsable = false;
    }

    #endregion
}