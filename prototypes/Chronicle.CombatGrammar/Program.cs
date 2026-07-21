using System.Text;
using Chronicle.CombatGrammarPrototype;

Console.OutputEncoding = Encoding.UTF8;
Console.CursorVisible = false;

var state = CombatModel.Create();

try
{
    while (true)
    {
        Render(state);
        var key = char.ToLowerInvariant(Console.ReadKey(intercept: true).KeyChar);

        if (key == 'q')
        {
            return;
        }

        var command = key switch
        {
            't' => CombatCommand.AdvanceTick,
            'w' => CombatCommand.ToggleWeapon,
            'b' => CombatCommand.StartBurn,
            'x' => CombatCommand.AbortBurn,
            'm' => CombatCommand.CycleBuild,
            'o' => CombatCommand.CycleTarget,
            'c' => CombatCommand.ToggleCompanion,
            'f' => CombatCommand.Retreat,
            'g' => CombatCommand.Reengage,
            'k' => CombatCommand.SkipSafeRecovery,
            'r' => CombatCommand.Reset,
            _ => (CombatCommand?)null,
        };

        if (command is { } selected)
        {
            state = CombatModel.Apply(state, selected);
        }
        else
        {
            state = state with { LastEvent = $"Unknown key '{key}'." };
        }
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
    const string bold = "\x1b[1m";
    const string dim = "\x1b[2m";
    const string reset = "\x1b[0m";

    Console.Write("\x1b[2J\x1b[H");
    Console.WriteLine($"{bold}COMBAT GRAMMAR — THROWAWAY PRESSURE TEST{reset}");
    Console.WriteLine($"{dim}Does physical tempo plus linked Burn create meaningful choices?{reset}");
    Console.WriteLine();

    Console.WriteLine($"{bold}Chronicle{reset}");
    Console.WriteLine($"  Tick             {state.Tick}");
    Console.WriteLine($"  Immediate danger {(state.Threatening ? "YES" : "no")}");
    Console.WriteLine($"  Ground           {(state.GroundScorched ? "SCORCHED" : "unchanged")}");
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
    Console.WriteLine("  [t] tick       [w] weapon on/off   [b] prepare Burn   [x] abandon");
    Console.WriteLine("  [m] next build [o] next Target     [c] Companion      [r] reset");
    Console.WriteLine("  [f] retreat    [g] re-engage       [k] skip safe      [q] quit");
}

static string Bar(int current, int maximum, int width = 20)
{
    var filled = maximum == 0
        ? 0
        : (int)Math.Round(width * Math.Clamp(current, 0, maximum) / (double)maximum);
    return $"[{new string('█', filled)}{new string('·', width - filled)}]";
}
