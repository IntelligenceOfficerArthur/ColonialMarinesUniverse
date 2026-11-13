using Content.Shared._RMC14.Scoping;
using Content.Shared.Popups;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Weapons.Ranged.Vulture;

// CMU14 class: keyboard-driven crosshair and breath control. Aim state stays writable only from this system.
public abstract partial class SharedVultureAimSystem
{
    /// <summary>
    ///     Resolves the keyboard-aimed Vulture a scoped user is looking through, if any.
    ///     Spotter scopes are never keyboard-aimed.
    /// </summary>
    public bool TryGetKeyboardAim(EntityUid user, out Entity<VultureAimComponent> aim)
    {
        aim = default;

        if (!TryComp(user, out ScopingComponent? scoping) ||
            scoping.Scope is not { } scope ||
            !_container.TryGetContainingContainer((scope, null), out var container) ||
            !TryComp(container.Owner, out VultureAimComponent? comp) ||
            !comp.KeyboardAim)
        {
            return false;
        }

        var candidate = (container.Owner, comp);
        if (!IsUsingVultureScope(candidate, user))
            return false;

        aim = candidate;
        return true;
    }

    /// <summary>
    ///     Moves the crosshair by whole tiles inside the box.
    /// </summary>
    public void StepCrosshair(Entity<VultureAimComponent> ent, Vector2i delta)
    {
        if (_net.IsClient || delta == Vector2i.Zero)
            return;

        var radius = ent.Comp.BoxRadius;
        ent.Comp.CrosshairAdjust = new Vector2i(
            Math.Clamp(ent.Comp.CrosshairAdjust.X + delta.X, -radius, radius),
            Math.Clamp(ent.Comp.CrosshairAdjust.Y + delta.Y, -radius, radius));
        UpdateAimCoordinates(ent);
        Dirty(ent);
    }

    /// <summary>
    ///     Starts a breath hold, which stills the crosshair for its duration.
    /// </summary>
    public bool TryHoldBreath(Entity<VultureAimComponent> ent, EntityUid user)
    {
        if (_net.IsClient)
            return false;

        if (ent.Comp.BreathCooldownEndsAt > Timing.CurTime)
        {
            _popup.PopupEntity(Loc.GetString("rmc-vulture-breath-cooldown"), user, user, PopupType.SmallCaution);
            return false;
        }

        ent.Comp.BreathEndsAt = Timing.CurTime + ent.Comp.BreathDuration;
        ent.Comp.BreathCooldownEndsAt = Timing.CurTime + ent.Comp.BreathDuration + ent.Comp.BreathCooldown;
        ent.Comp.SwayOffset = Vector2i.Zero;
        ent.Comp.DriftOffset = System.Numerics.Vector2.Zero;
        ent.Comp.DriftTarget = System.Numerics.Vector2.Zero;
        ent.Comp.NextSwayAt = ent.Comp.BreathEndsAt;
        UpdateAimCoordinates(ent);
        Dirty(ent);
        return true;
    }

    /// <summary>
    ///     Lets go of the breath early. The cooldown starts from now rather than from the end of the full hold.
    /// </summary>
    public void ReleaseBreath(Entity<VultureAimComponent> ent)
    {
        if (_net.IsClient || !ent.Comp.BreathActive(Timing.CurTime))
            return;

        ent.Comp.BreathEndsAt = Timing.CurTime;
        ent.Comp.BreathCooldownEndsAt = Timing.CurTime + ent.Comp.BreathCooldown;
        ent.Comp.NextSwayAt = Timing.CurTime;
        Dirty(ent);
    }
}
