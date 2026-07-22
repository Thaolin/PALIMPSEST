using System.Text.Json.Nodes;
using Chronicle.Core;

const long Seed = 41_337;

Run("authored successor fixture", VerifyAuthoredFixture);
Run("WG4 subjects and semantic overlays", VerifyWorldGrammarSubjects);
Run("engagement, pending commands, and heartbeat order", VerifyEngagementAndHeartbeatOrder);
Run("Burn plans, Load, and attunement safety", VerifyExpressionRules);
Run("target preview and release revalidation", VerifyTargetsAndRevalidation);
Run("recovery, persistence, death, and replay", VerifyPersistenceDeathAndReplay);
Run("strict v6 and literal predecessor migration", VerifyPersistenceAndMigration);
Run("Goal 6B physical return, Attunement, loss, and rebuild", VerifyGoal6BPowerComesHome);
Run("Goal 6B strict v7 replay, malformed state, and migration", VerifyGoal6BPersistenceAndReplay);

Console.WriteLine("RETAINED FOUNDATION MIGRATION PASS world-grammar=0-3 home=preserved bell=preserved cairn=preserved nouns=retired");
Console.WriteLine("GOAL6A CORE ACCEPTANCE PASS retained grammar=4 combat=deterministic migration=v6-v1+pre-envelope");
Console.WriteLine("GOAL6B CORE ACCEPTANCE PASS save=7 grammar=5 power-home=physical attunement=next-only replay=deterministic");

static void VerifyAuthoredFixture()
{
    Assert(ChronicleSaveCodec.CurrentVersion == 7, "Goal 6B must write strict save envelope v7.");
    Assert(ChronicleState.Begin(Seed).WorldGrammarVersion == 5, "New Chronicles must pin World Grammar v5.");

    var burn = WordCatalogue.Get(WordIds.Burn);
    var quickly = WordCatalogue.Get(WordIds.Quickly);
    var lasting = WordCatalogue.Get(WordIds.Lasting);
    Assert(
        burn.Kind == WordKind.Verb && burn.Load == 1 &&
        burn.SupportedModifiers.SequenceEqual([WordIds.Quickly, WordIds.Lasting]) &&
        quickly.Kind == WordKind.Modifier && quickly.Load == 6 &&
        lasting.Kind == WordKind.Modifier && lasting.Load == 5,
        "Burn, Quickly, and Lasting must retain their fixed authored successor definitions.");

    var fresh = new ChronicleSimulation(Goal6AFixture());
    Assert(fresh.Apply(new ChooseAgainstIntent()).Applied, "AGAINST must select the Goal 6A Combat opening.");
    Assert(
        fresh.State.Codex.Contains(WordIds.Burn) &&
        fresh.State.Codex.Contains(WordIds.Quickly) &&
        fresh.State.Codex.Contains(WordIds.Lasting),
        "The focused Combat acceptance Chronicle must supply Burn and both accepted Modifiers.");
    Assert(
        fresh.CombatContext.Incarnation.MaximumHitPoints == 34 &&
        fresh.CombatContext.Equipment.WeaponName == "Iron Cleaver" &&
        fresh.CombatContext.Equipment.ArmorReduction == 2 &&
        fresh.CombatContext.Equipment.AccessoryName == "Copper Ward",
        "The Core snapshot must expose the fixed HP and equipment fixture without UI recomputation.");
}

static void VerifyWorldGrammarSubjects()
{
    var state = Goal6AFixture();
    var bruteAddress = WorldArea.GeneratedMireBruteAddress(Seed);
    var basaltAddress = WorldArea.GeneratedBasaltAddress(Seed);
    var area = WorldArea.Generate(
        state,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(0, -1, 7, 3));
    var brute = area.Cells.Single(cell => cell.Address == bruteAddress);
    var basalt = area.Cells.Single(cell => cell.Address == basaltAddress);

    Assert(
        brute.MireBrute is
        {
            Identity: var identity,
            HitPoints: CombatState.MireBruteMaximumHitPoints,
            IsLiving: true,
        } && identity == WorldArea.GeneratedMireBruteIdentity(Seed),
        "WG4 must generate one stable Mire Brute identity, placement, HP, and living state.");
    Assert(
        basalt.Target is
        {
            Kind: CombatTargetKind.Basalt,
            Identity: var basaltIdentity,
        } && basaltIdentity == WorldArea.GeneratedBasaltIdentity(Seed),
        "WG4 must generate the stable basalt place Target.");
    Assert(
        area.Cells.Where(cell => cell.Address.Y == 0 && cell.Address.X is >= 0 and <= 5)
            .All(cell => cell.Ground == WorldGround.Grass && cell.Feature is null),
        "The authored acceptance clearing must provide the deterministic pursuit corridor.");

    var scorched = state with
    {
        Combat = state.Combat! with
        {
            Scorch = new ScorchedGroundState(bruteAddress, 0),
        },
    };
    var overlapping = WorldArea.Generate(
        scorched,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(bruteAddress.X, bruteAddress.Y, 1, 1)).Cells.Single();
    Assert(
        overlapping.MireBrute is not null && overlapping.IsScorched,
        "WorldArea must expose a Brute state and a scorch overlay independently at one Address.");

    var oldGrammar = Goal6AFixture() with { WorldGrammarVersion = 3, Combat = null };
    var oldArea = WorldArea.Generate(
        oldGrammar,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(bruteAddress.X, bruteAddress.Y, 1, 1));
    Assert(oldArea.Cells.Single().MireBrute is null, "Older World Grammar pins must not gain the Brute retroactively.");
}

static void VerifyEngagementAndHeartbeatOrder()
{
    var slowApproach = new ChronicleSimulation(Goal6AFixture());
    slowApproach.Apply(new ChooseAgainstIntent());
    slowApproach.Apply(new MoveIncarnation(1, 0));
    slowApproach.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    var queuedApproach = slowApproach.Apply(new MoveIncarnation(1, 0));
    Assert(
        queuedApproach.Applied &&
        slowApproach.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 0) &&
        slowApproach.State.Speed == ChronicleSpeed.Paused &&
        slowApproach.CombatContext.PendingAction is { Kind: TacticalActionKind.Move },
        "Any tactical movement issued while Slow must pause and expose the pending command before contact.");

    var simulation = NewEncounter(WordIds.Lasting, openingWeapon: true);
    var context = simulation.CombatContext;
    Assert(
        simulation.State.Speed == ChronicleSpeed.Paused &&
        context.Danger.IsImmediate &&
        context.WeaponStanceActive &&
        context.MireBrute is { HitPoints: 45, IsLiving: true } &&
        context.Forecast.Count > 0,
        "Contact must apply the Engagement Plan, pause before the first hostile Heartbeat, and expose HUD data.");
    Assert(
        simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Normal)).Applied == false,
        "Immediate danger must reject unsafe Chronicle speeds.");

    var bruteAddress = context.MireBrute!.Address;
    Assert(simulation.Apply(new PrepareBurn(bruteAddress)).Applied, "Paused valid Burn must begin Preparation immediately.");
    Assert(simulation.CombatContext.Preparation is { RemainingTicks: 3 }, "Lasting Burn must expose three Preparation Heartbeats.");

    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    simulation.AdvanceOneTick();
    Assert(simulation.CombatContext.Preparation is { RemainingTicks: 2 }, "Preparation must advance on a Heartbeat.");
    Assert(simulation.CombatContext.MireBrute!.Address.X == 4, "Threatening Brute pursuit must resolve X before Y.");

    simulation.AdvanceOneTick();
    Assert(simulation.CombatContext.Preparation is { RemainingTicks: 1 }, "Preparation must remain exposed until release.");
    Assert(
        simulation.CombatContext.MireBrute!.Address.X == 3,
        "The Brute must take its second deterministic pursuit step in the authored clearing.");

    simulation.AdvanceOneTick();
    var released = simulation.CombatContext;
    var releaseEvents = released.RecentResults
        .Where(result => result.Tick == simulation.State.Tick)
        .Select(result => result.Kind)
        .ToArray();
    Assert(
        releaseEvents.Take(3).SequenceEqual(
        [
            CombatResultKind.InvocationReleased,
            CombatResultKind.BurnDamage,
            CombatResultKind.WeaponStrike,
        ]) &&
        released.MireBrute!.HitPoints == 36 &&
        simulation.State.Speed == ChronicleSpeed.Paused,
        "Heartbeat order must release Burn, apply its consequence, then strike with the ready Weapon before hostile action.");

    // Any tactical input while Slow pauses first and leaves a named command for
    // the next simulation Heartbeat.
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    var queued = simulation.Apply(new SetWeaponStance(false));
    Assert(
        queued.Applied && simulation.State.Speed == ChronicleSpeed.Paused &&
        simulation.CombatContext.PendingAction is { Kind: TacticalActionKind.SetWeaponStance },
        "A Slow tactical command must pause before it resolves and expose its pending state.");
    Assert(simulation.Apply(new CancelPendingTacticalAction()).Applied, "A paused player must be able to cancel the one pending action.");

    var occupiedState = simulation.State with
    {
        Address = simulation.State.Combat!.MireBrute.Address with
        {
            X = simulation.State.Combat.MireBrute.Address.X - 1,
        },
        Speed = ChronicleSpeed.Paused,
        Combat = simulation.State.Combat with { PendingAction = null, Preparation = null },
    };
    var occupied = new ChronicleSimulation(occupiedState);
    var occupiedAddress = occupied.State.Combat!.MireBrute.Address;
    Assert(
        !occupied.Apply(new MoveIncarnation(1, 0)).Applied &&
        occupied.State.Address != occupiedAddress,
        "The Incarnation must never enter the living Mire Brute's occupied cell.");

    var initializedAfterCombat = new ChronicleSimulation(Goal6AFixture());
    Assert(initializedAfterCombat.Apply(new ChooseAgainstIntent()).Applied,
        "The post-combat movement fixture must enter the Combat opening.");
    var defeatedState = initializedAfterCombat.State with
    {
        Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
        Speed = ChronicleSpeed.Slow,
        Combat = initializedAfterCombat.State.Combat! with
        {
            MireBrute = initializedAfterCombat.State.Combat!.MireBrute with
            {
                HitPoints = 0,
                DefeatedTick = 1,
            },
            PendingAction = null,
            Preparation = null,
        },
    };
    var afterCombat = new ChronicleSimulation(defeatedState);
    Assert(afterCombat.Apply(new MoveIncarnation(1, 0)).Applied,
        "Safe movement after combat must remain available.");
    Assert(
        afterCombat.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 0) &&
        afterCombat.State.Speed == ChronicleSpeed.Slow &&
        afterCombat.CombatContext.PendingAction is null,
        "After combat ends, one movement command must move exactly one cell without pausing or leaving a queued extra step.");
    afterCombat.AdvanceOneTick();
    Assert(
        afterCombat.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 0),
        "Resuming the post-combat Clock must not replay the already-resolved movement command.");
}

static void VerifyExpressionRules()
{
    var safe = new ChronicleSimulation(Goal6AFixture());
    safe.Apply(new ChooseAgainstIntent());

    var duplicate = safe.Apply(new AttuneExpression(WordIds.Burn, [WordIds.Quickly, WordIds.Quickly]));
    var overLinkAndLoad = safe.Apply(new AttuneExpression(WordIds.Burn, [WordIds.Lasting, WordIds.Quickly]));
    Assert(
        !duplicate.Applied && duplicate.Message.Contains("only once", StringComparison.Ordinal) &&
        !overLinkAndLoad.Applied &&
        overLinkAndLoad.Message.Contains("Link", StringComparison.Ordinal) &&
        overLinkAndLoad.Message.Contains("Load", StringComparison.Ordinal),
        "Modifiers must be unique and the fixture must reject both excess Links and shared Load.");

    Assert(safe.Apply(new AttuneExpression(WordIds.Burn, [WordIds.Quickly])).Applied, "Burn + Quickly must fit 7/8 shared Load.");
    Assert(
        safe.CombatContext.Expression is
        {
            Verb: var verb,
            Modifiers: var modifiers,
            UsedLoad: 7,
            LinkCapacity: 2,
            SharedLoadCapacity: 8,
        } && verb == WordIds.Burn && modifiers.SequenceEqual([WordIds.Quickly]),
        "The loadout snapshot must retain canonical successor Expression data.");

    Assert(safe.Apply(new AttuneExpression(WordIds.Burn, [WordIds.Lasting])).Applied, "Burn + Lasting must fit 6/8 shared Load.");
    var lasting = NewEncounter(WordIds.Lasting, openingWeapon: false);

    // Bring the Brute adjacent, wait until its next swing is one Heartbeat
    // away, then begin an exposed three-Heartbeat Preparation.
    lasting.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    Advance(lasting, 3);
    Assert(lasting.CombatContext.MireBrute!.Address.X == 3, "The interruption fixture must be adjacent.");
    lasting.Apply(new SetChronicleSpeed(ChronicleSpeed.Paused));
    Assert(lasting.Apply(new PrepareBurn(lasting.CombatContext.MireBrute!.Address)).Applied, "A fresh exposed Lasting preparation must begin.");
    lasting.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    Advance(lasting, 2);
    Assert(
        lasting.CombatContext.Preparation is null &&
        lasting.State.Speed == ChronicleSpeed.Paused &&
        lasting.CombatContext.Incarnation.HitPoints == 29,
        "A Brute swing must deal Armor-mitigated damage then interrupt still-pending Preparation.");

    var quick = NewEncounter(WordIds.Quickly, openingWeapon: false);
    Assert(quick.Apply(new PrepareBurn(quick.CombatContext.MireBrute!.Address)).Applied, "Quickly Burn must prepare while paused.");
    quick.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    quick.AdvanceOneTick();
    Assert(
        quick.CombatContext.Preparation is null &&
        quick.CombatContext.OngoingBurn is { RemainingTicks: 2 } &&
        quick.CombatContext.Recovery.RemainingTicks == 7,
        "Quickly must release in one Heartbeat with the fixed three-tick consequence and Recovery.");
}

static void VerifyTargetsAndRevalidation()
{
    var simulation = NewEncounter(WordIds.Quickly, openingWeapon: false);
    var basalt = WorldArea.GeneratedBasaltAddress(Seed);
    var before = simulation.State;
    var preview = simulation.PreviewTarget(basalt);
    var rejected = simulation.Apply(new PrepareBurn(basalt));
    Assert(
        preview.Kind == CombatTargetKind.Basalt &&
        !preview.CanBurn &&
        preview.EligibilityReason.Contains("nonflammable", StringComparison.Ordinal) &&
        !rejected.Applied && simulation.State == before,
        "Basalt preview/rejection must report factual constraints without a time, Recovery, or state cost.");

    var ready = NewEncounter(WordIds.Lasting, openingWeapon: false);
    var preparation = new BurnPreparationState(
        ready.State.IncarnationId,
        ready.State.Combat!.MireBrute.Identity,
        ready.State.Combat.MireBrute.Address,
        new LoadoutSlot(WordIds.Burn, Modifier: WordIds.Lasting),
        1);

    AssertRevalidation(
        ready.State with
        {
            Speed = ChronicleSpeed.Slow,
            Combat = ready.State.Combat! with { Preparation = preparation with { ActorIncarnationId = 99 } },
        },
        "Incarnation");
    AssertRevalidation(
        ready.State with
        {
            Address = new WorldAddress(SurfacePatch.SurfaceStratum, -20, 0),
            Speed = ChronicleSpeed.Slow,
            Combat = ready.State.Combat! with { Preparation = preparation },
        },
        "range");
    AssertRevalidation(
        ready.State with
        {
            Speed = ChronicleSpeed.Slow,
            Combat = ready.State.Combat! with
            {
                Preparation = preparation,
                MireBrute = ready.State.Combat.MireBrute with { HitPoints = 0, DefeatedTick = 0 },
            },
        },
        "Target");
    AssertRevalidation(
        ready.State with
        {
            Speed = ChronicleSpeed.Slow,
            Loadout = LoadoutState.Empty,
            Combat = ready.State.Combat! with { Preparation = preparation },
        },
        "Expression state");
}

static void VerifyPersistenceDeathAndReplay()
{
    var replayA = PlayDeterministicTranscript();
    var replayB = PlayDeterministicTranscript();
    Assert(
        ChronicleSaveCodec.Serialize(replayA.State) == ChronicleSaveCodec.Serialize(replayB.State) &&
        replayA.CombatContext.Forecast.SequenceEqual(replayB.CombatContext.Forecast),
        "The same seed and command stream must replay the same persistent state and forecast.");

    var burn = NewEncounter(WordIds.Quickly, openingWeapon: false);
    Assert(burn.Apply(new PrepareBurn(burn.CombatContext.MireBrute!.Address)).Applied, "The persistence fixture must release Burn.");
    burn.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    burn.AdvanceOneTick();
    var scorchedAddress = burn.CombatContext.Scorch!.Address;
    var woundedBrute = burn.State.Combat!.MireBrute;
    var save = ChronicleSaveCodec.Serialize(burn.State);
    var restored = ChronicleSaveCodec.Deserialize(save);
    Assert(
        restored.Combat!.Scorch?.Address == scorchedAddress &&
        restored.Combat.MireBrute == woundedBrute,
        "Scorch and Brute state must survive a strict save/load round-trip.");

    // The damage fixture makes death deterministic without adding any test-only
    // command. Ongoing fire and the Brute remain Chronicle state, body state ends.
    var mortal = new ChronicleSimulation(burn.State with
    {
        Combat = burn.State.Combat! with { IncarnationHitPoints = 5 },
        Speed = ChronicleSpeed.Slow,
    });
    AdvanceUntil(mortal, simulation => !simulation.State.HasLivingIncarnation, 12);
    Assert(
        !mortal.State.HasLivingIncarnation &&
        mortal.State.Speed == ChronicleSpeed.Paused &&
        mortal.State.Combat!.Scorch?.Address == scorchedAddress &&
        mortal.State.Combat.MireBrute.HitPoints == 33,
        "Incarnation death must pause while preserving deterministic world-fire progress, scorch, and Brute state.");
    Assert(mortal.Apply(new CreateReplacementIncarnation()).Applied, "A dead Chronicle must create its next Incarnation through the simulation seam.");
    Assert(
        mortal.State.HasLivingIncarnation &&
        mortal.CombatContext.Incarnation.HitPoints == 34 &&
        mortal.CombatContext.Equipment.WeaponName == "Iron Cleaver" &&
        mortal.CombatContext.Recovery.RemainingTicks == 0 &&
        mortal.State.Combat!.Scorch?.Address == scorchedAddress,
        "Replacement must receive fresh fixed body state without erasing persistent material consequences.");
    Assert(
        !mortal.PreviewTarget(mortal.State.Combat!.MireBrute.Address).CanBurn,
        "Target eligibility must reflect that a replacement body has no Burn Expression attuned.");

    var retreatBase = new ChronicleSimulation(Goal6AFixture());
    retreatBase.Apply(new ChooseAgainstIntent());
    retreatBase.Apply(new AttuneExpression(WordIds.Burn, [WordIds.Quickly]));
    var retreatState = retreatBase.State with
    {
        Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
        Speed = ChronicleSpeed.Paused,
        Combat = retreatBase.State.Combat! with
        {
            RecoveryRemaining = 4,
            MireBrute = retreatBase.State.Combat!.MireBrute with { Address = new WorldAddress(SurfacePatch.SurfaceStratum, 5, 0) },
        },
    };
    var retreat = new ChronicleSimulation(retreatState);
    Assert(retreat.Apply(new MoveIncarnation(-1, 0)).Applied, "A safe retreat movement must remain available during Recovery.");
    Assert(
        !retreat.CombatContext.Danger.IsImmediate && retreat.CombatContext.Recovery.CanSkipSafely,
        "Retreat must preserve Recovery and expose when safe waiting may skip.");
    var recoveryTick = retreat.State.Tick;
    Assert(retreat.Apply(new SkipRecovery()).Applied, "Safe Recovery must skip through the Core command seam.");
    Assert(
        retreat.CombatContext.Recovery.RemainingTicks == 0 && retreat.State.Tick == recoveryTick + 4,
        "Safe skip must advance exactly to Recovery completion without unrelated simulation.");

    var burningRetreat = new ChronicleSimulation(retreatState with
    {
        Combat = retreatState.Combat! with
        {
            OngoingBurn = new BurnConsequenceState(
                retreatState.Combat.MireBrute.Identity,
                CombatState.BurnDamage,
                2),
        },
    });
    Assert(
        !burningRetreat.Apply(new SkipRecovery()).Applied,
        "Recovery cannot skip across a still-meaningful ongoing Burn consequence.");

    var overflowRetreat = new ChronicleSimulation(retreatState with
    {
        Tick = long.MaxValue - retreatState.Combat!.RecoveryRemaining,
    });
    Assert(
        !overflowRetreat.CombatContext.Recovery.CanSkipSafely &&
        !overflowRetreat.Apply(new SkipRecovery()).Applied &&
        overflowRetreat.State.Tick == long.MaxValue - retreatState.Combat.RecoveryRemaining,
        "Recovery skipping must reject the exact boundary that would consume the reserved next Chronicle tick.");
}

static void VerifyPersistenceAndMigration()
{
    var current = NewEncounter(WordIds.Quickly, openingWeapon: true);
    var json = ChronicleSaveCodec.Serialize(current.State);
    var node = JsonNode.Parse(json)!.AsObject();
    Assert(node["Version"]!.GetValue<int>() == 7, "Current serialization must write envelope v7.");
    Assert(node["Chronicle"]!["Combat"] is not null, "v7 must persist the bounded combat state.");
    var successorChronicle = node["Chronicle"]!.AsObject();
    Assert(
        !successorChronicle.ContainsKey("Study") &&
        !successorChronicle.ContainsKey("FirstConflict") &&
        !successorChronicle.ContainsKey("LooseStoneAddress") &&
        !successorChronicle.ContainsKey("BellAddress") &&
        !successorChronicle.ContainsKey("Home") &&
        !successorChronicle["Loadout"]!.AsObject().ContainsKey("Noun"),
        "Fresh v7 must serialize only the successor Loadout shape, not predecessor Study/Noun/FirstConflict fields.");
    Assert(!json.Contains("RecentResults", StringComparison.Ordinal), "Message Log history must not enter v7 persistence.");
    Assert(ChronicleSaveCodec.Deserialize(json) == current.State, "Strict v7 must round-trip exact durable successor state.");

    node["Unexpected"] = true;
    AssertThrows(() => ChronicleSaveCodec.Deserialize(node.ToJsonString()), "Strict v7 must reject unexpected envelope fields.");

    var nounNode = JsonNode.Parse(json)!.AsObject();
    nounNode["Chronicle"]!["Codex"]!["Words"]!.AsArray().Add("word.stone");
    AssertThrows(
        () => ChronicleSaveCodec.Deserialize(nounNode.ToJsonString()),
        "Strict v7 must reject predecessor Noun knowledge even though literal old-save readers still recognize it.");

    var fabricatedV5 = JsonNode.Parse(LiteralVersion5())!.AsObject();
    fabricatedV5["Chronicle"]!["WorldGrammarVersion"] = 4;
    AssertThrows(
        () => ChronicleSaveCodec.Deserialize(fabricatedV5.ToJsonString()),
        "Literal v5 migration must reject fabricated successor World Grammar state.");

    foreach (var predecessorPin in new[] { 0, 1, 2 })
    {
        var oldPinV5 = JsonNode.Parse(LiteralVersion5())!.AsObject();
        oldPinV5["Chronicle"]!["WorldGrammarVersion"] = predecessorPin;
        oldPinV5["Chronicle"]!["Home"] = null;
        oldPinV5["Chronicle"]!["FirstConflict"] = null;
        oldPinV5["Chronicle"]!["Codex"]!["Words"] =
            JsonNode.Parse("[\"word.fly\",\"word.stone\",\"word.bell\"]");
        oldPinV5["Chronicle"]!["Study"] = JsonNode.Parse(
            "{\"Understanding\":[],\"ActiveSourceId\":null,\"ActiveWord\":null}");
        var migratedOldPin = ChronicleSaveCodec.Deserialize(oldPinV5.ToJsonString());
        Assert(
            migratedOldPin.WorldGrammarVersion == predecessorPin && migratedOldPin.Combat is null,
            $"Literal v5 must retain valid predecessor World Grammar pin {predecessorPin} without adding Goal 6A combat.");
    }

    var v5 = ChronicleSaveCodec.Deserialize(LiteralVersion5());
    AssertRetiredPredecessor(v5, 3, "literal v5");
    var migratedV5Json = ChronicleSaveCodec.Serialize(v5);
    var migratedV5Node = JsonNode.Parse(migratedV5Json)!.AsObject();
    Assert(
        !migratedV5Json.Contains("\"FirstConflict\"", StringComparison.Ordinal) &&
        !migratedV5Json.Contains("\"Study\"", StringComparison.Ordinal) &&
        !migratedV5Json.Contains("\"Noun\"", StringComparison.Ordinal) &&
        migratedV5Node["Chronicle"]!["RetainedDurables"]!["RivenCairn"] is not null,
        "Migrated v5 must retain only neutral durable predecessor material, never predecessor gameplay schema.");

    var v4Node = JsonNode.Parse(LiteralVersion5())!.AsObject();
    v4Node["Version"] = 4;
    v4Node["Chronicle"]!.AsObject().Remove("BellAddress");
    var v4 = ChronicleSaveCodec.Deserialize(v4Node.ToJsonString());
    AssertRetiredPredecessor(v4, 3, "literal v4");

    var v3Node = JsonNode.Parse(v4Node.ToJsonString())!.AsObject();
    v3Node["Version"] = 3;
    v3Node["Chronicle"]!["WorldGrammarVersion"] = 2;
    v3Node["Chronicle"]!.AsObject().Remove("FirstConflict");
    v3Node["Chronicle"]!["Codex"]!["Words"] =
        JsonNode.Parse("[\"word.fly\",\"word.found\",\"word.stone\",\"word.bell\"]");
    var v3 = ChronicleSaveCodec.Deserialize(v3Node.ToJsonString());
    AssertRetiredPredecessor(v3, 2, "literal v3");

    var v2Node = JsonNode.Parse(v3Node.ToJsonString())!.AsObject();
    v2Node["Version"] = 2;
    v2Node["Chronicle"]!.AsObject().Remove("Home");
    v2Node["Chronicle"]!["Codex"]!["Words"] =
        JsonNode.Parse("[\"word.fly\",\"word.stone\",\"word.bell\"]");
    var v2 = ChronicleSaveCodec.Deserialize(v2Node.ToJsonString());
    AssertRetiredPredecessor(v2, 2, "literal v2");

    var v1 = ChronicleSaveCodec.Deserialize(LiteralVersion1());
    Assert(
        v1.WorldGrammarVersion == 0 && v1.Codex.Contains(WordIds.Fly) &&
        !v1.Codex.Contains(WordIds.Stone) && v1.Combat is null,
        "Literal v1 must retain its old grammar pin and explicitly retire predecessor Nouns.");

    var preEnvelope = ChronicleSaveCodec.Deserialize(LiteralPreEnvelope());
    Assert(
        preEnvelope.WorldGrammarVersion == 0 && preEnvelope.Combat is null &&
        ChronicleSaveCodec.Serialize(preEnvelope).Contains("\"Version\": 7", StringComparison.Ordinal),
        "Pre-envelope input must remain accepted and rewrite through strict v7 without a Brute.");
}

static void VerifyGoal6BPowerComesHome()
{
    var testingStart = new ChronicleSimulation(ChronicleState.Begin(Seed));
    Assert(testingStart.Apply(new ChooseHereIntent()).Applied,
        "The neutral testing start must retain Home without choosing a combat path.");
    var unreadPrimer = testingStart.PowerComesHomeContext;
    Assert(
        testingStart.State.Intent == OpeningIntent.Here &&
        !testingStart.State.Codex.Contains(WordIds.Burn) &&
        unreadPrimer.BurnPrimer is
        {
            Address: var primerAddress,
            IsRead: false,
        } &&
        primerAddress == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 2) &&
        unreadPrimer.Summary.Contains("CHECKLIST · LEARN BURN", StringComparison.Ordinal) &&
        unreadPrimer.Actions.Single(action => action.Id == "read-primer").Available,
        "The neutral start must expose one unread Burn Primer directly north of Home and no implicit Burn path reward.");
    var primerCell = WorldArea.Generate(
        testingStart.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(0, 2, 1, 1)).Cells.Single();
    Assert(
        primerCell.BurnPrimer is { IsRead: false } &&
        primerCell.BurnPrimer.Identity == unreadPrimer.BurnPrimer.Identity &&
        primerCell.Ground == WorldGround.Soil &&
        primerCell.Feature is null &&
        primerCell.MotifIdentity == "surface-burn-primer-clearing",
        "The unread Burn Primer must be a stable visible World subject on a clear-soil cell at its Core-owned Address.");
    AssertRoundTrip(testingStart.State, "unread Burn Primer");
    var primerTick = testingStart.State.Tick;
    Assert(testingStart.Apply(new ReadBurnPrimer()).Applied &&
           testingStart.State.Tick == primerTick &&
           testingStart.State.Codex.Contains(WordIds.Burn) &&
           testingStart.State.Codex.Contains(WordIds.Quickly) &&
           testingStart.State.Codex.Contains(WordIds.Lasting),
        "The nearby Burn Primer must teach the complete Goal 6B test Expression without spending a Heartbeat.");
    Assert(
        testingStart.PowerComesHomeContext.BurnPrimer.IsRead &&
        testingStart.PowerComesHomeContext.Summary.Contains("CHECKLIST · GET THE GOLD LODE", StringComparison.Ordinal) &&
        !testingStart.PowerComesHomeContext.Actions.Single(action => action.Id == "read-primer").Available,
        "Reading the Primer must persist its state and switch immediately to the Lode checklist.");
    AssertRoundTrip(testingStart.State, "read Burn Primer");

    var simulation = NewGoal6BFixture();
    var context = simulation.PowerComesHomeContext;
    Assert(
        simulation.State.Address == ChronicleState.AcceptedHomeFixtureAddress &&
        context.SeamAddress == new WorldAddress(SurfacePatch.SurfaceStratum, 8, 3) &&
        context.SeamIdentity == "place.singing-seam.41337" &&
        context.Lode.Identity == "resource.resonant-lode.41337" &&
        context.Lode.OriginAddress == context.SeamAddress &&
        context.ResonatorSite == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 3),
        "WG5 must expose the exact generated Seam, persistent Lode origin, Home, and sole eligible Source site.");
    AssertChecklist(context, "embedded", "GET THE GOLD LODE", "P — Extract", "STOPS:", "LOCKS:");
    var broad = WorldArea.Generate(
        simulation.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(6, 1, 5, 5));
    var overlap = WorldArea.Generate(
        simulation.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(8, 3, 2, 2));
    Assert(
        broad.Cells.Single(cell => cell.Address == context.SeamAddress).SingingSeam?.Identity == context.SeamIdentity &&
        overlap.Cells.Single(cell => cell.Address == context.SeamAddress).ResonantLode?.Identity == context.Lode.Identity,
        "Differently bounded and ordered queries must reproduce the exact Seam and Lode identities.");
    var sourceSite = WorldArea.Generate(
        simulation.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(context.ResonatorSite!.Value.X, context.ResonatorSite.Value.Y, 1, 1)).Cells.Single();
    Assert(
        sourceSite.IsHearthResonatorSite &&
        sourceSite.Ground == WorldGround.Soil &&
        sourceSite.Feature is null &&
        sourceSite.MotifIdentity == "surface-home-source-foundation",
        "WG5 must reserve the outlined Resonator site as a clear supported foundation, not place the Source inside a mountain feature.");

    var combinedBefore = simulation.State;
    var tooHeavy = simulation.Apply(new AttuneExpression(
        WordIds.Burn,
        [WordIds.Lasting, WordIds.Quickly]));
    Assert(
        !tooHeavy.Applied &&
        tooHeavy.Message.Contains("Needs 12 Load", StringComparison.Ordinal) &&
        tooHeavy.Message.Contains("capacity is 8", StringComparison.Ordinal) &&
        simulation.State == combinedBefore,
        "The previously impossible combined Expression must reject atomically with the authored capacity explanation.");

    simulation = At(simulation, new WorldAddress(SurfacePatch.SurfaceStratum, 7, 3));
    Assert(simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Extract)).Applied,
        "Extraction must begin cardinally adjacent to the Singing Seam.");
    var paused = simulation.State;
    simulation.AdvanceClockPulse();
    Assert(simulation.State == paused && simulation.PowerComesHomeContext.Commitment is { CompletedTicks: 0, WaitingForHeartbeat: true },
        "A paused extraction must stay visibly queued at 0/2.");
    AssertChecklist(simulation.PowerComesHomeContext, "extracting", "EXTRACT LODE 0/2", "SPACE", "NEXT", "LOCKS:");
    AdvanceActive(simulation, 1);
    Assert(simulation.State.PowerHome is { ExtractionProgress: 1, Commitment.CompletedTicks: 1 },
        "The first active extraction Heartbeat must persist represented 1/2 progress.");
    AssertRoundTrip(simulation.State, "extraction 1/2");
    simulation.AdvanceOneTick();
    Assert(
        simulation.State.PowerHome is
        {
            ExtractionProgress: 2,
            Commitment: null,
            Lode.Disposition: ResonantLodeDisposition.Loose,
        } &&
        simulation.PowerComesHomeContext.SeamIsEmpty,
        "The second extraction Heartbeat must leave one loose Lode and a persistent empty Seam.");
    AssertRoundTrip(simulation.State, "loose Lode");
    AssertChecklist(simulation.PowerComesHomeContext, "loose", "LIFT GOLD LODE", "P — Lift", "Carry it to HOME", "CARRYING LOCKS:");

    var extractionExit = At(NewGoal6BFixture(), new WorldAddress(SurfacePatch.SurfaceStratum, 7, 3));
    Assert(extractionExit.Apply(new BeginPowerCommitment(PowerCommitmentKind.Extract)).Applied,
        "The auto-pause regression fixture must begin extraction while paused.");
    AdvanceActive(extractionExit, 2);
    Assert(
        extractionExit.State.Speed == ChronicleSpeed.Slow &&
        extractionExit.PowerComesHomeContext.Commitment is null &&
        !extractionExit.CombatContext.Danger.IsImmediate &&
        extractionExit.State.Combat is { EngagementActive: false } &&
        extractionExit.CombatContext.PendingAction is null &&
        extractionExit.CombatContext.MireBrute is { IsLiving: true },
        "Completed extraction must leave a safe, disengaged, active post-commitment movement fixture with no stale pending action.");
    Assert(extractionExit.Apply(new MoveIncarnation(0, 1)).Applied,
        "One safe movement command after extraction must remain available.");
    Assert(
        extractionExit.State.Address == new WorldAddress(SurfacePatch.SurfaceStratum, 7, 4) &&
        extractionExit.State.Speed == ChronicleSpeed.Slow &&
        extractionExit.CombatContext.PendingAction is null,
        "Completing a Goal 6B commitment must release its auto-pause ownership; the next safe move must resolve once without pausing or queuing.");

    var liftTick = simulation.State.Tick;
    Assert(simulation.Apply(new LiftResonantLode()).Applied &&
           simulation.State.Tick == liftTick &&
           simulation.State.PowerHome!.Lode is
           {
               Disposition: ResonantLodeDisposition.Carried,
               Address: null,
               CarrierIncarnationId: 1,
           },
        "Lift must spend no Heartbeat and move the Lode exclusively onto the living Incarnation.");
    Assert(!simulation.Apply(new SetWeaponStance(true)).Applied,
        "The Iron Cleaver must reject while the Lode occupies both hands.");
    AssertChecklist(simulation.PowerComesHomeContext, "carried", "CARRY LODE HOME", "P — Build", "STOPS WORK:", "CARRYING LOCKS:");
    Assert(!simulation.Apply(new AttuneExpression(WordIds.Burn, [WordIds.Quickly])).Applied,
        "Focused Attunement must reject while carrying the Lode.");
    var carriedState = simulation.State;
    Assert(!simulation.Apply(new LiftResonantLode()).Applied && simulation.State == carriedState,
        "A second Lift must reject without duplicating or moving the carried Lode.");
    var burnWhileCarrying = simulation.Apply(new PrepareBurn(simulation.State.Combat!.MireBrute.Address));
    Assert(!burnWhileCarrying.Applied &&
           burnWhileCarrying.Message.Contains("Set down the Resonant Lode", StringComparison.Ordinal) &&
           simulation.State == carriedState,
        "Burn Preparation must reject precisely and atomically while carrying.");
    var dropFixture = new ChronicleSimulation(carriedState);
    Assert(dropFixture.Apply(new SetDownResonantLode()).Applied &&
           dropFixture.State.PowerHome!.Lode is
           { Disposition: ResonantLodeDisposition.Loose, Address: var droppedAt } &&
           droppedAt == carriedState.Address,
        "Set Down must create exactly one loose Lode at the carrier Address without spending a Heartbeat.");
    AssertRoundTrip(dropFixture.State, "dropped Lode");
    var deathFixture = new ChronicleSimulation(carriedState with { Address = SkyStratum.LandmarkAddress });
    Assert(deathFixture.Apply(new EndIncarnationAtBell()).Applied &&
           deathFixture.State.PowerHome!.Lode is
           {
               Disposition: ResonantLodeDisposition.Loose,
               Address: var deathDrop,
               CarrierIncarnationId: null,
           } && deathDrop == SkyStratum.LandmarkAddress &&
           deathFixture.State.Attunement is null,
        "Carrier death must drop exactly one physical Lode at the death Address before replacement.");
    Assert(deathFixture.Apply(new CreateReplacementIncarnation()).Applied &&
           deathFixture.State.PowerHome!.Lode.Address == SkyStratum.LandmarkAddress,
        "Replacement must receive no remote Lode copy; the death drop remains in the world.");
    AssertRoundTrip(simulation.State, "carried Lode");

    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Normal));
    Assert(simulation.Apply(new MoveIncarnation(-1, 0)).Applied &&
           simulation.PowerComesHomeContext.Lode.Address == simulation.State.Address,
        "The physical Lode must follow each resolved carrier move through the semantic snapshot.");
    simulation = At(simulation, ChronicleState.AcceptedHomeFixtureAddress);
    var noHome = new ChronicleSimulation(simulation.State with { Home = null });
    var noHomeBefore = noHome.State;
    Assert(!noHome.Apply(new BeginPowerCommitment(PowerCommitmentKind.Build)).Applied && noHome.State == noHomeBefore,
        "Build must reject atomically when Home does not exist.");
    var wrongSite = At(new ChronicleSimulation(simulation.State), new WorldAddress(SurfacePatch.SurfaceStratum, 4, 4));
    var wrongSiteBefore = wrongSite.State;
    Assert(!wrongSite.Apply(new BeginPowerCommitment(PowerCommitmentKind.Build)).Applied && wrongSite.State == wrongSiteBefore,
        "Build must reject atomically away from the exact Home-relative site.");
    var missingCarry = new ChronicleSimulation(dropFixture.State with { Address = ChronicleState.AcceptedHomeFixtureAddress });
    var missingCarryBefore = missingCarry.State;
    Assert(!missingCarry.Apply(new BeginPowerCommitment(PowerCommitmentKind.Build)).Applied && missingCarry.State == missingCarryBefore,
        "Build must reject a loose, not-carried Lode without consuming matter.");
    var dangerousCombat = simulation.State.Combat! with
    {
        MireBrute = simulation.State.Combat.MireBrute with
        {
            Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 2),
        },
    };
    var danger = new ChronicleSimulation(simulation.State with { Combat = dangerousCombat });
    var dangerBefore = danger.State;
    Assert(!danger.Apply(new BeginPowerCommitment(PowerCommitmentKind.Build)).Applied && danger.State == dangerBefore,
        "Build must reject immediate danger without spending time or consuming the Lode.");
    Assert(simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Build)).Applied,
        "The carried Lode must begin the sole Home-relative Source build.");
    var secondCommitmentBefore = simulation.State;
    Assert(!simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Extract)).Applied &&
           simulation.State == secondCommitmentBefore,
        "A second commitment must reject without replacing the active one.");
    AdvanceActive(simulation, 1);
    Assert(simulation.State.PowerHome!.Resonator is
           { Phase: HearthResonatorPhase.UnderConstruction, Progress: 1 },
        "The first construction Heartbeat must show a physical 1/3 foundation.");
    AssertRoundTrip(simulation.State, "construction 1/3");
    Assert(simulation.Apply(new CancelPowerCommitment()).Applied &&
           simulation.State.PowerHome!.Resonator is { Progress: 1 } &&
           simulation.State.PowerHome.Commitment is null,
        "Cancelling Build must preserve the committed Lode and material progress.");
    AssertChecklist(simulation.PowerComesHomeContext, "construction", "FINISH RESONATOR", "Resume Build (2 Heartbeats)", "STOPS:", "LOCKS:");
    Assert(simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Build)).Applied,
        "Build must resume the same represented foundation.");
    AdvanceActive(simulation, 1);
    AssertRoundTrip(simulation.State, "construction 2/3");
    simulation.AdvanceOneTick();
    Assert(
        simulation.State.PowerHome!.Resonator is
        { Phase: HearthResonatorPhase.Intact, Progress: 3 } &&
        simulation.PowerComesHomeContext.Attunement.NextAttunementCapacity == 12 &&
        simulation.PowerComesHomeContext.Attunement.CapacityAtLastAttunement == 8 &&
        simulation.PowerComesHomeContext.Attunement.CurrentUsedLoad == 0,
        "Source completion must expose future capacity 12 without remotely changing the current Loadout or last ceiling.");
    AssertRoundTrip(simulation.State, "intact Source");
    AssertChecklist(simulation.PowerComesHomeContext, "intact before Attunement", "USE NEW LOAD", "G — Attune", "NEXT", "BLOCKED BY:");

    Assert(simulation.Apply(new AttuneExpression(
               WordIds.Burn,
               [WordIds.Lasting, WordIds.Quickly])).Applied,
        "The intact Source must enable the canonical combined Expression at the next explicit Attunement.");
    Assert(
        simulation.State.ActiveLoadout[0].Modifiers.SequenceEqual([WordIds.Quickly, WordIds.Lasting]) &&
        simulation.CombatContext.Expression is
        { UsedLoad: 12, SharedLoadCapacity: 12, UsedLinks: 3, LinkCapacity: 3 } &&
        simulation.State.Attunement is { Capacity: 12, Tick: var attunedTick } &&
        attunedTick == simulation.State.Tick,
        "Combined Burn must be order-independent, canonical, three Links, Load 12, and record the exact Attunement tick.");
    AssertChecklist(simulation.PowerComesHomeContext, "intact after Attunement", "TEST SOURCE LOSS", "CURRENT Loadout uses 12", "P — Dismantle", "LOCKS:");

    Assert(simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Dismantle)).Applied,
        "Controlled Dismantle must be available at the intact Source.");
    AdvanceActive(simulation, 1);
    Assert(
        simulation.State.PowerHome!.Resonator?.Phase == HearthResonatorPhase.Damaged &&
        simulation.PowerComesHomeContext.Attunement.NextAttunementCapacity == 12,
        "Dismantling Heartbeat one must be visibly damaged while still contributing +4.");
    AssertChecklist(simulation.PowerComesHomeContext, "dismantling", "DISMANTLE RESONATOR 1/2", "NEXT Attunement falls to 8", "STOPS:", "LOCKS:");
    AssertRoundTrip(simulation.State, "damaged Source");
    simulation.AdvanceOneTick();
    var destroyed = simulation.State;
    Assert(
        destroyed.PowerHome!.Resonator?.Phase == HearthResonatorPhase.Destroyed &&
        destroyed.PowerHome.Lode is
        { Disposition: ResonantLodeDisposition.Loose, Address: var exposed } &&
        exposed == new WorldAddress(SurfacePatch.SurfaceStratum, 1, 3) &&
        simulation.PowerComesHomeContext.Attunement is
        { CurrentUsedLoad: 12, CapacityAtLastAttunement: 12, NextAttunementCapacity: 8 },
        "Dismantling Heartbeat two must expose the same Lode, lower only future capacity, and retain current 12/12.");
    AssertRoundTrip(destroyed, "destroyed Source");
    AssertChecklist(simulation.PowerComesHomeContext, "destroyed", "REBUILD POWER", "NEXT Attunement 8", "P — Rebuild", "LOCKS:");
    var beforeRejectedReattune = simulation.State;
    var rejectedAfterDestruction = simulation.Apply(
        new AttuneExpression(WordIds.Burn, [WordIds.Quickly, WordIds.Lasting]));
    Assert(!rejectedAfterDestruction.Applied &&
           rejectedAfterDestruction.Message.Contains("Hearth Resonator is destroyed", StringComparison.Ordinal) &&
           simulation.State == beforeRejectedReattune,
        "A new twelve-Load Attunement must reject after destruction without disabling the current Expression.");
    simulation = At(simulation, new WorldAddress(SurfacePatch.SurfaceStratum, 2, 0));
    var preview = simulation.PreviewTarget(simulation.State.Combat!.MireBrute.Address);
    Assert(preview.CanBurn && preview.PreparationTicks == 1 && preview.ConsequenceTicks == 6 && preview.RecoveryTicks == 8,
        "The grandfathered combined Expression must still preview Quickly, Lasting, and Burn timings after Source loss.");
    simulation.Apply(new PrepareBurn(simulation.State.Combat.MireBrute.Address));
    AdvanceActive(simulation, 1);
    Assert(simulation.CombatContext.RecentResults.Any(result => result.Kind == CombatResultKind.InvocationReleased),
        "The grandfathered combined Expression must release normally after Source destruction.");

    simulation = new ChronicleSimulation(destroyed with { Address = SkyStratum.LandmarkAddress });
    Assert(simulation.Apply(new EndIncarnationAtBell()).Applied &&
           simulation.State.Attunement is null && simulation.State.ActiveLoadout[0].IsEmpty,
        "Death must end the body-bound twelve-Load Attunement without changing Source durables.");
    Assert(simulation.Apply(new CreateReplacementIncarnation()).Applied &&
           simulation.PowerComesHomeContext.Attunement.NextAttunementCapacity == 8 &&
           !simulation.Apply(new AttuneExpression(WordIds.Burn, [WordIds.Quickly, WordIds.Lasting])).Applied,
        "A replacement must require fresh Attunement and reject the combined Expression while the Source is destroyed.");

    simulation = At(simulation, ChronicleState.AcceptedHomeFixtureAddress);
    Assert(simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Rebuild)).Applied,
        "Rebuild must commit the same exposed Lode at the same Source site.");
    AdvanceActive(simulation, 1);
    AssertRoundTrip(simulation.State, "rebuilding 1/3");
    Assert(simulation.Apply(new CancelPowerCommitment()).Applied,
        "Rebuild cancellation must preserve represented progress.");
    AssertChecklist(simulation.PowerComesHomeContext, "rebuilding", "FINISH REBUILD", "Resume Rebuild (2 Heartbeats)", "NEXT", "STOPS:");
    Assert(simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Rebuild)).Applied,
        "Rebuild must resume represented progress after cancellation.");
    AdvanceActive(simulation, 1);
    AssertRoundTrip(simulation.State, "rebuilding 2/3");
    simulation.AdvanceOneTick();
    Assert(simulation.State.PowerHome!.Resonator?.Phase == HearthResonatorPhase.Intact &&
           simulation.PowerComesHomeContext.Attunement.NextAttunementCapacity == 12 &&
           simulation.State.Attunement is null,
        "Rebuild must restore only future capacity without automatic replacement Loadout changes.");
    AssertChecklist(simulation.PowerComesHomeContext, "rebuilt before Attunement", "USE NEW LOAD", "G — Attune", "NEXT", "BLOCKED BY:");
    Assert(simulation.Apply(new AttuneExpression(WordIds.Burn, [WordIds.Quickly, WordIds.Lasting])).Applied,
        "Explicit post-rebuild Attunement must restore the combined Expression.");
}

static void VerifyGoal6BPersistenceAndReplay()
{
    var start = NewGoal6BFixture();
    start = At(start, new WorldAddress(SurfacePatch.SurfaceStratum, 7, 3));
    start.Apply(new BeginPowerCommitment(PowerCommitmentKind.Extract));
    AdvanceActive(start, 1);
    var split = new ChronicleSimulation(ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(start.State)));
    start.AdvanceOneTick();
    split.AdvanceOneTick();
    Assert(
        ChronicleSaveCodec.Serialize(start.State) == ChronicleSaveCodec.Serialize(split.State),
        "An uninterrupted and save-split Heartbeat stream must end in byte-equivalent canonical v7 state.");

    var interrupted = NewGoal6BFixture();
    interrupted = At(interrupted, new WorldAddress(SurfacePatch.SurfaceStratum, 7, 3));
    Assert(interrupted.Apply(new BeginPowerCommitment(PowerCommitmentKind.Extract)).Applied,
        "The hostile-interruption fixture must begin extraction while safe.");
    var hostileCombat = interrupted.State.Combat! with
    {
        MireBrute = interrupted.State.Combat.MireBrute with
        {
            Address = new WorldAddress(SurfacePatch.SurfaceStratum, 7, 2),
            SwingTicksRemaining = 1,
        },
    };
    interrupted = new ChronicleSimulation(interrupted.State with
    {
        Combat = hostileCombat,
        Speed = ChronicleSpeed.Slow,
    });
    interrupted.AdvanceOneTick();
    Assert(interrupted.State.PowerHome is { ExtractionProgress: 1, Commitment: null } &&
           interrupted.CombatContext.RecentResults.Any(result =>
               result.Kind == CombatResultKind.PowerHome &&
               result.Text.Contains("damage interrupts", StringComparison.Ordinal)),
        "Hostile damage must interrupt work after preserving the represented material step.");
    AssertRoundTrip(interrupted.State, "hostile-interrupted extraction 1/2");

    var v6 = ChronicleSaveCodec.Deserialize(Version6Fixture());
    Assert(v6.WorldGrammarVersion == 4 && v6.PowerHome is null &&
           v6.Attunement is { Capacity: 8 },
        "Literal strict v6 must migrate through the one current runtime without acquiring a Lode or Source.");

    var valid = JsonNode.Parse(ChronicleSaveCodec.Serialize(NewGoal6BFixture().State))!.AsObject();
    valid["Chronicle"]!["PowerHome"]!["Lode"]!["Disposition"] = (int)ResonantLodeDisposition.Carried;
    valid["Chronicle"]!["PowerHome"]!["Lode"]!["Address"] = JsonNode.Parse(
        "{\"Stratum\":\"surface\",\"X\":8,\"Y\":3}");
    valid["Chronicle"]!["PowerHome"]!["Lode"]!["CarrierIncarnationId"] = 99;
    AssertThrows(
        () => ChronicleSaveCodec.Deserialize(valid.ToJsonString()),
        "Strict v7 must reject dual-location and invalid-carrier Lode state.");

    var badProgress = JsonNode.Parse(ChronicleSaveCodec.Serialize(NewGoal6BFixture().State))!.AsObject();
    badProgress["Chronicle"]!["PowerHome"]!["ExtractionProgress"] = 3;
    AssertThrows(
        () => ChronicleSaveCodec.Deserialize(badProgress.ToJsonString()),
        "Strict v7 must reject extraction progress outside authored bounds.");

    var badAttunement = JsonNode.Parse(ChronicleSaveCodec.Serialize(NewGoal6BFixture().State))!.AsObject();
    badAttunement["Chronicle"]!["Attunement"]!["Capacity"] = 99;
    AssertThrows(
        () => ChronicleSaveCodec.Deserialize(badAttunement.ToJsonString()),
        "Strict v7 must reject impossible recorded Attunement capacity.");

    var unexplainedTwelve = JsonNode.Parse(ChronicleSaveCodec.Serialize(NewGoal6BFixture().State))!.AsObject();
    unexplainedTwelve["Chronicle"]!["Attunement"]!["Capacity"] = 12;
    AssertThrows(
        () => ChronicleSaveCodec.Deserialize(unexplainedTwelve.ToJsonString()),
        "Strict v7 must reject a twelve-Load Attunement with no historical Source.");

    var sourceWithoutLode = JsonNode.Parse(ChronicleSaveCodec.Serialize(NewGoal6BFixture().State))!.AsObject();
    sourceWithoutLode["Chronicle"]!["PowerHome"]!["Resonator"] = JsonNode.Parse(
        """
        {
          "Identity": "source.hearth-resonator.41337",
          "Address": { "Stratum": "surface", "X": 1, "Y": 3 },
          "Phase": 2,
          "Progress": 3
        }
        """);
    AssertThrows(
        () => ChronicleSaveCodec.Deserialize(sourceWithoutLode.ToJsonString()),
        "Strict v7 must reject a Source whose Lode remains embedded at the Seam.");

    var badCommitment = JsonNode.Parse(ChronicleSaveCodec.Serialize(interrupted.State))!.AsObject();
    badCommitment["Chronicle"]!["PowerHome"]!["Commitment"] = JsonNode.Parse(
        """
        {
          "Kind": 1,
          "ActorIncarnationId": 1,
          "SubjectIdentity": "resource.wrong",
          "Address": { "Stratum": "surface", "X": 8, "Y": 3 },
          "CompletedTicks": 1,
          "TotalTicks": 2
        }
        """);
    AssertThrows(
        () => ChronicleSaveCodec.Deserialize(badCommitment.ToJsonString()),
        "Strict v7 must reject a commitment whose subject identity disagrees with represented matter.");

    var wg4Area = WorldArea.Generate(
        v6,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(8, 3, 1, 1));
    Assert(wg4Area.Cells[0].SingingSeam is null && wg4Area.Cells[0].ResonantLode is null,
        "WG4 and older pins must never gain the WG5 Seam or Lode retroactively.");
}

static ChronicleSimulation NewGoal6BFixture()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(Seed));
    Assert(simulation.Apply(new ChooseHereIntent()).Applied,
        "The WG5 acceptance Chronicle must use the neutral Home testing start.");
    Assert(simulation.Apply(new ReadBurnPrimer()).Applied,
        "The WG5 acceptance Chronicle must acquire Burn from the physical Primer, not an opening path.");
    return simulation;
}

static ChronicleSimulation At(ChronicleSimulation simulation, WorldAddress address) =>
    new(simulation.State with { Address = address, Speed = ChronicleSpeed.Paused });

static void AdvanceActive(ChronicleSimulation simulation, int ticks)
{
    Assert(simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow)).Applied ||
           simulation.State.Speed == ChronicleSpeed.Slow,
        "The fixture must be able to resume at Slow speed.");
    for (var index = 0; index < ticks; index++)
    {
        simulation.AdvanceOneTick();
    }
}

static void AssertRoundTrip(ChronicleState state, string phase)
{
    Assert(ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(state)) == state,
        $"Strict v7 must round-trip {phase} exactly.");
}

static void AssertChecklist(PowerComesHomeContextSnapshot context, string stage, params string[] required)
{
    var lines = context.Summary.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    Assert(
        context.Summary.StartsWith("CHECKLIST · ", StringComparison.Ordinal) &&
        lines.Length <= 5 &&
        required.All(item => context.Summary.Contains(item, StringComparison.Ordinal)),
        $"Goal 6B {stage} guidance must be one concise state checklist with no more than five lines.");
}

static string Version6Fixture() =>
    """
    {
      "Version": 6,
      "Chronicle": {
        "Seed": 41337,
        "Tick": 0,
        "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
        "Speed": 2,
        "Intent": 3,
        "Codex": { "Words": ["word.burn", "word.quickly", "word.lasting"] },
        "Loadout": { "Verb": "word.burn", "Modifier": "word.quickly" },
        "IncarnationId": 1,
        "IncarnationLife": 0,
        "WorldGrammarVersion": 4,
        "Combat": {
          "IncarnationHitPoints": 34,
          "Equipment": {
            "WeaponIdentity": "equipment.iron-cleaver",
            "WeaponName": "Iron Cleaver",
            "ArmorIdentity": "equipment.quilted-jack",
            "ArmorName": "Quilted Jack",
            "AccessoryIdentity": "equipment.copper-ward",
            "AccessoryName": "Copper Ward",
            "MaximumHitPointBonus": 4,
            "PhysicalDamageReduction": 2
          },
          "EngagementPlan": { "OpenWithWeaponStance": false },
          "WeaponStanceActive": false,
          "WeaponTicksUntilReady": 0,
          "EngagementActive": false,
          "MireBrute": {
            "Identity": "subject.mire-brute.41337",
            "OriginAddress": { "Stratum": "surface", "X": 5, "Y": 0 },
            "Address": { "Stratum": "surface", "X": 5, "Y": 0 },
            "HitPoints": 45,
            "SwingTicksRemaining": 3,
            "DefeatedTick": null
          },
          "PendingAction": null,
          "Preparation": null,
          "OngoingBurn": null,
          "RecoveryRemaining": 0,
          "Scorch": null
        },
        "RetainedDurables": null
      }
    }
    """;

static ChronicleState Goal6AFixture() => ChronicleState.Begin(Seed) with
{
    Address = new WorldAddress(SurfacePatch.SurfaceStratum, 0, 0),
    Intent = OpeningIntent.Unchosen,
    Codex = new CodexState(),
    Loadout = LoadoutState.Empty,
    WorldGrammarVersion = 4,
    Home = null,
    Combat = CombatState.Create(Seed),
    PowerHome = null,
    Attunement = new LoadAttunementState(CombatState.SharedLoadCapacity, 0),
};

static ChronicleSimulation NewEncounter(WordId modifier, bool openingWeapon)
{
    var simulation = new ChronicleSimulation(Goal6AFixture());
    Assert(simulation.Apply(new ChooseAgainstIntent()).Applied, "Combat opening must be selectable.");
    Assert(simulation.Apply(new AttuneExpression(WordIds.Burn, [modifier])).Applied, "The requested Burn Expression must attune while safe.");
    Assert(simulation.Apply(new ConfigureEngagementPlan(openingWeapon)).Applied || !openingWeapon, "The Engagement Plan must configure while safe.");
    Assert(simulation.Apply(new MoveIncarnation(1, 0)).Applied, "The Incarnation must approach on the shared map.");
    Assert(simulation.Apply(new MoveIncarnation(1, 0)).Applied, "The second approach step must reach immediate threat range.");
    Assert(simulation.State.Speed == ChronicleSpeed.Paused, "Immediate threat contact must pause.");
    return simulation;
}

static ChronicleSimulation PlayDeterministicTranscript()
{
    var simulation = NewEncounter(WordIds.Quickly, openingWeapon: true);
    simulation.Apply(new PrepareBurn(simulation.CombatContext.MireBrute!.Address));
    simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
    Advance(simulation, 5);
    return simulation;
}

static void AssertRevalidation(ChronicleState state, string subject)
{
    var simulation = new ChronicleSimulation(state);
    simulation.AdvanceOneTick();
    Assert(
        simulation.CombatContext.Preparation is null &&
        simulation.CombatContext.RecentResults.Any(result => result.Kind == CombatResultKind.PreparationInterrupted),
        $"Burn release must revalidate {subject} through the same simulation seam.");
}

static void Advance(ChronicleSimulation simulation, int count)
{
    for (var index = 0; index < count; index++)
    {
        simulation.AdvanceOneTick();
    }
}

static void AdvanceUntil(ChronicleSimulation simulation, Func<ChronicleSimulation, bool> predicate, int limit)
{
    for (var index = 0; index < limit && !predicate(simulation); index++)
    {
        if (simulation.State.Speed == ChronicleSpeed.Paused && simulation.State.HasLivingIncarnation)
        {
            simulation.Apply(new SetChronicleSpeed(ChronicleSpeed.Slow));
        }

        simulation.AdvanceOneTick();
    }

    Assert(predicate(simulation), "The deterministic acceptance fixture did not reach its required outcome.");
}

static void AssertRetiredPredecessor(ChronicleState state, int grammarPin, string source)
{
    Assert(
        state.Seed == Seed &&
        state.Tick == 7 &&
        state.Address == SkyStratum.LandmarkAddress &&
        state.IncarnationId == 1 &&
        state.IncarnationLife == IncarnationLifeState.Alive &&
        state.WorldGrammarVersion == grammarPin &&
        state.Combat is null &&
        state.Codex.Contains(WordIds.Fly) &&
        !state.Codex.Contains(WordIds.Stone) &&
        !state.Codex.Contains(WordIds.Bell) &&
        state.Study.ActiveWord is null &&
        state.Study.UnderstandingFor(WordIds.Stone) == 0 &&
        state.ActiveLoadout[0] is { Verb: var verb, Noun: null } && verb == WordIds.Fly,
        $"{source} must preserve durable predecessor state while explicitly retiring Stone/Bell Nouns and fitted Fly[Noun].");

    if (grammarPin == 3)
    {
        Assert(
            state.Codex.Contains(WordIds.Smash) &&
            state.Home is
            {
                HoldingId: "holding.home",
                Address: var homeAddress,
                Material: HomeMaterialState.HearthstoneRaised,
            } && homeAddress == new WorldAddress(SurfacePatch.SurfaceStratum, 0, 3) &&
            state.CurrentBellAddress == SkyStratum.LandmarkAddress &&
            state.FirstConflict is
            {
                Outcome: FirstConflictOutcome.Shattered,
                Address: var cairnAddress,
                ResolvedTick: 7,
                ResolvingIncarnationId: 1,
            } && cairnAddress == LegacyCairnAddress(),
            $"{source} must preserve Home, Bell position, and the resolved first-conflict material result.");
    }
}

static string LiteralVersion5()
{
    var cairn = LegacyCairnAddress();
    return
    """
    {
      "Version": 5,
      "Chronicle": {
        "Seed": 41337,
        "Tick": 7,
        "Address": { "Stratum": "sky", "X": 0, "Y": -4 },
        "Speed": 0,
        "Intent": 1,
        "Codex": { "Words": ["word.fly", "word.found", "word.smash", "word.stone", "word.bell"] },
        "Study": {
          "Understanding": [{ "Word": "word.stone", "Amount": 7 }],
          "ActiveSourceId": "study-source.bell-that-fell-up.sky-stone",
          "ActiveWord": "word.stone"
        },
        "Loadout": {
          "Slot1": { "Verb": "word.fly", "Noun": "word.stone" },
          "Slot2": { "Verb": null, "Noun": null },
          "Slot3": { "Verb": null, "Noun": null },
          "Slot4": { "Verb": null, "Noun": null },
          "Slot5": { "Verb": null, "Noun": null },
          "Slot6": { "Verb": null, "Noun": null },
          "Slot7": { "Verb": null, "Noun": null },
          "Slot8": { "Verb": null, "Noun": null }
        },
        "LooseStoneAddress": { "Stratum": "surface", "X": 1, "Y": 0 },
        "IncarnationId": 1,
        "IncarnationLife": 0,
        "WorldGrammarVersion": 3,
        "Home": {
          "HoldingId": "holding.home",
          "DisplayName": "The First Hearth",
          "Address": { "Stratum": "surface", "X": 0, "Y": 3 },
          "FoundedTick": 1,
          "FoundingIncarnationId": 1,
          "Material": 1
        },
        "FirstConflict": {
          "SubjectId": "subject.river-ward",
          "Address": { "Stratum": "surface", "X": __CAIRN_X__, "Y": __CAIRN_Y__ },
          "ThreatenedTick": 6,
          "PendingAction": null,
          "Outcome": 1,
          "ResolvedTick": 7,
          "ResolvingIncarnationId": 1
        },
        "BellAddress": { "Stratum": "sky", "X": 0, "Y": -4 }
      }
    }
    """.Replace("__CAIRN_X__", cairn.X.ToString(), StringComparison.Ordinal)
        .Replace("__CAIRN_Y__", cairn.Y.ToString(), StringComparison.Ordinal);
}

static WorldAddress LegacyCairnAddress()
{
    var legacy = Goal6AFixture() with { WorldGrammarVersion = 3, Combat = null };
    return WorldArea.Generate(
            legacy,
            SurfacePatch.SurfaceStratum,
            new WorldRectangle(-96, -96, 193, 193))
        .Cells
        .Single(cell => cell.DurableIdentity == FirstConflictSubjects.RivenCairnIdentity)
        .Address;
}

static string LiteralVersion1() =>
    """
    {
      "Version": 1,
      "Chronicle": {
        "Seed": 41337,
        "Tick": 0,
        "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
        "Speed": 2,
        "Intent": 1,
        "Codex": { "HasFly": true, "HasStone": true },
        "Study": { "StoneUnderstanding": 7, "IsStudyingBell": false },
        "WorldGrammarVersion": 0
      }
    }
    """;

static string LiteralPreEnvelope() =>
    """
    {
      "Seed": 41337,
      "Tick": 0,
      "Address": { "Stratum": "surface", "X": 0, "Y": 0 },
      "Speed": 2,
      "WorldGrammarVersion": 0
    }
    """;

static void Run(string name, Action check)
{
    check();
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows(Action action, string message)
{
    try
    {
        action();
    }
    catch (InvalidOperationException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}
