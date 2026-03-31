using System;
using System.Threading;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace CustomPingWheel.CustomPingWheelCode.Patches;

/// <summary>
/// Intercepts FlavorSynchronizer.SendEndTurnPing() to route the ping button click
/// through the CustomPingWheel preset system instead of the vanilla end-turn ping.
///
/// Vanilla flow (suppressed):
///   NPingButton click → FlavorSynchronizer.SendEndTurnPing()
///     → _gameService.SendMessage(EndTurnPingMessage)
///     → CreateEndTurnPingDialogueIfNecessary(LocalPlayer)
///
/// Mod flow (replacement):
///   NPingButton click → FlavorSynchronizer.SendEndTurnPing()
///     → [Harmony Prefix — return false]
///     → CustomPingWheelState.SendPreset(0)   [TODO: replace with wheel UI selection]
///
/// Debounce note: vanilla debounce (1 s) lives inside SendEndTurnPing. Because we
/// return false before it executes, we implement our own 1 s cooldown here.
/// _nextAllowedPingTime is stored as long (signed) for Interlocked compatibility;
/// Time.GetTicksMsec() values are cast accordingly.
/// </summary>
[HarmonyPatch(typeof(FlavorSynchronizer), nameof(FlavorSynchronizer.SendEndTurnPing))]
public static class FlavorSynchronizer_SendEndTurnPing_Patch
{
    private static long _nextAllowedPingTime = 0;
    private const long CooldownMsec = 1000;

    [HarmonyPrefix]
    public static bool Prefix(FlavorSynchronizer __instance)
    {
        try
        {
            long now = (long)Time.GetTicksMsec();
            long current = Interlocked.Read(ref _nextAllowedPingTime);
            if (now < current)
                return false; // still in cooldown — suppress vanilla too

            Interlocked.Exchange(ref _nextAllowedPingTime, now + CooldownMsec);

            // TODO: replace hardcoded preset 0 with wheel UI selection
            const int presetIndex = 0;
            GD.Print($"[CustomPingWheel] SendEndTurnPing intercepted. Sending preset {presetIndex}.");
            CustomPingWheelState.SendPreset(presetIndex);
            return false; // suppress vanilla EndTurnPingMessage + dialogue
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CustomPingWheel] FlavorSynchronizer patch exception: {ex}");
            return true; // fallback to vanilla on error
        }
    }
}
