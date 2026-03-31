using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace CustomPingWheel.CustomPingWheelCode.Network;

/// <summary>
/// Network message sent when a player selects a preset from the ping wheel.
///
/// Auto-discovery chain (game internals, read-only for this mod):
///   <c>ReflectionHelper.GetSubtypesInMods&lt;INetMessage&gt;()</c>
///     → MessageTypes static ctor (vanilla + mod types, sorted alphabetically)
///     → NetTypeCache (byte ID → Type, used at deserialisation)
///
/// INetMessageSubtypes / _subtypes are private to the game DLL and are not
/// referenced anywhere in this mod. The mod relies entirely on the automatic
/// alphabetical inclusion performed by MessageTypes at startup.
///
/// Alphabetical position of "PingPresetMessage":
///   - Starts with 'P', so it inserts after all vanilla names that start with
///     'A'–'O' and before all names that start with 'Q'–'Z'.
///   - Within the 'P' bucket it comes after "Pa…"–"Ph…" names and before "Po…"–"Py…" names.
///   - The exact numeric ID depends on the full vanilla list; to find it at
///     runtime use: <c>MessageTypes.TypeToId&lt;PingPresetMessage&gt;()</c>
///
/// IMPORTANT: Both players must have this mod loaded for the network to work
/// correctly. If only one side loads the mod the byte IDs diverge and
/// NetMessageBus will throw IndexOutOfRangeException on the remote side.
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