using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace CustomPingWheel.CustomPingWheelCode.Patches;

/// <summary>
/// Intercepts FlavorSynchronizer.SendEndTurnPing() to replace the vanilla ping
/// with a custom preset message from the ping wheel.
///
/// Because this prefix returns false, the vanilla method never executes — the vanilla
/// debounce guard (_nextAllowedPingTime) does NOT apply.
/// TODO: Add a separate debounce in the custom path to prevent spam.
/// TODO: Replace hardcoded preset index 0 with an actual preset selection UI wheel.
/// </summary>
[HarmonyPatch(typeof(FlavorSynchronizer), nameof(FlavorSynchronizer.SendEndTurnPing))]
public static class FlavorSynchronizer_SendEndTurnPing_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(FlavorSynchronizer __instance)
    {
        try
        {
            // TODO: Show preset selection wheel UI here and let player choose.
            // For now, always send preset index 0.
            const int presetIndex = 0;

            GD.Print($"[CustomPingWheel] SendEndTurnPing intercepted. Sending preset {presetIndex}.");
            CustomPingWheelState.SendPreset(presetIndex);

            // Return false to suppress vanilla EndTurnPingMessage and vanilla dialogue.
            return false;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CustomPingWheel] FlavorSynchronizer patch exception: {ex}");
            // On error, fall through to vanilla behaviour.
            return true;
        }
    }
}
