using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace CustomPingWheel.CustomPingWheelCode.Patches;

/// <summary>
/// Intercepts FlavorSynchronizer.SendEndTurnPing() to replace the vanilla ping
/// with a custom preset selection.
///
/// Vanilla flow (ILSpy dump — sts2 v0.1.0.0):
///   public void SendEndTurnPing()
///   {
///       if (Time.GetTicksMsec() >= _nextAllowedPingTime)
///       {
///           _gameService.SendMessage(default(EndTurnPingMessage));
///           _nextAllowedPingTime = Time.GetTicksMsec() + 1000;
///           CreateEndTurnPingDialogueIfNecessary(LocalPlayer);
///       }
///   }
///
/// The Prefix returns false to suppress the vanilla ping when a preset is sent.
/// It returns true (fallback) if CustomPingWheelState is not ready (net not set, etc.).
///
/// NOTE: The vanilla debounce (1 s) is not enforced in this path. A custom debounce
/// can be added here once the UI wheel is implemented.
///
/// TODO: Replace the hardcoded preset index with the value selected by the UI wheel.
/// </summary>
[HarmonyPatch(typeof(FlavorSynchronizer), nameof(FlavorSynchronizer.SendEndTurnPing))]
public static class FlavorSynchronizer_SendEndTurnPing_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(FlavorSynchronizer __instance)
    {
        try
        {
            // TODO: Show preset selection wheel UI — currently hardcoded to preset 0
            // TODO (known issue): vanilla debounce is bypassed here — add custom 1 s cooldown
            //   before UI wheel is wired in to avoid rapid-fire spam.
            const int presetIndex = 0;
            GD.Print($"[CustomPingWheel] SendEndTurnPing intercepted. Sending preset {presetIndex}.");
            CustomPingWheelState.SendPreset(presetIndex);
            return false; // suppress vanilla ping
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CustomPingWheel] FlavorSynchronizer patch exception in SendPreset: {ex}");
            return true; // fallback to vanilla ping on error
        }
    }
}
