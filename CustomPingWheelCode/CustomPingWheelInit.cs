using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace CustomPingWheel.CustomPingWheelCode;

[ModInitializerAttribute("Initialize")]
public static class CustomPingWheelInit
{
    public static void Initialize()
    {
        GD.Print("[CustomPingWheel] Initializing mod...");
        try
        {
            var harmony = new Harmony("XRayBarnabey.CustomPingWheel");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            GD.Print("[CustomPingWheel] Harmony patches applied.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CustomPingWheel] Failed to apply Harmony patches: {ex}");
        }
        GD.Print("[CustomPingWheel] Mod initialized.");
    }
}