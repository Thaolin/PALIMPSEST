namespace Chronicle.CombatGrammarPrototype;

public enum BurnBuild
{
    Base,
    Quickly,
    Lasting,
}

public enum BurnTarget
{
    MireBrute,
    BasaltCairn,
}

public enum ClockPace
{
    Paused,
    Slow,
}

public enum CombatCommand
{
    AdvanceTick,
    TogglePace,
    ToggleOpeningWeapon,
    ToggleOpeningCompanion,
    Engage,
    ToggleWeapon,
    StartBurn,
    AbortBurn,
    CycleBuild,
    CycleTarget,
    ToggleCompanion,
    Retreat,
    SkipSafeRecovery,
    Reset,
}

public sealed record CombatState(
    int Tick,
    ClockPace Pace,
    int PlayerHp,
    int EnemyHp,
    int CompanionHp,
    bool CompanionEnabled,
    bool WeaponOnEngage,
    bool CompanionOnEngage,
    bool Threatening,
    bool WeaponAttacking,
    int PlayerWeaponReadyIn,
    int EnemyWeaponReadyIn,
    int CompanionWeaponReadyIn,
    BurnBuild Build,
    BurnTarget Target,
    int BurnPreparationRemaining,
    int BurnRecoveryRemaining,
    int EnemyBurningRemaining,
    bool GroundScorched,
    string LastEvent)
{
    public const int PlayerBaseHp = 30;
    public const int AccessoryHp = 4;
    public const int PlayerMaxHp = PlayerBaseHp + AccessoryHp;
    public const int EnemyMaxHp = 45;
    public const int CompanionMaxHp = 20;
    public const int LoadBudget = 8;

    public bool PlayerAlive => PlayerHp > 0;
    public bool EnemyAlive => EnemyHp > 0;
    public bool CompanionAlive => CompanionEnabled && CompanionHp > 0;
    public bool IsPreparing => BurnPreparationRemaining > 0;
    public bool IsBurning => EnemyBurningRemaining > 0;

    public string Expression => Build switch
    {
        BurnBuild.Base => "Burn",
        BurnBuild.Quickly => "Burn + Quickly",
        BurnBuild.Lasting => "Burn + Lasting",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public int LoadUsed => Build switch
    {
        BurnBuild.Base => 1,
        BurnBuild.Quickly => 7,
        BurnBuild.Lasting => 6,
        _ => throw new ArgumentOutOfRangeException(),
    };

    public int PreparationTicks => Build == BurnBuild.Quickly ? 1 : 3;
    public int BurnDuration => Build == BurnBuild.Lasting ? 6 : 3;

    public string TargetName => Target switch
    {
        BurnTarget.MireBrute => "Mire Brute",
        BurnTarget.BasaltCairn => "Basalt Cairn",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public string TargetFacts => Target switch
    {
        BurnTarget.MireBrute => "living flesh; damp hide; combustible",
        BurnTarget.BasaltCairn => "basalt; anchored; nonflammable",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public string BurnPreview => Target switch
    {
        BurnTarget.MireBrute when !Threatening =>
            "Rejected: the Mire Brute is beyond this fixture's combat range.",
        BurnTarget.MireBrute =>
            $"Valid: {Expression} prepares for {PreparationTicks} tick(s), then burns for {BurnDuration} tick(s).",
        BurnTarget.BasaltCairn =>
            "Rejected: basalt is nonflammable, so Burn has no combustible subject.",
        _ => throw new ArgumentOutOfRangeException(),
    };
}

public static class CombatModel
{
    private const int PlayerWeaponDamage = 5;
    private const int PlayerWeaponInterval = 2;
    private const int ArmorMitigation = 2;
    private const int EnemyWeaponDamage = 7;
    private const int EnemyWeaponInterval = 3;
    private const int CompanionWeaponDamage = 3;
    private const int CompanionWeaponInterval = 2;
    private const int BurnDamage = 4;
    private const int BurnRecovery = 8;

    public static CombatState Create(
        BurnBuild build = BurnBuild.Base,
        bool weaponOnEngage = true,
        bool companionOnEngage = true,
        BurnTarget target = BurnTarget.MireBrute,
        string lastEvent = "Opening plan ready. Engage when you want danger to begin.") =>
        new(
            Tick: 0,
            Pace: ClockPace.Paused,
            PlayerHp: CombatState.PlayerMaxHp,
            EnemyHp: CombatState.EnemyMaxHp,
            CompanionHp: CombatState.CompanionMaxHp,
            CompanionEnabled: false,
            WeaponOnEngage: weaponOnEngage,
            CompanionOnEngage: companionOnEngage,
            Threatening: false,
            WeaponAttacking: false,
            PlayerWeaponReadyIn: 0,
            EnemyWeaponReadyIn: 2,
            CompanionWeaponReadyIn: 0,
            Build: build,
            Target: target,
            BurnPreparationRemaining: 0,
            BurnRecoveryRemaining: 0,
            EnemyBurningRemaining: 0,
            GroundScorched: false,
            LastEvent: lastEvent);

    public static CombatState Apply(CombatState state, CombatCommand command) => command switch
    {
        CombatCommand.AdvanceTick => AdvanceTick(state),
        CombatCommand.TogglePace => TogglePace(state),
        CombatCommand.ToggleOpeningWeapon => ToggleOpeningWeapon(state),
        CombatCommand.ToggleOpeningCompanion => ToggleOpeningCompanion(state),
        CombatCommand.Engage => Engage(state),
        CombatCommand.ToggleWeapon => ToggleWeapon(state),
        CombatCommand.StartBurn => StartBurn(state),
        CombatCommand.AbortBurn => AbortBurn(state),
        CombatCommand.CycleBuild => CycleBuild(state),
        CombatCommand.CycleTarget => CycleTarget(state),
        CombatCommand.ToggleCompanion => ToggleCompanion(state),
        CombatCommand.Retreat => Retreat(state),
        CombatCommand.SkipSafeRecovery => SkipSafeRecovery(state),
        CombatCommand.Reset => Create(
            state.Build,
            state.WeaponOnEngage,
            state.CompanionOnEngage,
            state.Target,
            "Fixture reset to the opening plan."),
        _ => throw new ArgumentOutOfRangeException(nameof(command)),
    };

    private static CombatState TogglePace(CombatState state)
    {
        var pace = state.Pace == ClockPace.Paused
            ? ClockPace.Slow
            : ClockPace.Paused;

        return state with
        {
            Pace = pace,
            LastEvent = pace == ClockPace.Slow
                ? "Slow heartbeat resumed. Press Space or choose a tactical action to pause."
                : "Chronicle paused before the next heartbeat.",
        };
    }

    private static CombatState ToggleOpeningWeapon(CombatState state)
    {
        var enabled = !state.WeaponOnEngage;
        return state with
        {
            WeaponOnEngage = enabled,
            LastEvent = enabled
                ? "Engagement Plan will ready repeated Weapon attacks."
                : "Engagement Plan will hold the Weapon until ordered.",
        };
    }

    private static CombatState ToggleOpeningCompanion(CombatState state)
    {
        var enabled = !state.CompanionOnEngage;
        return state with
        {
            CompanionOnEngage = enabled,
            LastEvent = enabled
                ? "Engagement Plan will call the Ash Hound to screen."
                : "Engagement Plan will begin without the Ash Hound.",
        };
    }

    private static CombatState Engage(CombatState state)
    {
        if (!state.PlayerAlive || !state.EnemyAlive)
        {
            return state with { LastEvent = "Engagement requires both living combatants." };
        }

        if (state.Threatening)
        {
            return state with { LastEvent = "The Mire Brute is already an immediate threat." };
        }

        var callCompanion = state.CompanionOnEngage && state.CompanionHp > 0;
        var companionOutcome = !state.CompanionOnEngage
            ? "held back"
            : callCompanion
                ? "called"
                : "unavailable";
        return state with
        {
            Pace = ClockPace.Paused,
            Threatening = true,
            WeaponAttacking = state.WeaponOnEngage,
            CompanionEnabled = callCompanion,
            PlayerWeaponReadyIn = 0,
            CompanionWeaponReadyIn = 0,
            EnemyWeaponReadyIn = 2,
            LastEvent = $"Engaged and paused before the first heartbeat. Weapon {(state.WeaponOnEngage ? "ready" : "held")}; Ash Hound {companionOutcome}.",
        };
    }

    private static CombatState ToggleWeapon(CombatState state)
    {
        if (!state.PlayerAlive)
        {
            return state with { LastEvent = "The Incarnation is dead and cannot attack." };
        }

        var enabled = !state.WeaponAttacking;
        return state with
        {
            WeaponAttacking = enabled,
            LastEvent = enabled
                ? "Iron Cleaver attacks will repeat whenever the Incarnation is ready."
                : "Iron Cleaver attacks stopped.",
        };
    }

    private static CombatState StartBurn(CombatState state)
    {
        if (!state.PlayerAlive)
        {
            return state with { LastEvent = "The Incarnation is dead and cannot invoke Burn." };
        }

        if (!state.EnemyAlive)
        {
            return state with { LastEvent = "There is no living Mire Brute to Burn." };
        }

        if (state.IsPreparing)
        {
            return state with { LastEvent = "Burn is already being prepared." };
        }

        if (state.Target != BurnTarget.MireBrute || !state.Threatening)
        {
            return state with { LastEvent = state.BurnPreview };
        }

        if (state.BurnRecoveryRemaining > 0)
        {
            return state with
            {
                LastEvent = $"Burn is recovering for {state.BurnRecoveryRemaining} more tick(s).",
            };
        }

        return state with
        {
            BurnPreparationRemaining = state.PreparationTicks,
            LastEvent = $"Preparing {state.Expression}; physical swings pause while commitment continues.",
        };
    }

    private static CombatState AbortBurn(CombatState state) => state.IsPreparing
        ? state with
        {
            BurnPreparationRemaining = 0,
            LastEvent = "Burn Preparation abandoned; physical actions are available next tick.",
        }
        : state with { LastEvent = "No Burn Preparation is active." };

    private static CombatState CycleBuild(CombatState state)
    {
        var next = state.Build switch
        {
            BurnBuild.Base => BurnBuild.Quickly,
            BurnBuild.Quickly => BurnBuild.Lasting,
            BurnBuild.Lasting => BurnBuild.Base,
            _ => throw new ArgumentOutOfRangeException(),
        };

        var expression = next switch
        {
            BurnBuild.Base => "Burn",
            BurnBuild.Quickly => "Burn + Quickly",
            BurnBuild.Lasting => "Burn + Lasting",
            _ => throw new ArgumentOutOfRangeException(),
        };

        return Create(
            next,
            state.WeaponOnEngage,
            state.CompanionOnEngage,
            state.Target,
            $"Attuned {expression}; returned to the opening plan for a clean comparison.");
    }

    private static CombatState CycleTarget(CombatState state)
    {
        var next = state.Target == BurnTarget.MireBrute
            ? BurnTarget.BasaltCairn
            : BurnTarget.MireBrute;

        return state with
        {
            Target = next,
            LastEvent = next == BurnTarget.BasaltCairn
                ? "Selected Basalt Cairn for factual preview."
                : "Selected Mire Brute for factual preview.",
        };
    }

    private static CombatState ToggleCompanion(CombatState state)
    {
        if (!state.CompanionEnabled && state.CompanionHp == 0)
        {
            return state with { LastEvent = "The Ash Hound is down and cannot be recalled." };
        }

        var enabled = !state.CompanionEnabled;
        return state with
        {
            CompanionEnabled = enabled,
            LastEvent = enabled
                ? "Ash Hound joined and will screen autonomously."
                : "Ash Hound withdrew from immediate danger.",
        };
    }

    private static CombatState Retreat(CombatState state)
    {
        if (!state.Threatening)
        {
            return state with { LastEvent = "Already outside the Mire Brute's immediate threat." };
        }

        return state with
        {
            Pace = ClockPace.Paused,
            Threatening = false,
            WeaponAttacking = false,
            CompanionEnabled = false,
            BurnPreparationRemaining = 0,
            LastEvent = state.IsPreparing
                ? "Retreated beyond immediate threat and abandoned Burn Preparation. Recovery remains unchanged."
                : "Retreated beyond immediate threat. Recovery continues with Chronicle time.",
        };
    }

    private static CombatState SkipSafeRecovery(CombatState state)
    {
        if (state.Threatening)
        {
            return state with { LastEvent = "Cannot skip while the Mire Brute is an immediate threat." };
        }

        if (state.IsBurning)
        {
            return AdvanceTick(state) with
            {
                LastEvent = "Burning is a meaningful change, so safe skip advanced only one tick.",
            };
        }

        if (state.BurnRecoveryRemaining == 0)
        {
            return state with { LastEvent = "No Recovery time needs to be skipped." };
        }

        var skipped = state.BurnRecoveryRemaining;
        var current = state;
        for (var i = 0; i < skipped; i++)
        {
            current = AdvanceTick(current);
        }

        return current with
        {
            LastEvent = $"Skipped {skipped} safe Chronicle tick(s); Burn is ready.",
        };
    }

    private static CombatState AdvanceTick(CombatState state)
    {
        if (!state.PlayerAlive)
        {
            return state with
            {
                Pace = ClockPace.Paused,
                LastEvent = "The Incarnation is dead. Reset the fixture to continue.",
            };
        }

        var events = new List<string>();
        var occupiedByPreparation = state.IsPreparing;
        var next = state with
        {
            Tick = state.Tick + 1,
            BurnRecoveryRemaining = Math.Max(0, state.BurnRecoveryRemaining - 1),
            PlayerWeaponReadyIn = Math.Max(0, state.PlayerWeaponReadyIn - 1),
            EnemyWeaponReadyIn = Math.Max(0, state.EnemyWeaponReadyIn - 1),
            CompanionWeaponReadyIn = Math.Max(0, state.CompanionWeaponReadyIn - 1),
        };

        if (next.IsPreparing)
        {
            next = next with
            {
                BurnPreparationRemaining = next.BurnPreparationRemaining - 1,
            };

            if (!next.IsPreparing)
            {
                next = next with
                {
                    EnemyBurningRemaining = Math.Max(next.EnemyBurningRemaining, next.BurnDuration),
                    BurnRecoveryRemaining = BurnRecovery,
                    GroundScorched = true,
                };
                events.Add($"{next.Expression} released; the Mire Brute caught fire and the ground scorched.");
            }
            else
            {
                events.Add($"Burn Preparation: {next.BurnPreparationRemaining} tick(s) remain.");
            }
        }

        if (next.IsBurning && next.EnemyAlive)
        {
            var enemyHp = Math.Max(0, next.EnemyHp - BurnDamage);
            next = next with
            {
                EnemyHp = enemyHp,
                EnemyBurningRemaining = next.EnemyBurningRemaining - 1,
            };
            events.Add($"Burn dealt {BurnDamage} fire damage.");
        }

        if (next.Threatening && next.CompanionAlive && next.EnemyAlive && next.CompanionWeaponReadyIn == 0)
        {
            next = next with
            {
                EnemyHp = Math.Max(0, next.EnemyHp - CompanionWeaponDamage),
                CompanionWeaponReadyIn = CompanionWeaponInterval,
            };
            events.Add($"Ash Hound attacked for {CompanionWeaponDamage}.");
        }

        if (next.Threatening && next.WeaponAttacking && !occupiedByPreparation &&
            next.EnemyAlive && next.PlayerWeaponReadyIn == 0)
        {
            next = next with
            {
                EnemyHp = Math.Max(0, next.EnemyHp - PlayerWeaponDamage),
                PlayerWeaponReadyIn = PlayerWeaponInterval,
            };
            events.Add($"Iron Cleaver struck for {PlayerWeaponDamage}.");
        }

        if (next.Threatening && next.EnemyAlive && next.EnemyWeaponReadyIn == 0)
        {
            if (next.CompanionAlive)
            {
                next = next with
                {
                    CompanionHp = Math.Max(0, next.CompanionHp - EnemyWeaponDamage),
                    EnemyWeaponReadyIn = EnemyWeaponInterval,
                };
                events.Add($"Mire Brute hit the screening Ash Hound for {EnemyWeaponDamage}.");
            }
            else
            {
                var damage = Math.Max(0, EnemyWeaponDamage - ArmorMitigation);
                next = next with
                {
                    PlayerHp = Math.Max(0, next.PlayerHp - damage),
                    EnemyWeaponReadyIn = EnemyWeaponInterval,
                };
                events.Add($"Mire Brute hit the Quilted Jack for {damage} after Armor.");
            }
        }

        if (!next.EnemyAlive)
        {
            next = next with
            {
                Pace = ClockPace.Paused,
                Threatening = false,
                BurnPreparationRemaining = 0,
                EnemyBurningRemaining = 0,
            };
            events.Add("The Mire Brute fell. Immediate danger ended; the scorch remains.");
        }

        if (!next.PlayerAlive)
        {
            next = next with
            {
                Pace = ClockPace.Paused,
                Threatening = false,
                BurnPreparationRemaining = 0,
            };
            events.Add("The Incarnation died. The fixture awaits reset.");
        }

        if (events.Count == 0)
        {
            events.Add("Chronicle time advanced; no combatant became ready this tick.");
        }

        return next with { LastEvent = string.Join(" ", events) };
    }
}
