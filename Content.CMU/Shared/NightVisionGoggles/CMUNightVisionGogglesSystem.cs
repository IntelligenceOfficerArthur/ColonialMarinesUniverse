using Content.Shared._RMC14.NightVision;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.CMU14.NightVisionGoggles;

/// <summary>
///     Handles <see cref="CMUNightVisionGogglesComponent"/>: lights the goggles up while they're on, shorts them
///     out after a random stretch of use and lets a screwdriver fix them.
/// </summary>
public sealed class CMUNightVisionGogglesSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedNightVisionSystem _nightVision = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, MapInitEvent>(OnMapInit);
        // Has to run first so a broken pair never reaches the night vision toggle.
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, ToggleActionEvent>(OnToggleAction,
            before: [typeof(SharedNightVisionSystem)]);
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, CMUNightVisionGogglesRepairDoAfterEvent>(OnRepairDoAfter);
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<CMUNightVisionGogglesComponent> ent, ref MapInitEvent args)
    {
        RollMalfunction(ent);
        SetGlow(ent, false);
    }

    private void OnToggleAction(Entity<CMUNightVisionGogglesComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled || !ent.Comp.Broken)
            return;

        args.Handled = true;
        _popup.PopupClient(Loc.GetString("cmu-nvg-broken-toggle", ("goggles", ent.Owner)),
            args.Performer,
            args.Performer,
            PopupType.SmallCaution);
    }

    private void OnInteractUsing(Entity<CMUNightVisionGogglesComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, ent.Comp.RepairQuality))
            return;

        args.Handled = true;

        if (!ent.Comp.Broken)
        {
            _popup.PopupClient(Loc.GetString("cmu-nvg-not-broken", ("goggles", ent.Owner)), ent, args.User);
            return;
        }

        if (_tool.UseTool(args.Used,
                args.User,
                ent,
                (float) ent.Comp.RepairDelay.TotalSeconds,
                ent.Comp.RepairQuality,
                new CMUNightVisionGogglesRepairDoAfterEvent()))
        {
            _popup.PopupClient(Loc.GetString("cmu-nvg-repair-start", ("goggles", ent.Owner)), ent, args.User);
        }
    }

    private void OnRepairDoAfter(Entity<CMUNightVisionGogglesComponent> ent, ref CMUNightVisionGogglesRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !ent.Comp.Broken)
            return;

        args.Handled = true;
        ent.Comp.Broken = false;
        RollMalfunction(ent);
        Dirty(ent);

        _popup.PopupClient(Loc.GetString("cmu-nvg-repair-done", ("goggles", ent.Owner)), ent, args.User);
    }

    private void OnExamined(Entity<CMUNightVisionGogglesComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CMUNightVisionGogglesComponent)))
        {
            args.PushMarkup(Loc.GetString(ent.Comp.Broken ? "cmu-nvg-examine-broken" : "cmu-nvg-examine-working"));
        }
    }

    public override void Update(float frameTime)
    {
        // The malfunction roll is random, so only the server decides it; the result is networked.
        if (_net.IsClient)
            return;

        var frame = TimeSpan.FromSeconds(frameTime);
        var query = EntityQueryEnumerator<CMUNightVisionGogglesComponent, NightVisionItemComponent>();
        while (query.MoveNext(out var uid, out var goggles, out var nightVision))
        {
            var active = nightVision.User != null;
            if (active != goggles.Glowing)
                SetGlow((uid, goggles), active);

            if (!active || goggles.Broken)
                continue;

            goggles.RemainingUntilMalfunction ??= RandomMalfunctionTime(goggles);
            goggles.RemainingUntilMalfunction -= frame;
            if (goggles.RemainingUntilMalfunction > TimeSpan.Zero)
                continue;

            Malfunction((uid, goggles, nightVision));
        }
    }

    private void Malfunction(Entity<CMUNightVisionGogglesComponent, NightVisionItemComponent> ent)
    {
        var user = ent.Comp2.User;
        ent.Comp1.Broken = true;
        ent.Comp1.RemainingUntilMalfunction = null;
        Dirty(ent, ent.Comp1);

        _nightVision.DisableNightVisionItem((ent, ent.Comp2), user);
        SetGlow((ent, ent.Comp1), false);

        _audio.PlayPvs(ent.Comp1.MalfunctionSound, ent);
        if (user != null)
        {
            _popup.PopupEntity(Loc.GetString("cmu-nvg-malfunction", ("goggles", ent.Owner)),
                user.Value,
                user.Value,
                PopupType.MediumCaution);
        }
    }

    private void SetGlow(Entity<CMUNightVisionGogglesComponent> ent, bool glowing)
    {
        ent.Comp.Glowing = glowing;
        Dirty(ent);
        _pointLight.SetEnabled(ent, glowing);
    }

    private void RollMalfunction(Entity<CMUNightVisionGogglesComponent> ent)
    {
        if (_net.IsClient)
            return;

        ent.Comp.RemainingUntilMalfunction = RandomMalfunctionTime(ent.Comp);
    }

    private TimeSpan RandomMalfunctionTime(CMUNightVisionGogglesComponent comp)
    {
        var min = comp.MinTimeToMalfunction;
        var max = comp.MaxTimeToMalfunction < min ? min : comp.MaxTimeToMalfunction;
        return TimeSpan.FromSeconds(_random.NextDouble(min.TotalSeconds, max.TotalSeconds));
    }
}
