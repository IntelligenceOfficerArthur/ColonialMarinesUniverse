using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.NightVisionGoggles;

/// <summary>
///     Night vision goggles balanced by two drawbacks: they glow while switched on, giving away the wearer in the
///     dark, and every so often they short out and have to be fixed with a screwdriver before they work again.
///     Works alongside <see cref="Content.Shared._RMC14.NightVision.NightVisionItemComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMUNightVisionGogglesSystem))]
public sealed partial class CMUNightVisionGogglesComponent : Component
{
    /// <summary>Whether the goggles have shorted out and need a screwdriver.</summary>
    [DataField, AutoNetworkedField]
    public bool Broken;

    /// <summary>Whether the glow light is currently on, so it's only touched when the goggles change state.</summary>
    [DataField, AutoNetworkedField]
    public bool Glowing;

    /// <summary>Shortest stretch of switched-on use before the goggles can short out.</summary>
    [DataField]
    public TimeSpan MinTimeToMalfunction = TimeSpan.FromMinutes(4);

    /// <summary>Longest stretch of switched-on use before the goggles short out.</summary>
    [DataField]
    public TimeSpan MaxTimeToMalfunction = TimeSpan.FromMinutes(12);

    /// <summary>
    ///     Switched-on time left before the next malfunction. Only counts down while the goggles are on,
    ///     rolled again after every repair.
    /// </summary>
    [DataField]
    public TimeSpan? RemainingUntilMalfunction;

    [DataField]
    public TimeSpan RepairDelay = TimeSpan.FromSeconds(4);

    [DataField]
    public ProtoId<ToolQualityPrototype> RepairQuality = "Screwing";

    [DataField]
    public SoundSpecifier? MalfunctionSound = new SoundCollectionSpecifier("sparks");
}

[Serializable, NetSerializable]
public sealed partial class CMUNightVisionGogglesRepairDoAfterEvent : SimpleDoAfterEvent;
