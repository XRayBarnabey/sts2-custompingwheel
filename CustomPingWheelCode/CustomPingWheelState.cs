using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.Vfx;
using MegaCrit.Sts2.Core.Creatures;
using MegaCrit.Sts2.Core.Multiplayer;
using CustomPingWheel.CustomPingWheelCode.Network;

namespace CustomPingWheel.CustomPingWheelCode;

public static class CustomPingWheelState
{
    private static INetGameService? _net;

    /// <summary>
    /// Preset messages shown in the ping wheel.
    /// </summary>
    public static readonly List<string> Presets = new List<string>
    {
        "J'ai besoin d'aide ici !",
        "Je prends cette route.",
        "Attention à cet ennemi !",
        "Je vais ouvrir ce coffre.",
        "On se regroupe ici.",
    };

    public static void SetNet(INetGameService net)
    {
        _net = net;
        GD.Print("[CustomPingWheel] NetService set.");
    }

    /// <summary>
    /// Called when the local player selects a preset from the wheel.
    /// Shows a local speech bubble and sends the message to the remote player.
    /// </summary>
    public static void SendPreset(int presetIndex)
    {
        if (_net == null)
        {
            GD.PrintErr("[CustomPingWheel] SendPreset: _net is null, cannot send.");
            return;
        }
        if (NCombatRoom.Instance == null)
        {
            GD.PrintErr("[CustomPingWheel] SendPreset: NCombatRoom.Instance is null.");
            return;
        }
        if (presetIndex < 0 || presetIndex >= Presets.Count)
        {
            GD.PrintErr($"[CustomPingWheel] SendPreset: presetIndex {presetIndex} out of range.");
            return;
        }

        try
        {
            // Show bubble locally
            var localCreature = FindCreature(_net.NetId);
            if (localCreature != null)
                ShowBubble(localCreature, Presets[presetIndex]);
            else
                GD.PrintErr("[CustomPingWheel] SendPreset: could not find local creature.");

            // Send to remote
            GD.Print($"[CustomPingWheel] Sending preset {presetIndex}: \"{Presets[presetIndex]}\"");
            _net.SendMessage(new PingPresetMessage { presetIndex = (ushort)presetIndex });
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CustomPingWheel] SendPreset exception: {ex}");
        }
    }

    /// <summary>
    /// Called when a remote player sends a PingPresetMessage.
    /// </summary>
    public static void OnReceive(PingPresetMessage msg, ulong senderId)
    {
        GD.Print($"[CustomPingWheel] OnReceive: presetIndex={msg.presetIndex} from senderId={senderId}");
        try
        {
            if (NCombatRoom.Instance == null)
            {
                GD.PrintErr("[CustomPingWheel] OnReceive: NCombatRoom.Instance is null.");
                return;
            }
            if (msg.presetIndex >= Presets.Count)
            {
                GD.PrintErr($"[CustomPingWheel] OnReceive: presetIndex {msg.presetIndex} out of range.");
                return;
            }

            var creature = FindCreature(senderId);
            if (creature == null)
            {
                GD.PrintErr($"[CustomPingWheel] OnReceive: could not find creature for senderId={senderId}.");
                return;
            }

            ShowBubble(creature, Presets[msg.presetIndex]);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[CustomPingWheel] OnReceive exception: {ex}");
        }
    }

    private static Creature? FindCreature(ulong netId)
    {
        return NCombatRoom.Instance?.CreatureNodes
            .Select(n => n?.Entity)
            .FirstOrDefault(c => c != null && c.IsPlayer && c.Player?.NetId == netId);
    }

    private static void ShowBubble(Creature creature, string text)
    {
        var vfx = NSpeechBubbleVfx.Create(text, creature, 3.0f, VfxColor.White);
        NCombatRoom.Instance!.CombatVfxContainer.AddChild(vfx);
        GD.Print($"[CustomPingWheel] ShowBubble: \"{text}\" on {creature}");
    }
}