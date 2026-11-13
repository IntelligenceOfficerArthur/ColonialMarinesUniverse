#nullable enable
using Content.IntegrationTests.Tests.Movement;
using Content.Server._RMC14.Scoping;
using Content.Shared.Actions.Events;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Scoping;
using Content.Shared._RMC14.Weapons.Ranged.Vulture;
using Content.Shared.CMU14.Vulture;
using Content.Shared.Movement.Components;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.CMU14.Vulture;

[TestFixture]
public sealed class CMUVultureKeyboardAimTest : MovementTest
{
    private const string Rifle = "RMCWeaponSniperM707Vulture";

    private EntityUid _rifle;
    private EntityUid _scope;

    private async Task ScopeIn()
    {
        var netRifle = await PlaceInHands(Rifle);
        _rifle = SEntMan.GetEntity(netRifle);

        EntityUid bipodUid = default;
        await Server.WaitPost(() =>
        {
            var holders = Server.System<AttachableHolderSystem>();
            var holder = SEntMan.GetComponent<AttachableHolderComponent>(_rifle);
            Assert.That(holders.TryGetAttachable((_rifle, holder), "rmc-aslot-underbarrel", out var bipod), "bipod");
            bipodUid = bipod.Owner;
            Assert.That(holders.TryGetAttachable((_rifle, holder), "rmc-aslot-rail", out var scope), "scope");
            _scope = scope.Owner;

            // Deploy the bipod through its real toggle action; the Vulture refuses to scope without it.
            var toggle = new AttachableToggleActionEvent { Performer = SPlayer };
            SEntMan.EventBus.RaiseLocalEvent(bipodUid, toggle);
        });

        await RunSeconds(8);
        await Server.WaitPost(() =>
        {
            Assert.That(SEntMan.GetComponent<AttachableToggleableComponent>(bipodUid).Active, Is.True, "bipod deployed");
            var scopeComp = SEntMan.GetComponent<ScopeComponent>(_scope);
            Assert.That(Server.System<ScopeSystem>().StartScoping((_scope, scopeComp), SPlayer), Is.Not.Null);
        });

        await Pair.RunTicksSync(10);
    }

    private bool BreathHeld() => Aim().BreathEndsAt > STiming.CurTime;

    private VultureAimComponent Aim() => SEntMan.GetComponent<VultureAimComponent>(_rifle);

    [Test]
    public async Task MovementKeysSteerCrosshairAndShiftHoldsBreath()
    {
        await ScopeIn();
        var start = SEntMan.System<SharedTransformSystem>().GetMapCoordinates(SPlayer);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<ScopingComponent>(SPlayer), "scoped in");
            Assert.That(Aim().Sniper, Is.EqualTo(SPlayer));
            Assert.That(Aim().CrosshairAdjust, Is.EqualTo(Vector2i.Zero));
            Assert.That(Aim().WindageLeftAction, Is.Null, "keyboard aim does not grant the windage action");
            Assert.That(SEntMan.GetComponent<InputMoverComponent>(SPlayer).CanMove, Is.False, "scoped sniper is rooted");
        });

        // Tap up, then right: one tile each.
        await SetKey(EngineKeyFunctions.MoveUp, BoundKeyState.Down);
        await Pair.RunTicksSync(3);
        await SetKey(EngineKeyFunctions.MoveUp, BoundKeyState.Up);
        await Pair.RunTicksSync(3);
        await SetKey(EngineKeyFunctions.MoveRight, BoundKeyState.Down);
        await Pair.RunTicksSync(3);
        await SetKey(EngineKeyFunctions.MoveRight, BoundKeyState.Up);
        await Pair.RunTicksSync(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(Aim().CrosshairAdjust, Is.EqualTo(new Vector2i(1, 1)));
            Assert.That(SEntMan.HasComponent<ScopingComponent>(SPlayer), "movement keys must not unscope");
        });

        // Holding a key repeats until the edge of the 5x5 box.
        await SetKey(EngineKeyFunctions.MoveDown, BoundKeyState.Down);
        await RunSeconds(2);
        await SetKey(EngineKeyFunctions.MoveDown, BoundKeyState.Up);
        await Pair.RunTicksSync(3);

        await Server.WaitAssertion(() =>
        {
            Assert.That(Aim().CrosshairAdjust, Is.EqualTo(new Vector2i(1, -2)), "clamped to the box");
        });

        // Shift holds breath, releasing lets go early.
        await SetKey(EngineKeyFunctions.Walk, BoundKeyState.Down);
        await Pair.RunTicksSync(3);
        await Server.WaitAssertion(() =>
            Assert.That(BreathHeld(), Is.True, "shift holds breath"));
        await SetKey(EngineKeyFunctions.Walk, BoundKeyState.Up);
        await Pair.RunTicksSync(3);
        await Server.WaitAssertion(() =>
            Assert.That(BreathHeld(), Is.False, "releasing shift lets go"));

        await Server.WaitAssertion(() =>
        {
            var now = SEntMan.System<SharedTransformSystem>().GetMapCoordinates(SPlayer);
            Assert.That(now.Position, Is.EqualTo(start.Position), "the sniper never moved");
        });
    }

    [Test]
    public async Task ThermalSightDrawsBatteryAndRootingEndsWithScope()
    {
        await ScopeIn();

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<CMUVultureThermalComponent>(_scope));
            Assert.That(SEntMan.GetComponent<NightVisionItemComponent>(_scope).User, Is.EqualTo(SPlayer),
                "thermal rides on the scope's night vision");
            Assert.That(SEntMan.GetComponent<Content.Shared.PowerCell.Components.PowerCellDrawComponent>(_scope).Enabled,
                Is.True, "thermal draws power while scoped");
        });

        await Server.WaitPost(() =>
            Server.System<ScopeSystem>().Unscope((_scope, SEntMan.GetComponent<ScopeComponent>(_scope))));
        await Pair.RunTicksSync(5);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<ScopingComponent>(SPlayer), Is.False);
            Assert.That(SEntMan.GetComponent<InputMoverComponent>(SPlayer).CanMove, Is.True, "unscoping frees the sniper");
        });
    }
}
