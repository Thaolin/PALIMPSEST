using System.Text;
using Chronicle.CombatGrammarPrototype;

Console.OutputEncoding = Encoding.UTF8;
Console.CursorVisible = false;

var state = CombatModel.Create();
const int slowHeartbeatMilliseconds = 800;
var nextHeartbeat = DateTime.UtcNow.AddMilliseconds(slowHeartbeatMilliseconds);
var needsRender = true;

try
{
    while (true)
    {
        if (needsRender)
        {
            Render(state);
            needsRender = false;
        }

        if (!Console.KeyAvailable)
        {
            if (state.Pace == ClockPace.Slow && DateTime.UtcNow >= nextHeartbeat)
            {
                state = CombatModel.Apply(state, CombatCommand.AdvanceTick);
                nextHeartbeat = DateTime.UtcNow.AddMilliseconds(slowHeartbeatMilliseconds);
                needsRender = true;
            }
            else
            {
                Thread.Sleep(20);
            }

            continue;
        }

        var keyInfo = Console.ReadKey(intercept: true);
        var key = char.ToLowerInvariant(keyInfo.KeyChar);

        if (key == 'q')
        {
            return;
        }

        var command = keyInfo.Key == ConsoleKey.Spacebar
            ? CombatCommand.TogglePace
            : key switch
        {
            '1' => CombatCommand.ToggleOpeningWeapon,
            '2' => CombatCommand.ToggleOpeningCompanion,
            'e' => CombatCommand.Engage,
            't' => CombatCommand.AdvanceTick,
            'w' => CombatCommand.ToggleWeapon,
            'b' => CombatCommand.StartBurn,
            'x' => CombatCommand.AbortBurn,
            'm' => CombatCommand.CycleBuild,
            'o' => CombatCommand.CycleTarget,
            'c' => CombatCommand.ToggleCompanion,
            'f' => CombatCommand.Retreat,
            'k' => CombatCommand.SkipSafeRecovery,
            'r' => CombatCommand.Reset,
            _ => (CombatCommand?)null,
        };

        if (command is { } selected)
        {
            if (state.Pace == ClockPace.Slow && selected != CombatCommand.TogglePace)
            {
                state = CombatModel.Apply(state, CombatCommand.TogglePace);
            }

            state = CombatModel.Apply(state, selected);
        }
        else
        {
            state = state with { LastEvent = $"Unknown key '{key}'." };
        }

        nextHeartbeat = DateTime.UtcNow.AddMilliseconds(slowHeartbeatMilliseconds);
        needsRender = true;
    }
}
finally
{
    Console.CursorVisible = true;
    Console.ResetColor();
    Console.WriteLine();
}

static void Render(CombatState state)
{
    const int slowHeartbeatMilliseconds = 800;
    const string bold = "\x1b[1m";
    const string dim = "\x1b[2m";
    const string reset = "\x1b[0m";

    Console.Write("\x1b[2J\x1b[H");
    Console.WriteLine($"{bold}COMBAT GRAMMAR — THROWAWAY PRESSURE TEST{reset}");
    Console.WriteLine($"{dim}Paused decisions inside a Slow deterministic heartbeat.{reset}");
    Console.WriteLine();

    Console.WriteLine($"{bold}Chronicle{reset}");
    Console.WriteLine($"  Tick             {state.Tick}");
    Console.WriteLine($"  Clock            {state.Pace.ToString().ToUpperInvariant()} (Slow heartbeat: {slowHeartbeatMilliseconds} ms)");
    Console.WriteLine($"  Immediate danger {(state.Threatening ? "YES" : "no")}");
    Console.WriteLine($"  Ground           {(state.GroundScorched ? "SCORCHED" : "unchanged")}");
    Console.WriteLine();

    Console.WriteLine($"{bold}Engagement plan{reset}");
    Console.WriteLine($"  [1] Ready weapon     {(state.WeaponOnEngage ? "ON" : "off")}");
    Console.WriteLine($"  [2] Call Companion   {(state.CompanionOnEngage ? "ON" : "off")}");
    Console.WriteLine($"  Contact behavior     apply plan, then PAUSE before danger advances");
    Console.WriteLine();

    Console.WriteLine($"{bold}Incarnation{reset}");
    Console.WriteLine($"  HP               {Bar(state.PlayerHp, CombatState.PlayerMaxHp)} {state.PlayerHp}/{CombatState.PlayerMaxHp}");
    Console.WriteLine("  Weapon           Iron Cleaver — 5 damage / 2 ticks");
    Console.WriteLine("  Armor            Quilted Jack — 2 physical mitigation");
    Console.WriteLine("  Accessory        Copper Ward — +4 HP");
    Console.WriteLine($"  Weapon stance    {(state.WeaponAttacking ? "ATTACKING" : "held")} (ready in {state.PlayerWeaponReadyIn})");
    Console.WriteLine();

    Console.WriteLine($"{bold}Expression{reset}");
    Console.WriteLine($"  Attuned          {state.Expression}");
    Console.WriteLine($"  Shared Load      {state.LoadUsed}/{CombatState.LoadBudget}");
    Console.WriteLine($"  Preparation      {state.BurnPreparationRemaining}");
    Console.WriteLine($"  Recovery         {state.BurnRecoveryRemaining}");
    Console.WriteLine($"  Target           {state.TargetName}");
    Console.WriteLine($"  Target facts     {state.TargetFacts}");
    Console.WriteLine($"  Preview          {state.BurnPreview}");
    Console.WriteLine();

    Console.WriteLine($"{bold}Mire Brute{reset}");
    Console.WriteLine($"  HP               {Bar(state.EnemyHp, CombatState.EnemyMaxHp)} {state.EnemyHp}/{CombatState.EnemyMaxHp}");
    Console.WriteLine($"  Swing ready in   {state.EnemyWeaponReadyIn}");
    Console.WriteLine($"  Burning          {state.EnemyBurningRemaining} tick(s)");
    Console.WriteLine();

    Console.WriteLine($"{bold}Companion{reset}");
    Console.WriteLine($"  Ash Hound        {(state.CompanionEnabled ? "SCREENING AUTONOMOUSLY" : "absent")}");
    Console.WriteLine($"  HP               {Bar(state.CompanionEnabled ? state.CompanionHp : 0, CombatState.CompanionMaxHp)} {(state.CompanionEnabled ? $"{state.CompanionHp}/{CombatState.CompanionMaxHp}" : "—")}");
    Console.WriteLine($"  Bite ready in    {(state.CompanionEnabled ? state.CompanionWeaponReadyIn : 0)}");
    Console.WriteLine();

    Console.WriteLine($"{bold}Latest event{reset}");
    Console.WriteLine($"  {state.LastEvent}");
    Console.WriteLine();

    Console.WriteLine($"{bold}Commands{reset}");
    Console.WriteLine("  [1/2] opening plan   [e] engage     [Space] pause / Slow");
    Console.WriteLine("  [t] one tick         [w] weapon     [c] call/dismiss Companion");
    Console.WriteLine("  [b] prepare Burn     [x] abandon    [m] next build   [o] Target");
    Console.WriteLine("  [f] retreat          [k] skip safe  [r] reset        [q] quit");
    Console.WriteLine($"{dim}Any tactical command entered during Slow pauses the clock before it acts.{reset}");
}

static string Bar(int current, int maximum, int width = 20)
{
    var filled = maximum == 0
        ? 0
        : (int)Math.Round(width * Math.Clamp(current, 0, maximum) / (double)maximum);
    return $"[{new string('█', filled)}{new string('·', width - filled)}]";
}
