using Content.Shared._RMC14.Clothing;
using Content.Shared._RMC14.NightVision;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.CMU14.NightVisionGoggles;

/// <summary>
///     Handles <see cref="CMUNightVisionGogglesComponent"/>: gives night vision to whoever wears the helmet the
///     visor is flipped down on, lights them up while it's on, shorts the visor out after a random stretch of use
///     and lets a screwdriver fix it.
/// </summary>
public sealed class CMUNightVisionGogglesSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedNightVisionSystem _nightVision = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, CMUToggleNightVisionGogglesActionEvent>(OnToggleAction);
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, CMUNightVisionGogglesRepairDoAfterEvent>(OnRepairDoAfter);
        SubscribeLocalEvent<CMUNightVisionGogglesComponent, ExaminedEvent>(OnExamined);

        // A screwdriver on the helmet fixes a broken visor attached to it, before the helmet tries to store it.
        SubscribeLocalEvent<HelmetAccessoryHolderComponent, InteractUsingEvent>(OnHelmetInteractUsing,
            before: [typeof(SharedStorageSystem)]);
    }

    private void OnMapInit(Entity<CMUNightVisionGogglesComponent> ent, ref MapInitEvent args)
    {
        RollMalfunction(ent);
    }

    private void OnShutdown(Entity<CMUNightVisionGogglesComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsClient)
            return;

        _actions.RemoveAction(ent.Comp.Action);
        QueueDel(ent.Comp.Glow);
    }

    private void OnToggleAction(Entity<CMUNightVisionGogglesComponent> ent, ref CMUToggleNightVisionGogglesActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _itemToggle.Toggle(ent.Owner, args.Performer);
    }

    private void OnActivateAttempt(Entity<CMUNightVisionGogglesComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!ent.Comp.Broken)
            return;

        args.Cancelled = true;
        args.Popup = Loc.GetString("cmu-nvg-broken-toggle", ("goggles", ent.Owner));
    }

    private void OnInteractUsing(Entity<CMUNightVisionGogglesComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, ent.Comp.RepairQuality))
            return;

        args.Handled = true;
        TryStartRepair(ent, args.Used, args.User);
    }

    private void OnHelmetInteractUsing(Entity<HelmetAccessoryHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp(ent, out StorageComponent? storage) ||
            storage.Container == null)
        {
            return;
        }

        foreach (var contained in storage.Container.ContainedEntities)
        {
            if (!TryComp(contained, out CMUNightVisionGogglesComponent? goggles) ||
                !goggles.Broken ||
                !_tool.HasQuality(args.Used, goggles.RepairQuality))
            {
                continue;
            }

            args.Handled = true;
            TryStartRepair((contained, goggles), args.Used, args.User);
            return;
        }
    }

    private void TryStartRepair(Entity<CMUNightVisionGogglesComponent> ent, EntityUid used, EntityUid user)
    {
        if (!ent.Comp.Broken)
        {
            _popup.PopupClient(Loc.GetString("cmu-nvg-not-broken", ("goggles", ent.Owner)), ent, user);
            return;
        }

        if (_tool.UseTool(used,
                user,
                ent,
                (float) ent.Comp.RepairDelay.TotalSeconds,
                ent.Comp.RepairQuality,
                new CMUNightVisionGogglesRepairDoAfterEvent()))
        {
            _popup.PopupClient(Loc.GetString("cmu-nvg-repair-start", ("goggles", ent.Owner)), ent, user);
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
            args.PushMarkup(Loc.GetString("cmu-nvg-examine-helmet"));
        }
    }

    public override void Update(float frameTime)
    {
        // Wearer tracking, the glow entity and the random malfunction roll are all server-decided and networked.
        if (_net.IsClient)
            return;

        var frame = TimeSpan.FromSeconds(frameTime);
        var query = EntityQueryEnumerator<CMUNightVisionGogglesComponent, NightVisionItemComponent, ItemToggleComponent>();
        while (query.MoveNext(out var uid, out var goggles, out var nightVision, out var toggle))
        {
            var ent = (uid, goggles);
            var wearer = GetWearer(uid);

            UpdateAction(ent, wearer, toggle.Activated);

            var shouldSee = wearer != null && toggle.Activated && !goggles.Broken;
            if (shouldSee && nightVision.User != wearer)
                _nightVision.EnableNightVisionItem((uid, nightVision), wearer!.Value);
            else if (!shouldSee && nightVision.User != null)
                _nightVision.DisableNightVisionItem((uid, nightVision), nightVision.User);

            UpdateGlow(ent, nightVision.User);

            if (nightVision.User == null)
                continue;

            goggles.RemainingUntilMalfunction ??= RandomMalfunctionTime(goggles);
            goggles.RemainingUntilMalfunction -= frame;
            if (goggles.RemainingUntilMalfunction > TimeSpan.Zero)
                continue;

            Malfunction(ent, (uid, nightVision));
        }
    }

    /// <summary>The visor only works attached to a helmet that's worn on someone's head.</summary>
    private EntityUid? GetWearer(EntityUid visor)
    {
        if (!_container.TryGetContainingContainer((visor, null), out var storage) ||
            storage.ID != StorageComponent.ContainerId ||
            !TryComp(storage.Owner, out HelmetAccessoryHolderComponent? holder))
        {
            return null;
        }

        var helmet = storage.Owner;
        if (!_inventory.TryGetContainingSlot((helmet, null, null), out var slot) ||
            (slot.SlotFlags & holder.Slot) == 0 ||
            !_container.TryGetContainingContainer((helmet, null), out var slotContainer))
        {
            return null;
        }

        return slotContainer.Owner;
    }

    private void UpdateAction(Entity<CMUNightVisionGogglesComponent> ent, EntityUid? wearer, bool activated)
    {
        if (ent.Comp.ActionUser != wearer)
        {
            if (ent.Comp.ActionUser != null)
                _actions.RemoveAction(ent.Comp.ActionUser.Value, ent.Comp.Action);

            if (wearer != null)
                _actions.AddAction(wearer.Value, ref ent.Comp.Action, ent.Comp.ActionId, ent);

            ent.Comp.ActionUser = wearer;
            Dirty(ent);
        }

        if (ent.Comp.Action != null)
            _actions.SetToggled(ent.Comp.Action, activated && !ent.Comp.Broken);
    }

    private void UpdateGlow(Entity<CMUNightVisionGogglesComponent> ent, EntityUid? user)
    {
        var current = ent.Comp.Glow;
        if (current != null && (TerminatingOrDeleted(current) || Transform(current.Value).ParentUid != user))
        {
            QueueDel(current);
            ent.Comp.Glow = null;
            Dirty(ent);
        }

        if (user == null || ent.Comp.Glow != null)
            return;

        ent.Comp.Glow = Spawn(ent.Comp.GlowPrototype, new EntityCoordinates(user.Value, default));
        Dirty(ent);
    }

    private void Malfunction(Entity<CMUNightVisionGogglesComponent> ent, Entity<NightVisionItemComponent> nightVision)
    {
        var user = nightVision.Comp.User;
        ent.Comp.Broken = true;
        ent.Comp.RemainingUntilMalfunction = null;
        Dirty(ent);

        _nightVision.DisableNightVisionItem(nightVision, user);
        _itemToggle.TryDeactivate(ent.Owner, null, predicted: false, showPopup: false);
        UpdateGlow(ent, null);

        if (ent.Comp.Action != null)
            _actions.SetToggled(ent.Comp.Action, false);

        if (user == null)
            return;

        _audio.PlayPvs(ent.Comp.MalfunctionSound, user.Value);
        _popup.PopupEntity(Loc.GetString("cmu-nvg-malfunction", ("goggles", ent.Owner)),
            user.Value,
            user.Value,
            PopupType.MediumCaution);
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
