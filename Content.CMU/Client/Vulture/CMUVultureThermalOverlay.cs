using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Scoping;
using Content.Shared.CMU14.Vulture;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Vulture;

/// <summary>
///     Remaps the main game view to a high-contrast thermal picture while the local player looks through a powered
///     thermal scope. Drawn below the Vulture's own scope mask so the mask and reticle stay on top.
/// </summary>
public sealed class CMUVultureThermalOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ThermalShader = "CMUVultureThermal";

    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public CMUVultureThermalOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = -1;
        _shader = _prototypes.Index(ThermalShader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        // Other viewports, such as camera consoles, are not looking through the scope.
        return args.ViewportControl == _eye.MainViewport && TryGetThermal(out _);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!TryGetThermal(out var thermal))
            return;

        _shader.SetParameter("low_cut", thermal.LowCut);
        _shader.SetParameter("high_cut", thermal.HighCut);
        _shader.SetParameter("blur_radius", thermal.BlurRadius);
        _shader.SetParameter("floor_level", thermal.FloorLevel);

        var handle = args.ScreenHandle;
        handle.UseShader(_shader);
        handle.DrawTextureRect(args.Viewport.RenderTarget.Texture, args.ViewportBounds);
        handle.UseShader(null);
    }

    private bool TryGetThermal(out CMUVultureThermalComponent thermal)
    {
        thermal = default!;

        if (_players.LocalEntity is not { } player ||
            !_entity.TryGetComponent(player, out ScopingComponent? scoping) ||
            scoping.Scope is not { } scope ||
            !_entity.TryGetComponent(scope, out CMUVultureThermalComponent? comp) ||
            !_entity.TryGetComponent(scope, out NightVisionItemComponent? nightVision) ||
            nightVision.User != player)
        {
            return false;
        }

        thermal = comp;
        return true;
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
