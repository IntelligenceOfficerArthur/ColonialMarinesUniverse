using Robust.Client.Graphics;

namespace Content.Client.CMU14.Vulture;

public sealed class CMUVultureThermalSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlays = default!;

    public override void Initialize()
    {
        _overlays.AddOverlay(new CMUVultureThermalOverlay());
    }

    public override void Shutdown()
    {
        _overlays.RemoveOverlay<CMUVultureThermalOverlay>();
    }
}
