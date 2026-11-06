using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.NightVisionGoggles;

/// <summary>
///     A helmet-mounted night vision visor. Attached to a worn helmet and flipped down, it gives the wearer night
///     vision. It's balanced by two drawbacks: it glows while on, giving the wearer away in the dark, and every so
///     often it shorts out and has to be fixed with a screwdriver before it works again.
///     Drives the <see cref="Content.Shared._RMC14.NightVision.NightVisionItemComponent"/> on the same entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMUNightVisionGogglesSystem))]
public sealed partial class CMUNightVisionGogglesComponent : Component
{
    /// <summary>Whether the visor has shorted out and needs a screwdriver.</summary>
    [DataField, AutoNetworkedField]
    public bool Broken;

    /// <summary>Action given to whoever wears the helmet the visor is attached to.</summary>
    [DataField]
    public EntProtoId ActionId = "CMUActionToggleNightVisionGoggles";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>Who currently has <see cref="Action"/>.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionUser;

    /// <summary>
    ///     Light following the wearer while the visor is on. The visor sits in the helmet's storage, which blocks
    ///     light, so the glow lives on its own entity parented to the wearer.
    /// </summary>
    [DataField]
    public EntProtoId GlowPrototype = "CMUNightVisionGogglesGlow";

    [DataField, AutoNetworkedField]
    public EntityUid? Glow;

    /// <summary>Shortest stretch of switched-on use before the visor can short out.</summary>
    [DataField]
    public TimeSpan MinTimeToMalfunction = TimeSpan.FromMinutes(4);

    /// <summary>Longest stretch of switched-on use before the visor shorts out.</summary>
    [DataField]
    public TimeSpan MaxTimeToMalfunction = TimeSpan.FromMinutes(12);

    /// <summary>
    ///     Switched-on time left before the next malfunction. Only counts down while the visor is on,
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

public sealed partial class CMUToggleNightVisionGogglesActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class CMUNightVisionGogglesRepairDoAfterEvent : SimpleDoAfterEvent;
