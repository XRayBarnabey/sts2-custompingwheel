using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace CustomPingWheel.CustomPingWheelCode.Network;

/// <summary>
/// Network message sent when a player selects a preset from the ping wheel.
/// Automatically discovered by MessageTypes via ReflectionHelper.GetSubtypesInMods.
/// ID is assigned alphabetically — 'PingPresetMessage' sorts among vanilla messages.
/// IMPORTANT: Both players must have this mod loaded for the network to work correctly.
/// </summary>
public struct PingPresetMessage : INetMessage, IPacketSerializable
{
    public ushort presetIndex;

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;

    public void Serialize(PacketWriter w) => w.WriteUShort(presetIndex);
    public void Deserialize(PacketReader r) => presetIndex = r.ReadUShort();
}