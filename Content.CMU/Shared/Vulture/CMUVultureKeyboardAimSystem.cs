using Content.Shared._RMC14.Scoping;
using Content.Shared._RMC14.Weapons.Ranged.Vulture;
using Content.Shared.ActionBlocker;
using Content.Shared.Popups;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.CMU14.Vulture;

/// <summary>
///     Turns WASD into crosshair steps and Shift into a breath hold while looking through a keyboard-aimed Vulture
///     scope. The sniper is rooted for as long as they scope, so the keys never move the character.
/// </summary>
public sealed class CMUVultureKeyboardAimSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedVultureAimSystem _vulture = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>Next repeat time per user while a movement key is held. Server only.</summary>
    private readonly Dictionary<EntityUid, TimeSpan> _nextRepeat = new();
    private readonly List<EntityUid> _stale = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ScopingComponent, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<ScopingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<ScopingComponent, ScopedEvent>(OnScoped);
        SubscribeLocalEvent<ScopingComponent, ComponentShutdown>(OnScopingShutdown);
    }

    private void OnScoped(Entity<ScopingComponent> user, ref ScopedEvent args)
    {
        if (_net.IsClient || !_vulture.TryGetKeyboardAim(user, out _))
            return;

        _blocker.UpdateCanMove(user);
        _popup.PopupEntity(Loc.GetString("cmu-vulture-keyboard-controls"), user, user);
    }

    private void OnScopingShutdown(Entity<ScopingComponent> user, ref ComponentShutdown args)
    {
        _nextRepeat.Remove(user);

        if (_net.IsClient || TerminatingOrDeleted(user))
            return;

        _blocker.UpdateCanMove(user);
    }

    private void OnUpdateCanMove(EntityUid user, ScopingComponent scoping, UpdateCanMoveEvent args)
    {
        // A removed component must not keep the sniper rooted.
        if (scoping.LifeStage >= ComponentLifeStage.Stopping)
            return;

        if (_vulture.TryGetKeyboardAim(user, out _))
            args.Cancel();
    }

    private void OnMoveInput(Entity<ScopingComponent> user, ref MoveInputEvent args)
    {
        if (_net.IsClient || !_vulture.TryGetKeyboardAim(user, out var aim))
            return;

        var held = args.Entity.Comp.HeldMoveButtons;
        var pressed = held & ~args.OldMovement;
        var released = args.OldMovement & ~held;

        var step = StepFor(pressed);
        if (step != Vector2i.Zero)
        {
            _vulture.StepCrosshair(aim, step);
            _nextRepeat[user] = _timing.CurTime + aim.Comp.KeyRepeatDelay;
        }

        if ((SharedMoverController.GetNormalizedMovement(held) & MoveButtons.AnyDirection) == MoveButtons.None)
            _nextRepeat.Remove(user);

        if ((pressed & MoveButtons.Walk) != 0)
            _vulture.TryHoldBreath(aim, user);

        if ((released & MoveButtons.Walk) != 0)
            _vulture.ReleaseBreath(aim);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient || _nextRepeat.Count == 0)
            return;

        _stale.Clear();
        foreach (var (user, next) in _nextRepeat)
        {
            if (_timing.CurTime < next)
                continue;

            if (!TryComp(user, out InputMoverComponent? mover) ||
                !_vulture.TryGetKeyboardAim(user, out var aim))
            {
                _stale.Add(user);
                continue;
            }

            var step = StepFor(SharedMoverController.GetNormalizedMovement(mover.HeldMoveButtons));
            if (step == Vector2i.Zero)
            {
                _stale.Add(user);
                continue;
            }

            _vulture.StepCrosshair(aim, step);
            _nextRepeat[user] = _timing.CurTime + aim.Comp.KeyRepeatInterval;
        }

        foreach (var user in _stale)
        {
            _nextRepeat.Remove(user);
        }
    }

    /// <summary>
    ///     Maps held buttons to a crosshair step. Up is +Y because the scope view is not rotated.
    /// </summary>
    private static Vector2i StepFor(MoveButtons buttons)
    {
        var x = 0;
        var y = 0;

        if ((buttons & MoveButtons.Up) != 0)
            y++;
        if ((buttons & MoveButtons.Down) != 0)
            y--;
        if ((buttons & MoveButtons.Right) != 0)
            x++;
        if ((buttons & MoveButtons.Left) != 0)
            x--;

        return new Vector2i(x, y);
    }
}
