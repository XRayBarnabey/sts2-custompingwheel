using Godot;
using HarmonyLib;

namespace CustomPingWheel.CustomPingWheelCode.Patches;

/// <summary>
/// Intercepts the vanilla ping button click by prefixing NPingButton.OnRelease.
/// Replaces the vanilla ping with a custom preset message and suppresses the original call.
///
/// Injection point confirmed via ILSpy analysis of NCombatUi:
///   - NPingButton is initialized in NCombatUi._Ready() via GetNode&lt;NPingButton&gt;("%PingButton").
///   - OnRelease() is the method called when the player clicks the Ping button.
///   - NCombatUi has no dedicated ping signal or handler: the button manages its own click.
///
/// NOTE: If this patch does not apply (Harmony warning in log), verify the full type name
/// of NPingButton in ILSpy — it may be in MegaCrit.Sts2.Core.Combat.UI instead.
/// </summary>
[HarmonyPatch("MegaCrit.Sts2.Core.UI.NPingButton", "OnRelease")]
public static class NPingButton_OnRelease_Patch
{
    /// <summary>Default preset sent until the wheel UI selection is implemented.</summary>
    private const int DefaultPresetIndex = 0;

    /// <summary>
    /// Returns false to suppress the vanilla ping and send a custom preset instead.
    /// </summary>
    [HarmonyPrefix]
    public static bool Prefix()
    {
        GD.Print("[CustomPingWheel] NPingButton.OnRelease intercepted — sending custom preset.");
        CustomPingWheelState.SendPreset(DefaultPresetIndex);
        return false;
    }
}
