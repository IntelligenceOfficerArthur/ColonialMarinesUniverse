using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.Vulture;

/// <summary>
///     Marks a scope as a thermal sight. The picture is shown while the scope's night vision is active, so it
///     draws battery power through the scope's <c>PowerCellDraw</c> and drops back to the plain view when drained.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CMUVultureThermalComponent : Component
{
    /// <summary>
    ///     Luminance at or below which the backdrop is darkest.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LowCut = 0.10f;

    /// <summary>
    ///     Luminance at or above which the backdrop is brightest. A narrower band than the fighter sensor gives
    ///     the backdrop more contrast.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HighCut = 0.75f;

    /// <summary>
    ///     Brightness of the darkest backdrop, so the scene never goes fully black.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FloorLevel = 0.05f;

    /// <summary>
    ///     Brightness of the brightest backdrop. Kept well under white so heat colours stand out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CeilingLevel = 0.5f;

    /// <summary>
    ///     Base heat of a living body, from 0 (dark red) to 1 (yellow).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatBase = 0.6f;

    /// <summary>
    ///     How much a body's own sprite brightness shifts its heat. A low value makes bodies look alike, which makes
    ///     telling a marine from a xeno from a civilian harder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatSpread = 0.5f;

    /// <summary>
    ///     When false a body must be in line of sight. When true it shows through walls.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SeeThroughWalls;
}
