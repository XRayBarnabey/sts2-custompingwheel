using System;
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
        // TODO: Intercept the Ping button click to show the preset wheel.
        // Requires an ILSpy dump of the ping button handler first.
        // Search in ILSpy: Ctrl+Shift+F → "Ping" in MegaCrit.Sts2.Core.UI / MegaCrit.Sts2.Core.Combat
    }
}