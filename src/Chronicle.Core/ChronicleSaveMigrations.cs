using System.Text.Json;

namespace Chronicle.Core;

public static partial class ChronicleSaveCodec
{
    private static ChronicleState MigratePredecessor(
        PredecessorChronicleState predecessor,
        bool migrateWorldGrammarOne)
    {
        if (predecessor.Intent is not (OpeningIntent.Unchosen or OpeningIntent.Up))
        {
            throw new InvalidOperationException(
                "Predecessor Chronicle saves only support Unchosen or UP Intent.");
        }

        if (predecessor.WorldGrammarVersion is not (0 or 1 or 2))
        {
            throw new InvalidOperationException(
                "Predecessor Chronicle saves only support World Grammar pins 0, 1, or 2.");
        }

        var codex = new CodexState(
            predecessor.Codex?.HasFly ?? false,
            predecessor.Codex?.HasStone ?? false);
        var study = new StudyState(
            predecessor.Study?.StoneUnderstanding ?? 0,
            predecessor.Study?.IsStudyingBell ?? false);
        var loadout = predecessor.Loadout is null
            ? (LoadoutState?)null
            : new LoadoutState(
                MigrateSlot(predecessor.Loadout.Slot1),
                MigrateSlot(predecessor.Loadout.Slot2),
                MigrateSlot(predecessor.Loadout.Slot3),
                MigrateSlot(predecessor.Loadout.Slot4),
                MigrateSlot(predecessor.Loadout.Slot5),
                MigrateSlot(predecessor.Loadout.Slot6),
                MigrateSlot(predecessor.Loadout.Slot7),
                MigrateSlot(predecessor.Loadout.Slot8));
        var worldGrammarVersion =
            migrateWorldGrammarOne && predecessor.WorldGrammarVersion == 1
                ? 2
                : predecessor.WorldGrammarVersion;

        return new ChronicleState(
            predecessor.Seed,
            predecessor.Tick,
            predecessor.Address,
            predecessor.Speed,
            predecessor.Intent,
            codex,
            study,
            loadout,
            predecessor.LooseStoneAddress,
            predecessor.IncarnationId,
            predecessor.IncarnationLife,
            worldGrammarVersion).MigrateAndValidate();
    }

    private static ChronicleState MigrateVersion2(Version2ChronicleState predecessor)
    {
        if (predecessor.Intent is not (OpeningIntent.Unchosen or OpeningIntent.Up))
        {
            throw new InvalidOperationException(
                "Version 2 Chronicle saves only support Unchosen or UP Intent.");
        }

        if (predecessor.WorldGrammarVersion is not (0 or 1 or 2))
        {
            throw new InvalidOperationException(
                "Version 2 Chronicle saves only support World Grammar pins 0, 1, or 2.");
        }

        var state = new ChronicleState(
            predecessor.Seed,
            predecessor.Tick,
            predecessor.Address,
            predecessor.Speed,
            predecessor.Intent,
            predecessor.Codex,
            predecessor.Study,
            predecessor.Loadout,
            predecessor.LooseStoneAddress,
            predecessor.IncarnationId,
            predecessor.IncarnationLife,
            predecessor.WorldGrammarVersion,
            Home: null);
        var migrated = state.MigrateAndValidate();
        ValidateCurrentState(migrated);
        return migrated;
    }

    private static ChronicleState MigrateVersion3(Version3ChronicleState predecessor)
    {
        if (predecessor.Intent == OpeningIntent.Against ||
            predecessor.WorldGrammarVersion is not (0 or 1 or 2))
        {
            throw new InvalidOperationException(
                "Version 3 Chronicle saves only support World Grammar pins 0, 1, or 2 without AGAINST.");
        }

        var migrated = new ChronicleState(
            predecessor.Seed,
            predecessor.Tick,
            predecessor.Address,
            predecessor.Speed,
            predecessor.Intent,
            predecessor.Codex,
            predecessor.Study,
            predecessor.Loadout,
            predecessor.LooseStoneAddress,
            predecessor.IncarnationId,
            predecessor.IncarnationLife,
            predecessor.WorldGrammarVersion,
            predecessor.Home,
            FirstConflict: null,
            BellAddress: SkyStratum.LandmarkAddress).MigrateAndValidate();
        ValidateCurrentState(migrated);
        return migrated;
    }

    private static ChronicleState MigrateVersion4(Version4ChronicleState predecessor)
    {
        var migrated = new ChronicleState(
            predecessor.Seed,
            predecessor.Tick,
            predecessor.Address,
            predecessor.Speed,
            predecessor.Intent,
            predecessor.Codex,
            predecessor.Study,
            predecessor.Loadout,
            predecessor.LooseStoneAddress,
            predecessor.IncarnationId,
            predecessor.IncarnationLife,
            predecessor.WorldGrammarVersion,
            predecessor.Home,
            predecessor.FirstConflict,
            BellAddress: SkyStratum.LandmarkAddress).MigrateAndValidate();
        ValidateCurrentState(migrated);
        return migrated;
    }

    private static LoadoutSlot MigrateSlot(PredecessorLoadoutSlot? slot)
    {
        if (slot is null)
        {
            return new LoadoutSlot();
        }

        var verb = slot.Verb switch
        {
            null => (WordId?)null,
            1 => WordIds.Fly,
            _ => throw new InvalidOperationException(
                $"Unknown predecessor Loadout Verb value '{slot.Verb}'."),
        };
        var noun = slot.Noun switch
        {
            null => (WordId?)null,
            1 => WordIds.Stone,
            _ => throw new InvalidOperationException(
                $"Unknown predecessor Loadout Noun value '{slot.Noun}'."),
        };
        return new LoadoutSlot(verb, noun);
    }
}
