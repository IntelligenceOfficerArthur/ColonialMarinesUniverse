using System.Numerics;
using Content.Shared._RMC14.Armor.ThermalCloak;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Scoping;
using Content.Shared._RMC14.Weapons.Ranged.Vulture;
using Content.Shared.CMU14.Vulture;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Vulture;

/// <summary>
///     The Vulture's thermal picture: the scene as a dim grey backdrop, living bodies redrawn as heat silhouettes, and
///     a thin crosshair. Active while the local player looks through a powered thermal scope.
/// </summary>
public sealed class CMUVultureThermalOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> WorldShaderId = "CMUVultureThermal";
    private static readonly ProtoId<ShaderPrototype> HeatShaderId = "CMUVultureThermalHeat";
    private static readonly Color CrosshairColor = Color.Black.WithAlpha(0.85f);
    private const float CrosshairGap = 0.18f;
    private const float SightRange = 40f;

    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly ExamineSystemShared _examine;
    private readonly MobStateSystem _mobState;
    private readonly SharedContainerSystem _container;
    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;

    private readonly ShaderInstance _worldShader;
    private readonly ShaderInstance _heatShader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    public CMUVultureThermalOverlay()
    {
        IoCManager.InjectDependencies(this);

        // Above the night vision overlays, which draw the scene this pass turns grey.
        ZIndex = 10;

        _examine = _entity.System<ExamineSystemShared>();
        _mobState = _entity.System<MobStateSystem>();
        _container = _entity.System<SharedContainerSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<SharedTransformSystem>();

        _worldShader = _prototypes.Index(WorldShaderId).InstanceUnique();
        _heatShader = _prototypes.Index(HeatShaderId).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        // Other viewports, such as camera consoles, are not looking through the scope.
        return args.Viewport.Eye == _eye.CurrentEye && TryGetThermal(out _, out _);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!TryGetThermal(out var thermal, out var aim) ||
            _players.LocalEntity is not { } player)
        {
            return;
        }

        var handle = args.WorldHandle;

        if (ScreenTexture != null)
        {
            _worldShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _worldShader.SetParameter("low_cut", thermal.LowCut);
            _worldShader.SetParameter("high_cut", thermal.HighCut);
            _worldShader.SetParameter("floor_level", thermal.FloorLevel);
            _worldShader.SetParameter("ceiling_level", thermal.CeilingLevel);
            _worldShader.SetParameter("detail_gain", thermal.DetailGain);
            _worldShader.SetParameter("detail_radius", thermal.DetailRadius);

            handle.UseShader(_worldShader);
            handle.DrawRect(args.WorldBounds, Color.White);
            handle.UseShader(null);
        }

        var eye = args.Viewport.Eye;
        var eyeRot = eye?.Rotation ?? default;
        var map = eye?.Position.MapId;
        var window = GetWindow(args, aim);

        _heatShader.SetParameter("heat_base", thermal.HeatBase);
        _heatShader.SetParameter("heat_spread", thermal.HeatSpread);
        handle.UseShader(_heatShader);

        var bodies = _entity.EntityQueryEnumerator<MobStateComponent, SpriteComponent, TransformComponent>();
        while (bodies.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            if (xform.MapID != map ||
                !sprite.Visible ||
                uid == player ||
                _mobState.IsDead(uid) ||
                _entity.HasComponent<ThermalCloakUserComponent>(uid) ||
                _container.IsEntityOrParentInContainer(uid, xform: xform))
            {
                continue;
            }

            var (position, rotation) = _transform.GetWorldPositionRotation(xform);
            if (!window.Contains(position))
                continue;

            if (!thermal.SeeThroughWalls && !_examine.InRangeUnOccluded(player, uid, SightRange))
                continue;

            _sprite.RenderSprite((uid, sprite), handle, eyeRot, rotation, position);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);

        DrawCrosshair(handle, aim, window);
    }

    /// <summary>
    ///     The world area the sniper can see, used to skip bodies that the scope mask hides anyway.
    /// </summary>
    private static Box2 GetWindow(in OverlayDrawArgs args, VultureAimComponent? aim)
    {
        if (aim?.ViewCenter is { } center && center.MapId == args.MapId)
            return Box2.CenteredAround(center.Position, new Vector2(aim.BoxRadius * 2 + 1));

        return args.WorldAABB;
    }

    private static void DrawCrosshair(DrawingHandleWorld handle, VultureAimComponent? aim, Box2 window)
    {
        if (aim?.CrosshairCoordinates is not { } crosshair)
            return;

        var c = crosshair.Position;

        // Four arms that stop short of the centre so the target stays visible.
        handle.DrawLine(new Vector2(window.Left, c.Y), new Vector2(c.X - CrosshairGap, c.Y), CrosshairColor);
        handle.DrawLine(new Vector2(c.X + CrosshairGap, c.Y), new Vector2(window.Right, c.Y), CrosshairColor);
        handle.DrawLine(new Vector2(c.X, window.Bottom), new Vector2(c.X, c.Y - CrosshairGap), CrosshairColor);
        handle.DrawLine(new Vector2(c.X, c.Y + CrosshairGap), new Vector2(c.X, window.Top), CrosshairColor);
    }

    private bool TryGetThermal(out CMUVultureThermalComponent thermal, out VultureAimComponent? aim)
    {
        thermal = default!;
        aim = null;

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

        if (_container.TryGetContainingContainer((scope, null), out var container))
            _entity.TryGetComponent(container.Owner, out aim);

        return true;
    }

    protected override void DisposeBehavior()
    {
        _worldShader.Dispose();
        _heatShader.Dispose();
        base.DisposeBehavior();
    }
}
