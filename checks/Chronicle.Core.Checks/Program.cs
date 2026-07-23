using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using Chronicle.Core;

const long Seed = 41_337;

Run("authored successor fixture", VerifyAuthoredFixture);
Run("WG4 subjects and semantic overlays", VerifyWorldGrammarSubjects);
Run("engagement, pending commands, and heartbeat order", VerifyEngagementAndHeartbeatOrder);
Run("Burn plans, Load, and attunement safety", VerifyExpressionRules);
Run("target preview and release revalidation", VerifyTargetsAndRevalidation);
Run("recovery, persistence, death, and replay", VerifyPersistenceDeathAndReplay);
Run("strict v6 and literal predecessor migration", VerifyPersistenceAndMigration);
Run("Goal 6B physical return, Attunement, loss, and rebuild", VerifyHoldingRules);
Run("Goal 6B strict v7 replay, malformed state, and migration", VerifyGoal6BPersistenceAndReplay);
Run("Goal 7A Agent grammar, promotion, and scale", VerifyAgentGrammarAndScale);
Run("Goal 7A arrival, welcome, persistence, and replay", VerifyAgentJourneyAndPersistence);
Run("Goal 7B social Words, Directive truth table, and agency", VerifyDirectiveRules);
Run("Goal 7B persistence, replacement, scale, and migration", VerifyDirectivePersistenceAndMigration);
Run("Core owns facts, not player-facing copy", VerifyCoreOwnsNoPresentationCopy);
Run("authored Word effects extend without resolver edits", VerifyAuthoredWordEffects);
Run("accepted pre-6C save bytes remain stable", VerifyHistoricalSaveOracle);

Console.WriteLine("RETAINED FOUNDATION MIGRATION PASS world-grammar=0-3 home=preserved bell=preserved cairn=preserved nouns=retired");
Console.WriteLine("GOAL6A CORE ACCEPTANCE PASS retained grammar=4 combat=deterministic migration=v6-v1+pre-envelope");
Console.WriteLine("GOAL6B CORE ACCEPTANCE PASS save=7 grammar=5 power-home=physical attunement=next-only replay=deterministic");
Console.WriteLine("GOAL7A CORE ACCEPTANCE PASS save=8 grammar=6 agent=consequential welcome=autonomous replay=deterministic");
Console.WriteLine("GOAL7B CORE ACCEPTANCE PASS save=9 grammar=6 directive=bounded agency=preserved replay=deterministic");

static void VerifyAuthoredFixture()
{
    Assert(ChronicleSaveCodec.CurrentVersion == 9, "Goal 7B must write strict save envelope v9.");
    Assert(ChronicleState.Begin(Seed).WorldGrammarVersion == 6, "New Chronicles must pin World Grammar v6.");

    var burn = WordCatalogue.Get(WordIds.Burn);
    var quickly = WordCatalogue.Get(WordIds.Quickly);
    var lasting = WordCatalogue.Get(WordIds.Lasting);
    Assert(
        burn.Kind == WordKind.Verb && burn.Load == 1 &&
        quickly.Kind == WordKind.Modifier && quickly.Load == 6 &&
        quickly.SupportedVerbs.SequenceEqual([WordIds.Burn]) &&
        lasting.Kind == WordKind.Modifier && lasting.Load == 5 &&
        lasting.SupportedVerbs.SequenceEqual([WordIds.Burn]),
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
        brute.Subject(WorldSubjectKind.Creature) is
        {
            Identity: var identity,
            Archetype: WorldSubjects.MireBruteArchetype,
            Progress: { Current: CombatState.MireBruteMaximumHitPoints, Maximum: CombatState.MireBruteMaximumHitPoints },
            Condition: WorldSubjects.Living,
        } && identity == WorldArea.GeneratedMireBruteIdentity(Seed),
        "WG4 must generate one stable Mire Brute identity, placement, HP, and living state.");
    Assert(
        basalt.Subject(WorldSubjectKind.Target) is
        {
            Archetype: WorldSubjects.BasaltArchetype,
            Identity: var basaltIdentity,
        } && basaltIdentity == WorldArea.GeneratedBasaltIdentity(Seed),
        "WG4 must generate the stable basalt place Target.");
    AssertArgumentThrows(
        () => _ = new WorldSubjectProgress(-1, 2),
        "WorldSubject progress must reject a current value below zero.");
    AssertArgumentThrows(
        () => _ = new WorldSubjectProgress(3, 2),
        "WorldSubject progress must reject a current value above its maximum.");
    AssertArgumentThrows(
        () => _ = new WorldSubject(
            "subject.too-many-marks",
            WorldSubjectKind.Target,
            "verification",
            "present",
            marks:
            [
                WorldSubjectMark.Wounded,
                WorldSubjectMark.Burning,
                WorldSubjectMark.Selected,
                WorldSubjectMark.Wounded,
                WorldSubjectMark.Burning,
            ]),
        "WorldSubject marks must remain explicitly bounded.");
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
        overlapping.Has(WorldSubjectKind.Creature) && overlapping.IsScorched,
        "WorldArea must expose a Brute state and a scorch overlay independently at one Address.");

    var oldGrammar = Goal6AFixture() with { WorldGrammarVersion = 3, Combat = null };
    var oldArea = WorldArea.Generate(
        oldGrammar,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(bruteAddress.X, bruteAddress.Y, 1, 1));
    Assert(
        !oldArea.Cells.Single().Has(WorldSubjectKind.Creature),
        "Older World Grammar pins must not gain the Brute retroactively.");
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
    Assert(node["Version"]!.GetValue<int>() == 9, "Current serialization must write envelope v9.");
    Assert(node["Chronicle"]!["Combat"] is not null, "v9 must persist the bounded combat state.");
    var successorChronicle = node["Chronicle"]!.AsObject();
    Assert(
        !successorChronicle.ContainsKey("Study") &&
        !successorChronicle.ContainsKey("FirstConflict") &&
        !successorChronicle.ContainsKey("LooseStoneAddress") &&
        !successorChronicle.ContainsKey("BellAddress") &&
        !successorChronicle.ContainsKey("Home") &&
        !successorChronicle["Loadout"]!.AsObject().ContainsKey("Noun"),
        "Fresh v9 must serialize only successor state, not predecessor Study/Noun/FirstConflict fields.");
    Assert(!json.Contains("RecentResults", StringComparison.Ordinal), "Message Log history must not enter v9 persistence.");
    Assert(ChronicleSaveCodec.Deserialize(json) == current.State, "Strict v9 must round-trip exact durable successor state.");

    node["Unexpected"] = true;
    AssertThrows(() => ChronicleSaveCodec.Deserialize(node.ToJsonString()), "Strict v9 must reject unexpected envelope fields.");

    var nounNode = JsonNode.Parse(json)!.AsObject();
    nounNode["Chronicle"]!["Codex"]!["Words"]!.AsArray().Add("word.stone");
    AssertThrows(
        () => ChronicleSaveCodec.Deserialize(nounNode.ToJsonString()),
        "Strict v9 must reject predecessor Noun knowledge even though literal old-save readers still recognize it.");

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
        ChronicleSaveCodec.Serialize(preEnvelope).Contains("\"Version\": 9", StringComparison.Ordinal),
        "Pre-envelope input must remain accepted and rewrite through strict v9 without a Brute.");
}

static void VerifyHoldingRules()
{
    var testingStart = new ChronicleSimulation(LegacyGoal6BStart());
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
        unreadPrimer.Objective.Kind == HoldingObjectiveKind.LearnBurn &&
        unreadPrimer.Actions.Single(action => action.Id == "read-primer").Available,
        "The neutral start must expose one unread Burn Primer directly north of Home and no implicit Burn path reward.");
    var primerCell = WorldArea.Generate(
        testingStart.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(0, 2, 1, 1)).Cells.Single();
    Assert(
        primerCell.Subject(WorldSubjectKind.StudySource) is
        {
            Condition: WorldSubjects.Unread,
            Identity: var primerIdentity,
        } &&
        primerIdentity == unreadPrimer.BurnPrimer.Identity &&
        primerCell.Ground == WorldGround.Soil &&
        primerCell.Feature is null &&
        primerCell.MotifIdentity == "surface-burn-primer-clearing",
        "The unread Burn Primer must be a stable visible World subject on a clear-soil cell at its Core-owned Address.");
    AssertRoundTrip(testingStart.State, "unread Burn Primer");
    var standingOnPrimer = new ChronicleSimulation(testingStart.State with { Address = unreadPrimer.BurnPrimer.Address });
    Assert(
        standingOnPrimer.PowerComesHomeContext.Actions.Single(action => action.Id == "read-primer").Available &&
        standingOnPrimer.Apply(new ReadBurnPrimer()).Applied,
        "Standing on a non-blocking Goal 6B subject must be as valid for interaction as standing cardinally adjacent.");
    var primerTick = testingStart.State.Tick;
    Assert(testingStart.Apply(new ReadBurnPrimer()).Applied &&
           testingStart.State.Tick == primerTick &&
           testingStart.State.Codex.Contains(WordIds.Burn) &&
           testingStart.State.Codex.Contains(WordIds.Quickly) &&
           testingStart.State.Codex.Contains(WordIds.Lasting),
        "The nearby Burn Primer must teach the complete Goal 6B test Expression without spending a Heartbeat.");
    Assert(
        testingStart.PowerComesHomeContext.BurnPrimer.IsRead &&
        testingStart.PowerComesHomeContext.Objective.Kind == HoldingObjectiveKind.GetTheLode &&
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
    AssertObjective(
        context,
        "embedded",
        HoldingObjectiveKind.GetTheLode,
        HoldingActionKind.Extract,
        actionHeartbeats: 2,
        constraints:
        [
            HoldingConstraint.HostileInterruptionKeepsProgress,
            HoldingConstraint.LocksAllOtherActionsWhileActive,
        ]);
    var broad = WorldArea.Generate(
        simulation.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(6, 1, 5, 5));
    var overlap = WorldArea.Generate(
        simulation.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(8, 3, 2, 2));
    Assert(
        broad.Cells.Single(cell => cell.Address == context.SeamAddress)
            .Subject(WorldSubjectKind.MaterialSeam)?.Identity == context.SeamIdentity &&
        overlap.Cells.Single(cell => cell.Address == context.SeamAddress)
            .Subject(WorldSubjectKind.LooseMaterial)?.Identity == context.Lode.Identity,
        "Differently bounded and ordered queries must reproduce the exact Seam and Lode identities.");
    var sourceSite = WorldArea.Generate(
        simulation.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(context.ResonatorSite!.Value.X, context.ResonatorSite.Value.Y, 1, 1)).Cells.Single();
    Assert(
        sourceSite.Has(WorldSubjectKind.ConstructionSite) &&
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
    AssertObjective(
        simulation.PowerComesHomeContext,
        "extracting",
        HoldingObjectiveKind.Commitment,
        HoldingActionKind.AdvanceHeartbeat,
        outcome: HoldingOutcome.ExtractionProgressRemains,
        commitmentCompletedTicks: 0,
        commitmentTotalTicks: 2,
        waitingForHeartbeat: true,
        constraints:
        [
            HoldingConstraint.HostileInterruptionKeepsProgress,
            HoldingConstraint.LocksAllOtherActionsWhileActive,
        ]);
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
    AssertObjective(
        simulation.PowerComesHomeContext,
        "loose",
        HoldingObjectiveKind.LiftTheLode,
        HoldingActionKind.Lift,
        showsCarryHomeStep: true,
        constraints: [HoldingConstraint.CarryingLocksWeaponInvocationFlightAttunement]);

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
    AssertObjective(
        simulation.PowerComesHomeContext,
        "carried",
        HoldingObjectiveKind.CarryLodeHome,
        HoldingActionKind.Build,
        actionHeartbeats: 3,
        constraints:
        [
            HoldingConstraint.HostileInterruptionKeepsWorkProgress,
            HoldingConstraint.CarryingLocksWeaponInvocationFlightAttunement,
        ]);
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
    AssertObjective(
        simulation.PowerComesHomeContext,
        "construction",
        HoldingObjectiveKind.FinishConstruction,
        HoldingActionKind.ResumeBuild,
        actionHeartbeats: 2,
        constraints:
        [
            HoldingConstraint.HostileInterruptionKeepsProgress,
            HoldingConstraint.LocksAllOtherActionsWhileActive,
        ]);
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
    AssertObjective(
        simulation.PowerComesHomeContext,
        "intact before Attunement",
        HoldingObjectiveKind.UseNewLoad,
        HoldingActionKind.Attune,
        fact: HoldingEstablishedFact.SourceContributesAtNextAttunement,
        outcome: HoldingOutcome.LoadoutChangesAtAttunement,
        constraints: [HoldingConstraint.BlockedByCarryingWorkOrDanger]);

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
    AssertObjective(
        simulation.PowerComesHomeContext,
        "intact after Attunement",
        HoldingObjectiveKind.TestSourceLoss,
        HoldingActionKind.Dismantle,
        actionHeartbeats: 2,
        fact: HoldingEstablishedFact.CurrentLoadoutSurvivesSourceLoss,
        outcome: HoldingOutcome.DamagedThenDestroyedNextFallsToInherent,
        constraints: [HoldingConstraint.LocksMovementFightInvocationAttunement]);

    Assert(simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Dismantle)).Applied,
        "Controlled Dismantle must be available at the intact Source.");
    AdvanceActive(simulation, 1);
    Assert(
        simulation.State.PowerHome!.Resonator?.Phase == HearthResonatorPhase.Damaged &&
        simulation.PowerComesHomeContext.Attunement.NextAttunementCapacity == 12,
        "Dismantling Heartbeat one must be visibly damaged while still contributing +4.");
    AssertObjective(
        simulation.PowerComesHomeContext,
        "dismantling",
        HoldingObjectiveKind.Commitment,
        HoldingActionKind.AdvanceHeartbeat,
        outcome: HoldingOutcome.ResonatorDestroyedNextFallsToInherent,
        commitmentCompletedTicks: 1,
        commitmentTotalTicks: 2,
        constraints:
        [
            HoldingConstraint.HostileInterruptionKeepsProgress,
            HoldingConstraint.LocksAllOtherActionsWhileActive,
        ]);
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
    AssertObjective(
        simulation.PowerComesHomeContext,
        "destroyed",
        HoldingObjectiveKind.RebuildPower,
        HoldingActionKind.Rebuild,
        actionHeartbeats: 3,
        fact: HoldingEstablishedFact.SourceDestroyedNextIsInherent,
        outcome: HoldingOutcome.ResonatorIntactRestoresFullNext,
        constraints: [HoldingConstraint.LocksMovementFightInvocationAttunement]);
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
    AssertObjective(
        simulation.PowerComesHomeContext,
        "rebuilding",
        HoldingObjectiveKind.FinishRebuild,
        HoldingActionKind.ResumeRebuild,
        actionHeartbeats: 2,
        outcome: HoldingOutcome.ResonatorIntactRestoresFullNext,
        constraints: [HoldingConstraint.LocksMovementFightInvocationAttunement]);
    Assert(simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Rebuild)).Applied,
        "Rebuild must resume represented progress after cancellation.");
    AdvanceActive(simulation, 1);
    AssertRoundTrip(simulation.State, "rebuilding 2/3");
    simulation.AdvanceOneTick();
    Assert(simulation.State.PowerHome!.Resonator?.Phase == HearthResonatorPhase.Intact &&
           simulation.PowerComesHomeContext.Attunement.NextAttunementCapacity == 12 &&
           simulation.State.Attunement is null,
        "Rebuild must restore only future capacity without automatic replacement Loadout changes.");
    AssertObjective(
        simulation.PowerComesHomeContext,
        "rebuilt before Attunement",
        HoldingObjectiveKind.UseNewLoad,
        HoldingActionKind.Attune,
        fact: HoldingEstablishedFact.SourceContributesAtNextAttunement,
        outcome: HoldingOutcome.LoadoutChangesAtAttunement,
        constraints: [HoldingConstraint.BlockedByCarryingWorkOrDanger]);
    Assert(simulation.Apply(new AttuneExpression(WordIds.Burn, [WordIds.Quickly, WordIds.Lasting])).Applied,
        "Explicit post-rebuild Attunement must restore the combined Expression.");
}

static void VerifyCoreOwnsNoPresentationCopy()
{
    var coreRoot = LocateCoreSources();
    var bannedTokens = new[]
    {
        "[ ]", "[x]", "[P]", "[Q]", "[L]", "[G]", "[X]",
        "SPACE", "Press G", "Press P", "Press Q", "Press L", "press the",
        "CHECKLIST", "GOLD SEAM", "GOLD LODE", "OUTLINED", "BURN PRIMER",
        "STOPS:", "LOCKS:", "BLOCKED BY:", "keybind", "hotkey",
    };

    var offences = new List<string>();
    foreach (var file in Directory.EnumerateFiles(coreRoot, "*.cs", SearchOption.AllDirectories))
    {
        if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            continue;
        }

        var lineNumber = 0;
        foreach (var line in File.ReadLines(file))
        {
            lineNumber++;
            if (!line.Contains('"'))
            {
                continue;
            }

            foreach (var token in bannedTokens)
            {
                if (line.Contains(token, StringComparison.Ordinal))
                {
                    offences.Add($"{Path.GetFileName(file)}:{lineNumber} contains '{token}'");
                }
            }
        }
    }

    Assert(
        offences.Count == 0,
        "Chronicle.Core must contain no input key name, HUD label, or checklist glyph: " +
        string.Join("; ", offences));
}

static string LocateCoreSources()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, "src", "Chronicle.Core");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("The Chronicle.Core sources are required for the vocabulary gate.");
}

static void VerifyAuthoredWordEffects()
{
    var burn = WordCatalogue.Get(WordIds.Burn);
    var quickly = WordCatalogue.Get(WordIds.Quickly);
    var lasting = WordCatalogue.Get(WordIds.Lasting);

    Assert(
        WordEffects.Compose(burn, []) is { Preparation: 3, Consequence: 3, Recovery: 8, Damage: 4 },
        "Burn must carry its authored 3/3/8 timing and 4 damage as catalogue data.");
    Assert(
        WordEffects.Compose(burn, [quickly]) is { Preparation: 1, Consequence: 3 },
        "Quickly must shorten authored Preparation to one Heartbeat through its own data.");
    Assert(
        WordEffects.Compose(burn, [lasting]) is { Preparation: 3, Consequence: 6 },
        "Lasting must extend authored consequence to six Heartbeats through its own data.");
    Assert(
        WordEffects.Compose(burn, [quickly, lasting]) ==
        WordEffects.Compose(burn, [lasting, quickly]),
        "Composed Modifier effects must be order-independent.");

    // A fourth Modifier is authoring data alone: composing it changes resolved
    // timing without one line of resolver change.
    var steadily = new WordDefinition(
        new WordId("word.steadily"),
        "Steadily",
        WordKind.Modifier,
        "Lengthen the exposed preparation of an authored compatible Verb.",
        0,
        Array.Empty<WordId>(),
        Load: 2,
        Effect: new WordEffect(Preparation: 2, Consequence: 1),
        CompatibleVerbs: Array.AsReadOnly([WordIds.Burn]));

    Assert(
        WordEffects.Compose(burn, [steadily]) is { Preparation: 5, Consequence: 4, Recovery: 8, Damage: 4 },
        "A fourth authored Modifier must change resolved timing through catalogue data alone.");
    Assert(
        WordEffects.Compose(burn, [quickly, steadily]) is { Preparation: 3 },
        "Authored Modifier deltas must accumulate rather than switch on individual Words.");
    using (WordCatalogue.UseDefinitionsForVerification(WordCatalogue.Words.Append(steadily)))
    {
        var fixture = Goal6AFixture() with
        {
            Intent = OpeningIntent.Against,
            Codex = new CodexState().Learn(WordIds.Burn).Learn(steadily.Id),
        };
        var simulation = new ChronicleSimulation(fixture);
        Assert(
            simulation.Apply(new AttuneExpression(WordIds.Burn, [steadily.Id])).Applied,
            "The fourth Modifier must attune through the production expression validator.");
        simulation = At(
            simulation,
            new WorldAddress(SurfacePatch.SurfaceStratum, 2, 0));
        Assert(
            simulation.Apply(new PrepareBurn(WorldArea.GeneratedMireBruteAddress(Seed))).Applied &&
            simulation.State.Combat?.Preparation?.RemainingTicks == 5,
            "The fourth Modifier must change production Burn preparation timing through its definition alone.");

        var loaded = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(simulation.State));
        Assert(
            loaded.Combat?.Preparation?.RemainingTicks == 5 &&
            loaded.ActiveLoadout.Slots.Single(slot => !slot.IsEmpty).Modifiers.SequenceEqual([steadily.Id]),
            "Strict v7 validation and save/load must accept the authored fourth Modifier without a resolver edit.");
    }

    Assert(
        WordCatalogue.Words.All(word => word.Id != steadily.Id),
        "The test-only fourth Modifier must never enter the shipped Word Catalogue.");
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
    Assert(
        !wg4Area.Cells[0].Has(WorldSubjectKind.MaterialSeam) &&
        !wg4Area.Cells[0].Has(WorldSubjectKind.LooseMaterial),
        "WG4 and older pins must never gain the WG5 Seam or Lode retroactively.");
}

static void VerifyAgentGrammarAndScale()
{
    var origin = HoldingFacts.SingingSeamAddress;
    var lodeIdentity = HoldingRules.ResonantLodeIdentity(Seed);
    var tamar = AgentGrammar.Generate(Seed, 6, lodeIdentity, origin, 0);
    Assert(
        tamar.DisplayName == "Tamar Venn" &&
        tamar.Archetype == AgentGrammar.WayfarerListenerArchetype &&
        tamar.ProvenanceIdentity == lodeIdentity &&
        tamar.OriginAddress == origin,
        "Seed 41337 plus the installed Resonant Lode provenance must freeze Tamar Venn without a singleton profile.");
    Assert(
        AgentGrammar.Generate(Seed, 6, lodeIdentity, origin with { X = origin.X + 1 }, 0).Identity !=
            tamar.Identity &&
        AgentGrammar.Generate(Seed, 7, lodeIdentity, origin, 0).Identity != tamar.Identity,
        "Stable Agent identity must include origin and World Grammar version, not only a short provenance hash.");

    var keys = Enumerable.Range(0, 512)
        .Select(index => (Provenance: $"fixture.provenance.{index}", Ordinal: index % 7))
        .ToArray();
    var forward = keys.ToDictionary(
        key => key,
        key => AgentGrammar.Generate(
            Seed,
            6,
            key.Provenance,
            new WorldAddress(SurfacePatch.SurfaceStratum, 100 + key.Ordinal, indexY(key.Provenance)),
            key.Ordinal));
    var reverse = keys.Reverse().ToDictionary(
        key => key,
        key => AgentGrammar.Generate(
            Seed,
            6,
            key.Provenance,
            new WorldAddress(SurfacePatch.SurfaceStratum, 100 + key.Ordinal, indexY(key.Provenance)),
            key.Ordinal));
    var shuffledKeys = keys.OrderBy(key => DeterministicOrder(key.Provenance)).ToArray();
    var shuffled = shuffledKeys.ToDictionary(
        key => key,
        key => AgentGrammar.Generate(
            Seed,
            6,
            key.Provenance,
            new WorldAddress(SurfacePatch.SurfaceStratum, 100 + key.Ordinal, indexY(key.Provenance)),
            key.Ordinal));
    Assert(
        keys.All(key => forward[key] == reverse[key] && forward[key] == shuffled[key]) &&
        forward.Values.Select(profile => profile.Identity).Distinct(StringComparer.Ordinal).Count() == 512 &&
        forward.Values.All(profile => !string.IsNullOrWhiteSpace(profile.DisplayName) &&
                                      profile.Archetype == AgentGrammar.WayfarerListenerArchetype),
        "Agent possibility generation must be request-order independent, unique by stable key, and authored without mutating a Chronicle.");

    var scaleAgents = Enumerable.Range(0, 256)
        .Select(index =>
        {
            var provenance = $"scale.provenance.{index}";
            var agentOrigin = new WorldAddress(SurfacePatch.SurfaceStratum, 2000 + index, 1000);
            var profile = AgentGrammar.Generate(Seed, 6, provenance, agentOrigin, index);
            var address = new WorldAddress(SurfacePatch.SurfaceStratum, 4000 + index * 3L, 2000);
            return new AgentState(
                profile,
                address,
                address,
                AgentPresenceState.AtHome,
                new AgentNeedState(AgentNeedKind.Refuge, AgentNeedStatus.Satisfied),
                new AgentHomeRelationshipState("holding.home", AgentHomeRelationshipKind.Guest, 4, 1),
                AgentIntentKind.RemainAtHome,
                PromotedTick: 1,
                ArrivalTick: 2,
                WelcomeOfferedTick: 3,
                RoadRollAddress: address with { X = address.X + 1 });
        })
        .ToArray();
    var scaleState = ChronicleState.Begin(Seed) with
    {
        Tick = 10,
        Agents = new AgentCollectionState(scaleAgents),
    };
    var scaleLoaded = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(scaleState));
    Assert(
        scaleLoaded.Agents.Count == 256 && scaleLoaded.Agents.SequenceEqual(scaleAgents),
        "Strict v9 must round-trip 256 consequential Agent records and their personal subjects exactly.");

    var duplicateIdentity = scaleState with
    {
        Agents = new AgentCollectionState([scaleAgents[0], scaleAgents[0] with
        {
            Address = scaleAgents[0].Address with { Y = 2100 },
            WaitingAddress = scaleAgents[0].Address with { Y = 2100 },
            RoadRollAddress = scaleAgents[0].Address with { X = scaleAgents[0].Address.X + 1, Y = 2100 },
        }]),
    };
    AssertThrows(
        () => ChronicleSaveCodec.Serialize(duplicateIdentity),
        "Strict v9 must reject duplicate consequential Agent identities.");

    var badProvenance = scaleState with
    {
        Agents = new AgentCollectionState([scaleAgents[0] with
        {
            Profile = scaleAgents[0].Profile with { ProvenanceIdentity = "fabricated.provenance" },
        }]),
    };
    AssertThrows(
        () => ChronicleSaveCodec.Serialize(badProvenance),
        "Strict v9 must reject a generated profile that disagrees with its provenance.");

    var second = scaleAgents[1] with
    {
        Address = scaleAgents[0].Address,
        WaitingAddress = scaleAgents[0].Address,
    };
    AssertThrows(
        () => ChronicleSaveCodec.Serialize(scaleState with
        {
            Agents = new AgentCollectionState([scaleAgents[0], second]),
        }),
        "Strict v9 must reject duplicate exclusive Agent occupancy.");

    AssertThrows(
        () => ChronicleSaveCodec.Serialize(scaleState with
        {
            Agents = new AgentCollectionState([scaleAgents[0] with
            {
                Need = new AgentNeedState((AgentNeedKind)99, AgentNeedStatus.Satisfied),
            }]),
        }),
        "Strict v9 must reject unknown authored need values.");

    AssertThrows(
        () => ChronicleSaveCodec.Serialize(scaleState with
        {
            Agents = new AgentCollectionState([scaleAgents[0] with
            {
                Presence = AgentPresenceState.WaitingAtHome,
                Need = new AgentNeedState(AgentNeedKind.Refuge, AgentNeedStatus.Seeking),
                HomeRelationship = new AgentHomeRelationshipState(
                    "holding.home",
                    AgentHomeRelationshipKind.Unfamiliar),
                Intent = AgentIntentKind.WaitForWelcome,
                WelcomeOfferedTick = null,
            }]),
        }),
        "Strict v9 must reject an orphaned personal road-roll.");

    static long indexY(string provenance) =>
        long.Parse(provenance[(provenance.LastIndexOf('.') + 1)..], System.Globalization.CultureInfo.InvariantCulture);
    static uint DeterministicOrder(string value) =>
        value.Aggregate(2166136261u, (hash, character) => unchecked((hash ^ character) * 16777619u));
}

static void VerifyAgentJourneyAndPersistence()
{
    var promoted = CompleteGoal7AResonator();
    var tamar = promoted.AgentContext.PrimaryAgent;
    Assert(
        promoted.State.Agents.Count == 1 &&
        tamar is
        {
            DisplayName: "Tamar Venn",
            Presence: AgentPresenceState.ApproachingHome,
            Need: { Kind: AgentNeedKind.Refuge, Status: AgentNeedStatus.Seeking },
            HomeRelationship.Kind: AgentHomeRelationshipKind.Unfamiliar,
            Intent: AgentIntentKind.ApproachHome,
        } &&
        tamar.Address == AgentRules.ResonanceListenerStartAddress(promoted.State) &&
        tamar.WaitingAddress == AgentRules.ResonanceListenerWaitingAddress(promoted.State),
        "Completing the first intact Resonator must promote exactly one Tamar at the three-step route start.");

    var sourceHistory = promoted.State;
    Assert(promoted.Apply(new BeginPowerCommitment(PowerCommitmentKind.Dismantle)).Applied,
        "The post-promotion fixture must be able to dismantle its Source.");
    AdvanceActive(promoted, 2);
    Assert(
        promoted.State.PowerHome!.Resonator?.Phase == HearthResonatorPhase.Destroyed &&
        promoted.State.Agents.Count == 1 &&
        promoted.State.Agents[0].Profile == sourceHistory.Agents[0].Profile &&
        promoted.State.Agents[0].Address.X == sourceHistory.Agents[0].Address.X + 2,
        "Destroying the Source must remove future Load capacity without unmaking or redirecting its historical Agent.");

    var paused = new ChronicleSimulation(sourceHistory with { Speed = ChronicleSpeed.Paused });
    paused.AdvanceOneTick();
    Assert(paused.State == sourceHistory with { Speed = ChronicleSpeed.Paused },
        "Pause must freeze Agent motion and intent.");

    var blockedDestination = sourceHistory.Agents[0].Address with { X = sourceHistory.Agents[0].Address.X + 1 };
    var blocked = new ChronicleSimulation(sourceHistory with
    {
        Address = blockedDestination,
        Speed = ChronicleSpeed.Slow,
    });
    blocked.AdvanceOneTick();
    Assert(
        blocked.State.Agents[0].Address == sourceHistory.Agents[0].Address &&
        blocked.AgentContext.RecentEvents.Last().Kind == AgentEventKind.Blocked &&
        blocked.AgentContext.RecentEvents.Last().Blocker == AgentBlockerKind.Incarnation,
        "A blocked arrival step must delay in place with an inspectable cause and no overlap.");
    blocked = new ChronicleSimulation(blocked.State with
    {
        Address = ChronicleState.AcceptedHomeFixtureAddress,
        Speed = ChronicleSpeed.Slow,
    });
    blocked.AdvanceOneTick();
    Assert(blocked.State.Agents[0].Address == blockedDestination,
        "Clearing a blocked route must advance only the current Heartbeat, never a catch-up step.");

    var arrival = new ChronicleSimulation(sourceHistory with { Speed = ChronicleSpeed.Slow });
    arrival.AdvanceOneTick();
    var afterOne = arrival.State.Agents[0].Address;
    arrival.AdvanceOneTick();
    var afterTwo = arrival.State.Agents[0].Address;
    arrival.AdvanceOneTick();
    var arrived = arrival.State.Agents[0];
    Assert(
        afterOne.X == sourceHistory.Agents[0].Address.X + 1 &&
        afterTwo.X == afterOne.X + 1 &&
        arrived.Address == arrived.WaitingAddress &&
        arrived.Presence == AgentPresenceState.WaitingAtHome &&
        arrived.ArrivalTick == arrival.State.Tick &&
        arrival.State.Speed == ChronicleSpeed.Paused,
        "Tamar must move exactly one cardinal cell per active Heartbeat and pause once on arrival.");
    var arrivalTick = arrival.State.Tick;
    arrival.AdvanceOneTick();
    Assert(arrival.State.Tick == arrivalTick && arrival.State.Agents[0] == arrived,
        "The arrival interruption must remain frozen until explicitly resumed.");
    AdvanceActive(arrival, 1);
    Assert(arrival.State.Tick == arrivalTick + 1 && arrival.State.Speed == ChronicleSpeed.Slow &&
           arrival.State.Agents[0] == arrived,
        "Clearing arrival pause must advance normally without another pause or queued Agent step.");

    var distant = new ChronicleSimulation(arrival.State with
    {
        Address = new WorldAddress(SurfacePatch.SurfaceStratum, 3, 3),
        Speed = ChronicleSpeed.Paused,
    });
    Assert(!distant.Apply(new OfferWelcome(arrived.Profile.Identity)).Applied,
        "Offering welcome outside physical interaction reach must reject atomically.");

    var sameCell = new ChronicleSimulation(arrival.State with
    {
        Address = arrived.Address,
        Speed = ChronicleSpeed.Paused,
    });
    Assert(sameCell.Apply(new OfferWelcome(arrived.Profile.Identity)).Applied,
        "Standing on an Agent's cell must remain valid for interaction.");

    var welcome = new ChronicleSimulation(arrival.State with
    {
        Address = ChronicleState.AcceptedHomeFixtureAddress,
        Speed = ChronicleSpeed.Paused,
    });
    var offeredAt = welcome.State.Tick;
    Assert(welcome.Apply(new OfferWelcome(arrived.Profile.Identity)).Applied &&
           welcome.State.Agents[0] is
           {
               Need.Status: AgentNeedStatus.Offered,
               HomeRelationship.Kind: AgentHomeRelationshipKind.WelcomeOffered,
               Intent: AgentIntentKind.ConsiderWelcome,
               WelcomeOfferedTick: var offerTick,
           } && offerTick == offeredAt &&
           welcome.AgentContext.Actions.Single(action => action.Kind == AgentActionKind.OfferWelcome)
               .AvailabilityReason == AgentActionAvailabilityReason.AnotherWelcomeIsOpen,
        "A cardinal-neighbor welcome must open one pending autonomous answer for the next active Heartbeat.");
    welcome.AdvanceOneTick();
    Assert(welcome.State.Tick == offeredAt &&
           welcome.State.Agents[0].HomeRelationship.Kind == AgentHomeRelationshipKind.WelcomeOffered,
        "A paused open welcome must remain visibly pending.");
    Assert(welcome.Apply(new WithdrawWelcome(arrived.Profile.Identity)).Applied &&
           welcome.State.Agents[0].HomeRelationship.Kind == AgentHomeRelationshipKind.Unfamiliar,
        "The Incarnation must be able to withdraw the welcome before its resolving Heartbeat.");
    Assert(welcome.Apply(new OfferWelcome(arrived.Profile.Identity)).Applied,
        "A withdrawn welcome must be offerable again while physical facts remain valid.");
    AdvanceActive(welcome, 1);
    var guest = welcome.State.Agents[0];
    Assert(
        guest is
        {
            Presence: AgentPresenceState.AtHome,
            Need.Status: AgentNeedStatus.Satisfied,
            HomeRelationship:
            {
                Kind: AgentHomeRelationshipKind.Guest,
                WelcomingIncarnationId: 1,
                EstablishedTick: var established,
            },
            Intent: AgentIntentKind.RemainAtHome,
            RoadRollAddress: var roll,
        } &&
        established == welcome.State.Tick &&
        roll == AgentRules.ResonanceListenerRoadRollAddress(welcome.State) &&
        welcome.State.Speed == ChronicleSpeed.Paused,
        "The next active Heartbeat must satisfy Refuge, establish Guest history, place Tamar's road-roll, and pause once.");
    Assert(!welcome.Apply(new OfferWelcome(guest.Profile.Identity)).Applied,
        "A Guest must reject a repeated welcome.");

    var local = WorldArea.Generate(
        welcome.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(-2, 2, 4, 4));
    Assert(
        local.Cells.Single(cell => cell.Address == guest.Address).Subject(WorldSubjectKind.Agent) is
        {
            Identity: var agentIdentity,
            Condition: WorldSubjects.Guest,
        } && agentIdentity == guest.Profile.Identity &&
        local.Cells.Single(cell => cell.Address == guest.RoadRollAddress)
            .Subject(WorldSubjectKind.PersonalPlace) is
        {
            OwnerIdentity: var owner,
            Condition: WorldSubjects.Laid,
        } && owner == guest.Profile.Identity,
        "World Grammar must expose Tamar and the owned road-roll as linked durable WorldSubjects.");

    var acceptedEventCount = welcome.AgentContext.RecentEvents.Count(@event =>
        @event.Kind == AgentEventKind.WelcomeAccepted);
    AdvanceActive(welcome, 1);
    Assert(
        welcome.State.Speed == ChronicleSpeed.Slow &&
        welcome.State.Agents[0] == guest &&
        welcome.AgentContext.RecentEvents.Count(@event => @event.Kind == AgentEventKind.WelcomeAccepted) == acceptedEventCount,
        "Acceptance must release its pause ownership without re-answering or latching auto-pause.");

    AssertRoundTrip(welcome.State, "accepted Guest and road-roll");
    var restored = new ChronicleSimulation(
        ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(welcome.State)));
    var departedArea = WorldArea.Generate(
        restored.State,
        SurfacePatch.SurfaceStratum,
        new WorldRectangle(20, 20, 3, 3));
    Assert(departedArea.Cells.All(cell => !cell.Has(WorldSubjectKind.Agent)),
        "Leaving the local viewport must not eagerly render distant consequential Agents.");

    restored = new ChronicleSimulation(restored.State with
    {
        Address = restored.State.CurrentBellAddress,
        Speed = ChronicleSpeed.Paused,
    });
    Assert(restored.Apply(new EndIncarnationAtBell()).Applied &&
           restored.State.Agents[0] == guest,
        "Incarnation death must not reset Tamar, Refuge, the Guest relationship, or road-roll.");
    Assert(restored.Apply(new CreateReplacementIncarnation()).Applied &&
           restored.State.IncarnationId == 2 && restored.State.Agents[0] == guest,
        "Replacement must preserve the same Agent and the prior welcoming Incarnation cause.");
    Assert(restored.Apply(new MoveIncarnation(0, 1)).Applied &&
           restored.Apply(new MoveIncarnation(0, 1)).Applied &&
           restored.Apply(new MoveIncarnation(0, 1)).Applied &&
           restored.State.Address == ChronicleState.AcceptedHomeFixtureAddress,
        "A replacement must return physically to Home to encounter the continuing Guest.");
    AssertRoundTrip(restored.State, "replacement return to the same Guest");

    var replayA = CompleteGoal7AWelcome();
    var replayB = CompleteGoal7AWelcome();
    Assert(
        ChronicleSaveCodec.Serialize(replayA.State) == ChronicleSaveCodec.Serialize(replayB.State) &&
        replayA.AgentContext.Agents.SequenceEqual(replayB.AgentContext.Agents) &&
        replayA.AgentContext.RecentEvents.SequenceEqual(replayB.AgentContext.RecentEvents),
        "The exact Goal 7A command and Heartbeat stream must replay to byte-equal state and ordered Agent snapshots.");

    var v7 = ChronicleSaveCodec.Deserialize(
        ChronicleSaveCodec.SerializeVersion7ForVerification(LegacyGoal6BStart()));
    Assert(v7.WorldGrammarVersion == 5 && v7.Agents.Count == 0,
        "Literal v7 must migrate to strict v9 with no retroactive Agent and its WG5 pin intact.");

    var malformed = JsonNode.Parse(ChronicleSaveCodec.Serialize(welcome.State))!.AsObject();
    malformed["Chronicle"]!["Agents"]![0]!["Profile"]!["DisplayName"] = "Not Tamar";
    AssertThrows(() => ChronicleSaveCodec.Deserialize(malformed.ToJsonString()),
        "Strict v9 must reject a profile whose stored presentation identity disagrees with stable generation.");
}

static ChronicleSimulation CompleteGoal7AResonator()
{
    var simulation = new ChronicleSimulation(ChronicleState.Begin(Seed));
    Assert(simulation.Apply(new ChooseHereIntent()).Applied, "Goal 7A uses the neutral test start.");
    Assert(simulation.Apply(new ReadBurnPrimer()).Applied, "Goal 7A retains the nearby Burn Primer.");
    simulation = At(simulation, new WorldAddress(SurfacePatch.SurfaceStratum, 7, 3));
    Assert(simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Extract)).Applied,
        "Goal 7A must extract the historical Resonant Lode.");
    AdvanceActive(simulation, 2);
    Assert(simulation.Apply(new LiftResonantLode()).Applied,
        "Goal 7A must carry the historical Lode physically.");
    simulation = At(simulation, ChronicleState.AcceptedHomeFixtureAddress);
    Assert(simulation.Apply(new BeginPowerCommitment(PowerCommitmentKind.Build)).Applied,
        "Goal 7A must build the historical Load Source at Home.");
    AdvanceActive(simulation, 3);
    return simulation;
}

static ChronicleSimulation CompleteGoal7AWelcome()
{
    var simulation = CompleteGoal7AResonator();
    AdvanceActive(simulation, 3);
    var identity = simulation.State.Agents[0].Profile.Identity;
    Assert(simulation.Apply(new OfferWelcome(identity)).Applied,
        "The deterministic replay must offer Tamar welcome.");
    AdvanceActive(simulation, 1);
    return simulation;
}

static void VerifyDirectiveRules()
{
    var suggest = WordCatalogue.Get(WordIds.Suggest);
    var command = WordCatalogue.Get(WordIds.Command);
    Assert(
        suggest.Kind == WordKind.Verb && suggest.Load == 1 && suggest.SupportedVerbs.Count == 0 &&
        command.Kind == WordKind.Verb && command.Load == 3 && command.SupportedVerbs.Count == 0,
        "Suggest and Command must retain their bounded authored identities, Loads, and no compatible Modifiers.");

    var simulation = Goal7BSocialFixture(WordIds.Suggest);
    var tamar = simulation.State.Agents[0];
    var safe = DirectiveRules.Action(simulation.State, tamar.Profile.Identity, 0, DirectiveKind.RestByRoadRoll);
    var dangerous = DirectiveRules.Action(simulation.State, tamar.Profile.Identity, 0, DirectiveKind.ApproachMireBrute);
    Assert(
        safe is { Available: true, ActiveForce: SocialVerbForce.Suggest, MinimumForce: SocialVerbForce.Suggest } &&
        dangerous is { Available: false, AvailabilityReason: DirectiveAvailabilityReason.InsufficientForce,
            ActiveForce: SocialVerbForce.Suggest, MinimumForce: SocialVerbForce.Command },
        "Suggest must be enough for the safe request and visibly insufficient for the dangerous request.");

    var alternateProfile = AgentGrammar.Generate(
        Seed,
        6,
        "fixture.second-directive-recipient",
        new WorldAddress(SurfacePatch.SurfaceStratum, 90, 90),
        0);
    var alternate = tamar with { Profile = alternateProfile };
    var alternateState = simulation.State with
    {
        Agents = new AgentCollectionState([alternate]),
    };
    Assert(
        DirectiveRules.Action(alternateState, alternateProfile.Identity, 0, DirectiveKind.RestByRoadRoll).Available &&
        DirectiveRules.Action(alternateState, alternateProfile.Identity, 0, DirectiveKind.ApproachMireBrute)
            .AvailabilityReason == DirectiveAvailabilityReason.InsufficientForce,
        "Directive admissibility must follow equivalent Agent facts, never Tamar's name, seed-derived identity, or UI label.");

    var deadBrute = simulation.State.Combat!.MireBrute with { HitPoints = 0 };
    var missingObjective = simulation.State with
    {
        Combat = simulation.State.Combat with { MireBrute = deadBrute },
    };
    Assert(
        DirectiveRules.Action(missingObjective, tamar.Profile.Identity, 0, DirectiveKind.ApproachMireBrute)
            .AvailabilityReason == DirectiveAvailabilityReason.ObjectiveUnavailable,
        "A dead or missing dangerous objective must reject before Agent consideration.");

    var rejectedState = simulation.State;
    Assert(!simulation.Apply(new DeliverDirective(0, tamar.Profile.Identity, DirectiveKind.ApproachMireBrute)).Applied &&
           simulation.State == rejectedState && tamar.DirectiveMemories.Count == 0,
        "An insufficient Suggest must reject atomically without pending intent, movement, or memory.");

    var sameCell = simulation.State with { Address = tamar.Address };
    Assert(DirectiveRules.Action(sameCell, tamar.Profile.Identity, 0, DirectiveKind.RestByRoadRoll).Available,
        "Standing on Tamar's cell must be valid Directive reach.");
    var distant = simulation.State with { Address = tamar.Address with { X = tamar.Address.X + 3 } };
    Assert(DirectiveRules.Action(distant, tamar.Profile.Identity, 0, DirectiveKind.RestByRoadRoll)
               .AvailabilityReason == DirectiveAvailabilityReason.RecipientOutOfReach,
        "A distant Tamar must make delivery unavailable with an explicit reach reason.");

    Assert(simulation.Apply(new DeliverDirective(0, tamar.Profile.Identity, DirectiveKind.RestByRoadRoll)).Applied &&
           simulation.State.Speed == ChronicleSpeed.Paused &&
           simulation.State.Agents[0].PendingDirective is { ResolvesAtTick: var resolvesAt } &&
           resolvesAt == simulation.State.Tick + 1,
        "Delivering a Directive must pause with one visible pending answer on the next active Heartbeat.");
    var onePending = simulation.State;
    Assert(!simulation.Apply(new DeliverDirective(0, tamar.Profile.Identity, DirectiveKind.RestByRoadRoll)).Applied &&
           simulation.State == onePending,
        "One recipient may retain only one pending Directive.");
    AssertRoundTrip(simulation.State, "pending Suggest Directive");
    Assert(simulation.Apply(new WithdrawDirective(tamar.Profile.Identity)).Applied &&
           simulation.State.Agents[0] is { PendingDirective: null, Intent: AgentIntentKind.RemainAtHome } &&
           simulation.State.Agents[0].DirectiveMemories.Count == 0,
        "The original issuer must be able to withdraw before resolution without creating history.");

    Assert(simulation.Apply(new DeliverDirective(0, tamar.Profile.Identity, DirectiveKind.RestByRoadRoll)).Applied,
        "A withdrawn safe Directive must remain deliverable.");
    AdvanceActive(simulation, 1);
    var accepted = simulation.State.Agents[0];
    Assert(
        accepted.Address == accepted.RoadRollAddress &&
        accepted.PendingDirective is null &&
        accepted.DirectiveMemories.Single() is
        {
            Verb: var acceptedVerb,
            Directive: DirectiveKind.RestByRoadRoll,
            Response: DirectiveResponseKind.Accepted,
            Reason: DirectiveResponseReason.RestAccepted,
            IssuingIncarnationId: 1,
        } && acceptedVerb == WordIds.Suggest &&
        simulation.State.Speed == ChronicleSpeed.Paused,
        "Tamar must autonomously accept the safe Suggest, move one bounded step, remember it, and pause once.");
    var memoryCount = accepted.DirectiveMemories.Count;
    AdvanceActive(simulation, 1);
    Assert(simulation.State.Agents[0].DirectiveMemories.Count == memoryCount &&
           simulation.State.Speed == ChronicleSpeed.Slow,
        "A resolved answer must release pause ownership without invisible retry or repeated memory.");

    var blocked = Goal7BSocialFixture(WordIds.Suggest);
    var blockedTamar = blocked.State.Agents[0];
    blocked = new ChronicleSimulation(blocked.State with
    {
        Address = blockedTamar.RoadRollAddress!.Value,
        Speed = ChronicleSpeed.Paused,
    });
    Assert(blocked.Apply(new DeliverDirective(0, blockedTamar.Profile.Identity, DirectiveKind.RestByRoadRoll)).Applied,
        "The safe Directive must be deliverable while the Incarnation physically blocks its objective.");
    AdvanceActive(blocked, 1);
    var delayed = blocked.State.Agents[0];
    Assert(
        delayed.Address == delayed.WaitingAddress && delayed.PendingDirective is null &&
        delayed.DirectiveMemories.Single() is
        {
            Response: DirectiveResponseKind.Delayed,
            Reason: DirectiveResponseReason.DestinationBlocked,
            Blocker: AgentBlockerKind.Incarnation,
        },
        "A blocked objective must produce one Delayed answer in place and no hidden retry queue.");
    AssertRoundTrip(blocked.State, "delayed blocked Directive");
    blocked = new ChronicleSimulation(blocked.State with
    {
        Address = ChronicleState.AcceptedHomeFixtureAddress,
        Speed = ChronicleSpeed.Paused,
    });
    Assert(blocked.Apply(new DeliverDirective(0, delayed.Profile.Identity, DirectiveKind.RestByRoadRoll)).Applied,
        "Clearing the objective must still require a newly delivered Directive.");
    AdvanceActive(blocked, 1);
    Assert(blocked.State.Agents[0].Address == delayed.RoadRollAddress &&
           blocked.State.Agents[0].DirectiveMemories.Count == 2,
        "The explicit retry may then be accepted once, without catch-up movement.");

    var commanded = Goal7BSocialFixture(WordIds.Command);
    var commandedTamar = commanded.State.Agents[0];
    Assert(
        DirectiveRules.Action(commanded.State, commandedTamar.Profile.Identity, 0, DirectiveKind.RestByRoadRoll).Available &&
        DirectiveRules.Action(commanded.State, commandedTamar.Profile.Identity, 0, DirectiveKind.ApproachMireBrute).Available,
        "Command must satisfy both safe and dangerous delivery thresholds without guaranteeing obedience.");
    var bruteBefore = commanded.State.Combat!.MireBrute;
    Assert(commanded.Apply(new DeliverDirective(0, commandedTamar.Profile.Identity, DirectiveKind.ApproachMireBrute)).Applied,
        "Command must allow delivery of the dangerous Directive.");
    AdvanceActive(commanded, 1);
    Assert(
        commanded.State.Agents[0].Address == commandedTamar.Address &&
        commanded.State.Combat.MireBrute == bruteBefore &&
        commanded.State.Agents[0].DirectiveMemories.Single() is
        {
            Response: DirectiveResponseKind.Refused,
            Reason: DirectiveResponseReason.GuestHasNoViolentCommitment,
        },
        "Tamar must autonomously refuse violence; Command is permission to ask, never unit control.");
    AssertRoundTrip(commanded.State, "refused dangerous Directive");
}

static void VerifyDirectivePersistenceAndMigration()
{
    var pending = Goal7BSocialFixture(WordIds.Suggest);
    var identity = pending.State.Agents[0].Profile.Identity;
    Assert(pending.Apply(new DeliverDirective(0, identity, DirectiveKind.RestByRoadRoll)).Applied,
        "The persistence fixture must open a pending Directive.");
    var pendingLoaded = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(pending.State));
    Assert(pendingLoaded == pending.State && pendingLoaded.Agents[0].PendingDirective is not null,
        "Strict v9 must preserve a pending Directive exactly.");

    var replaced = new ChronicleSimulation(pendingLoaded with
    {
        Address = pendingLoaded.CurrentBellAddress,
        Speed = ChronicleSpeed.Paused,
    });
    Assert(replaced.Apply(new EndIncarnationAtBell()).Applied &&
           replaced.Apply(new CreateReplacementIncarnation()).Applied &&
           replaced.State.IncarnationId == 2,
        "Death and replacement must not erase a pending Directive from the prior Incarnation.");
    var replacementPending = replaced.State;
    Assert(!replaced.Apply(new WithdrawDirective(identity)).Applied && replaced.State == replacementPending,
        "A replacement Incarnation must not withdraw the prior body's pending Directive.");
    AdvanceActive(replaced, 1);
    Assert(replaced.State.Agents[0].DirectiveMemories.Single().IssuingIncarnationId == 1,
        "Resolution after replacement must preserve attribution to the dead issuing Incarnation.");
    AssertRoundTrip(replaced.State, "resolved Directive after replacement");

    var replayA = Goal7BSocialFixture(WordIds.Command);
    var replayB = Goal7BSocialFixture(WordIds.Command);
    foreach (var replay in new[] { replayA, replayB })
    {
        Assert(replay.Apply(new DeliverDirective(0, replay.State.Agents[0].Profile.Identity,
                DirectiveKind.ApproachMireBrute)).Applied,
            "The replay fixture must deliver the dangerous Directive.");
        AdvanceActive(replay, 1);
    }
    Assert(ChronicleSaveCodec.Serialize(replayA.State) == ChronicleSaveCodec.Serialize(replayB.State) &&
           replayA.DirectiveContext.RecentEvents.SequenceEqual(replayB.DirectiveContext.RecentEvents) &&
           replayA.AgentContext.Agents.SequenceEqual(replayB.AgentContext.Agents),
        "The exact Directive command and Heartbeat stream must replay to byte-equal state, Agent snapshots, and ordered events.");

    var scaleBase = ChronicleState.Begin(Seed);
    var scaleAgents = Enumerable.Range(0, 256).Select(index =>
    {
        var provenance = $"directive.scale.{index}";
        var origin = new WorldAddress(SurfacePatch.SurfaceStratum, 5000 + index, 1000);
        var profile = AgentGrammar.Generate(Seed, 6, provenance, origin, index);
        var address = new WorldAddress(SurfacePatch.SurfaceStratum, 7000 + index * 3L, 2000);
        var objective = address with { X = address.X + 1 };
        var memories = new DirectiveMemoryCollectionState([
            new DirectiveMemoryState(profile.Identity, 1, WordIds.Suggest, DirectiveKind.RestByRoadRoll,
                AgentRules.RoadRollIdentity(profile.Identity), objective, 2, 3,
                DirectiveResponseKind.Accepted, DirectiveResponseReason.RestAccepted,
                AgentBlockerKind.None, objective),
            new DirectiveMemoryState(profile.Identity, 1, WordIds.Command, DirectiveKind.ApproachMireBrute,
                "creature.historical-brute", objective with { Y = objective.Y + 1 }, 4, 5,
                DirectiveResponseKind.Refused, DirectiveResponseReason.GuestHasNoViolentCommitment,
                AgentBlockerKind.None, objective),
        ]);
        return new AgentState(profile, address, address, AgentPresenceState.AtHome,
            new AgentNeedState(AgentNeedKind.Refuge, AgentNeedStatus.Satisfied),
            new AgentHomeRelationshipState("holding.home", AgentHomeRelationshipKind.Guest, 4, 1),
            AgentIntentKind.RemainAtHome, 1, 2, 3, objective, null, memories);
    }).ToArray();
    var scaleState = scaleBase with
    {
        Tick = 10,
        Agents = new AgentCollectionState(scaleAgents),
    };
    var scaleLoaded = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.Serialize(scaleState));
    Assert(scaleLoaded.Agents.Count == 256 &&
           scaleLoaded.Agents.Sum(agent => agent.DirectiveMemories.Count) == 512 &&
           scaleLoaded.Agents.SequenceEqual(scaleAgents),
        "Strict v9 must round-trip 256 consequential Agents and 512 ordered Directive memories exactly.");

    var v8Source = CompleteGoal7AWelcome().State;
    var v8Bytes = Encoding.UTF8.GetBytes(ChronicleSaveCodec.SerializeVersion8ForVerification(v8Source));
    var v8Digest = Convert.ToHexString(SHA256.HashData(v8Bytes)).ToLowerInvariant();
    Assert(
        v8Digest == "73195f641365eb8ef3bb39d585bc58a8c0c8d16eb86e3ce61dc74fb7ee161c7a",
        $"The accepted Goal 7A canonical v8 fixture must remain byte-identical; actual {v8Digest}.");
    var migrated = ChronicleSaveCodec.Deserialize(ChronicleSaveCodec.SerializeVersion8ForVerification(v8Source));
    Assert(
        migrated.WorldGrammarVersion == 6 && migrated.Agents.Count == 1 &&
        migrated.Agents[0].PendingDirective is null && migrated.Agents[0].DirectiveMemories.Count == 0 &&
        !migrated.Codex.Contains(WordIds.Suggest) && !migrated.Codex.Contains(WordIds.Command),
        "Literal v8 must migrate once to v9 without retroactive social Words, pending intent, or Directive history.");

    var accepted = Goal7BSocialFixture(WordIds.Suggest);
    Assert(accepted.Apply(new DeliverDirective(0, accepted.State.Agents[0].Profile.Identity,
            DirectiveKind.RestByRoadRoll)).Applied,
        "The malformed-state fixture must deliver a safe Directive.");
    AdvanceActive(accepted, 1);
    var malformed = JsonNode.Parse(ChronicleSaveCodec.Serialize(accepted.State))!.AsObject();
    malformed["Chronicle"]!["Agents"]![0]!["DirectiveMemories"]![0]!["Reason"] = 3;
    AssertThrows(() => ChronicleSaveCodec.Deserialize(malformed.ToJsonString()),
        "Strict v9 must reject a Directive response whose reason disagrees with its result.");
}

static ChronicleSimulation Goal7BSocialFixture(WordId activeVerb)
{
    var accepted = CompleteGoal7AWelcome();
    var learned = accepted.State with
    {
        Codex = accepted.State.Codex.Learn(WordIds.Suggest).Learn(WordIds.Command),
        Speed = ChronicleSpeed.Paused,
    };
    var simulation = new ChronicleSimulation(learned);
    Assert(simulation.Apply(new AttuneExpression(activeVerb, [])).Applied,
        $"The Goal 7B fixture must attune authored social Verb {activeVerb.Value} while safe.");
    return simulation;
}

static void VerifyHistoricalSaveOracle()
{
    var bytes = Encoding.UTF8.GetBytes(
        ChronicleSaveCodec.SerializeVersion7ForVerification(LegacyGoal6BStart()));
    var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    Assert(
        digest == "8adb027068df2ccb5ebf69ff5a6e7e66b75ba88fff4af28e03293689d2d6f5c1",
        $"The accepted pre-6C canonical v7 fixture must remain byte-identical; actual {digest}.");
}

static ChronicleSimulation NewGoal6BFixture()
{
    var simulation = new ChronicleSimulation(LegacyGoal6BStart());
    Assert(simulation.Apply(new ChooseHereIntent()).Applied,
        "The WG5 acceptance Chronicle must use the neutral Home testing start.");
    Assert(simulation.Apply(new ReadBurnPrimer()).Applied,
        "The WG5 acceptance Chronicle must acquire Burn from the physical Primer, not an opening path.");
    return simulation;
}

static ChronicleState LegacyGoal6BStart() => ChronicleState.Begin(Seed) with
{
    WorldGrammarVersion = 5,
    Agents = default,
};

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
        $"Strict v9 must round-trip {phase} exactly.");
}

static void AssertObjective(
    PowerComesHomeContextSnapshot context,
    string stage,
    HoldingObjectiveKind kind,
    HoldingActionKind action,
    int actionHeartbeats = 0,
    HoldingEstablishedFact fact = HoldingEstablishedFact.None,
    HoldingOutcome outcome = HoldingOutcome.None,
    bool showsCarryHomeStep = false,
    int commitmentCompletedTicks = 0,
    int commitmentTotalTicks = 0,
    bool waitingForHeartbeat = false,
    IReadOnlyList<HoldingConstraint>? constraints = null)
{
    var objective = context.Objective;
    Assert(
        objective.Kind == kind &&
        objective.Action == action &&
        objective.ActionHeartbeats == actionHeartbeats &&
        objective.EstablishedFact == fact &&
        objective.NextOutcome == outcome &&
        objective.ShowsCarryHomeStep == showsCarryHomeStep &&
        objective.CommitmentCompletedTicks == commitmentCompletedTicks &&
        objective.CommitmentTotalTicks == commitmentTotalTicks &&
        objective.WaitingForHeartbeat == waitingForHeartbeat &&
        objective.Constraints.SequenceEqual(constraints ?? []),
        $"Goal 6B {stage} guidance must state exactly the authored material facts.");
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

static void AssertArgumentThrows(Action action, string message)
{
    try
    {
        action();
    }
    catch (ArgumentException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}
