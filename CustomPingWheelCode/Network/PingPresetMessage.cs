using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace CustomPingWheel.CustomPingWheelCode.Network;

/// <summary>
/// Network message sent when a player selects a preset from the ping wheel.
///
/// AUTOMATIC DISCOVERY: Discovered by MessageTypes via ReflectionHelper.GetSubtypesInMods
/// — no manual registration needed. NetTypeCache sorts all INetMessage types alphabetically
/// by Type.Name (string.CompareOrdinal), so IDs are deterministic.
///
/// CONFIRMED ID POSITION (with mod loaded):
///   ID 29: PeerInputMessage     (vanilla)
///   ID 30: PingPresetMessage    ← THIS MESSAGE (mod)
///   ID 31: PlayerChoiceMessage  (vanilla, was ID 30 without mod)
///   ... all subsequent vanilla IDs shift by +1 when mod is loaded
///
/// NO NAME COLLISION: No vanilla type starts with "PingP" — confirmed from full
/// INetMessageSubtypes._subtypes list (49 vanilla types).
///
/// CRITICAL: Both players MUST have this mod loaded. If one side is missing the mod,
/// IDs 30+ will be mismatched → IndexOutOfRangeException in TryDeserializeMessage.
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