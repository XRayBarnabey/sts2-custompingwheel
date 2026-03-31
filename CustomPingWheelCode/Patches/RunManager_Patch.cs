using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Run;

namespace CustomPingWheel.CustomPingWheelCode.Patches;

/// <summary>
/// Registers the PingPresetMessage handler after RunManager initializes the net service.
/// Uses RunManager.Instance.NetService (not parameter injection) for robustness.
/// </summary>
[HarmonyPatch(typeof(RunManager), "InitializeShared")]
public static class RunManager_InitializeShared_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        try
        {
            var net = RunManager.Instance?.NetService;
            if (net == null)
            {
                GD.PrintErr("[CustomPingWheel] InitializeShared postfix: NetService is null.");
                return;
            }
            GD.Print("[CustomPingWheel] InitializeShared: registering handler...");
            CustomPingWheelState.SetNet(net);
            net.RegisterMessageHandler<PingPresetMessage>(CustomPingWheelState.OnReceive);
            GD.Print("[CustomPingWheel] InitializeShared: PingPresetMessage handler registered.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CustomPingWheel] InitializeShared patch exception: {ex}");
        }
    }
}