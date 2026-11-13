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
    ///     Luminance at or below which a pixel is fully cold.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LowCut = 0.30f;

    /// <summary>
    ///     Luminance at or above which a pixel is fully hot. A narrow gap to <see cref="LowCut"/> makes the picture
    ///     close to black and white, which hides the detail needed to identify a target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HighCut = 0.62f;

    /// <summary>
    ///     Radius in viewport pixels of the blur that merges sprite detail into blobs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BlurRadius = 1.25f;

    /// <summary>
    ///     Brightness of cold pixels, so the scene never goes fully black.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FloorLevel = 0.04f;
}
