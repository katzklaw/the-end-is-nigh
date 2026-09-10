using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace Ender;

[BepInPlugin(PluginGuid, "Ender", PluginVersion)]
[BepInProcess("Among Us.exe")]
public class EnderPlugin : BasePlugin
{
    public const string PluginGuid = "com.katzklaw.ender";
    public const string PluginVersion = "1.0.0";

    public Harmony Harmony { get; } = new(PluginGuid);
    public static EnderPlugin Instance;
    public static new ManualLogSource Log;

    public override void Load()
    {
        Instance = this;
        Log = base.Log;

        try
        {
            Harmony.PatchAll();
            Log.LogInfo($"Ender {PluginVersion} loaded.");
        }
        catch (Exception ex)
        {
            Log.LogError("Ender failed to load: " + ex);
        }
    }
}