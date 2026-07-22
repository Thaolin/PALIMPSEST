using System.Text.Json;

namespace Chronicle.Core;

public static partial class ChronicleSaveCodec
{
    private static void ValidateVersion3Document(JsonElement chronicle)
    {
        ValidateCurrentDocument(chronicle);
        ValidateAllowedWordIdentities(
            chronicle,
            "Version 3",
            WordIds.Fly,
            WordIds.Found,
            WordIds.Stone,
            WordIds.Bell);
    }

    private static void ValidateCurrentDocument(JsonElement chronicle)
    {
        RequireObjectWithProperties(
            chronicle,
            "Chronicle",
            "Seed",
            "Tick",
            "Address",
            "Speed",
            "Intent",
            "Codex",
            "Study",
            "Loadout",
            "LooseStoneAddress",
            "IncarnationId",
            "IncarnationLife",
            "WorldGrammarVersion",
            "Home");
        RequireExactObjectWithProperties(
            chronicle.GetProperty("Address"),
            "Chronicle Address",
            "Stratum",
            "X",
            "Y");
        RequireExactObjectWithProperties(
            chronicle.GetProperty("LooseStoneAddress"),
            "loose-Stone Address",
            "Stratum",
            "X",
            "Y");

        var codex = chronicle.GetProperty("Codex");
        RequireExactObjectWithProperties(codex, "Codex", "Words");
        ValidateCodexWords(codex.GetProperty("Words"));
        var study = chronicle.GetProperty("Study");
        RequireExactObjectWithProperties(
            study,
            "Study",
            "Understanding",
            "ActiveSourceId",
            "ActiveWord");
        var understanding = study.GetProperty("Understanding");
        ValidateUnderstandingEntries(understanding);

        var loadout = chronicle.GetProperty("Loadout");
        RequireExactObjectWithProperties(
            loadout,
            "Loadout",
            "Slot1",
            "Slot2",
            "Slot3",
            "Slot4",
            "Slot5",
            "Slot6",
            "Slot7",
            "Slot8");
        for (var index = 1; index <= LoadoutState.SlotCount; index++)
        {
            RequireExactObjectWithProperties(
                loadout.GetProperty($"Slot{index}"),
                $"Loadout slot {index}",
                "Verb",
                "Noun");
        }

        var home = chronicle.GetProperty("Home");

        if (home.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        RequireExactObjectWithProperties(
            home,
            "Home",
            "HoldingId",
            "DisplayName",
            "Address",
            "FoundedTick",
            "FoundingIncarnationId",
            "Material");
        RequireExactObjectWithProperties(
            home.GetProperty("Address"),
            "Home Address",
            "Stratum",
            "X",
            "Y");
    }

    private static void ValidateVersion4Document(JsonElement chronicle)
    {
        RequireExactObjectWithProperties(
            chronicle,
            "Chronicle",
            "Seed",
            "Tick",
            "Address",
            "Speed",
            "Intent",
            "Codex",
            "Study",
            "Loadout",
            "LooseStoneAddress",
            "IncarnationId",
            "IncarnationLife",
            "WorldGrammarVersion",
            "Home",
            "FirstConflict");
        ValidateCurrentDocument(chronicle);
        ValidateConflictDocument(chronicle.GetProperty("FirstConflict"));
    }

    private static void ValidateVersion5Document(JsonElement chronicle)
    {
        RequireExactObjectWithProperties(
            chronicle,
            "Chronicle",
            "Seed",
            "Tick",
            "Address",
            "Speed",
            "Intent",
            "Codex",
            "Study",
            "Loadout",
            "LooseStoneAddress",
            "IncarnationId",
            "IncarnationLife",
            "WorldGrammarVersion",
            "Home",
            "FirstConflict",
            "BellAddress");
        ValidateCurrentDocument(chronicle);
        ValidateConflictDocument(chronicle.GetProperty("FirstConflict"));
        ValidateAllowedWordIdentities(
            chronicle,
            "Version 5",
            WordIds.Fly,
            WordIds.Found,
            WordIds.Smash,
            WordIds.Stone,
            WordIds.Bell);
        if (!chronicle.GetProperty("WorldGrammarVersion").TryGetInt32(out var grammarVersion) ||
            grammarVersion is < 0 or > 3)
        {
            throw new InvalidOperationException("Version 5 saves support only predecessor World Grammar pins 0 through 3.");
        }
        RequireExactObjectWithProperties(
            chronicle.GetProperty("BellAddress"),
            "Bell Address",
            "Stratum",
            "X",
            "Y");
    }

    private static void ValidateVersion6Document(JsonElement chronicle)
    {
        RequireExactObjectWithProperties(
            chronicle,
            "Version 6 Chronicle",
            "Seed",
            "Tick",
            "Address",
            "Speed",
            "Intent",
            "Codex",
            "Loadout",
            "IncarnationId",
            "IncarnationLife",
            "WorldGrammarVersion",
            "Combat",
            "RetainedDurables");
        ValidateV6Address(chronicle.GetProperty("Address"), "Chronicle Address");

        var codex = chronicle.GetProperty("Codex");
        RequireExactObjectWithProperties(codex, "Codex", "Words");
        ValidateCodexWords(codex.GetProperty("Words"));
        foreach (var wordElement in codex.GetProperty("Words").EnumerateArray())
        {
            var word = ReadKnownWordId(wordElement, "Version 6 Codex Word");
            if (WordCatalogue.Get(word).Kind == WordKind.Noun)
            {
                throw new InvalidOperationException(
                    $"Version 6 Codex cannot retain predecessor Noun '{word}'.");
            }
        }

        ValidateV6Loadout(chronicle.GetProperty("Loadout"), "Loadout");
        ValidateV6Combat(chronicle.GetProperty("Combat"), expressionHasSecondModifier: false);
        ValidateV6RetainedDurables(chronicle.GetProperty("RetainedDurables"));
    }

    private static void ValidateVersion7Document(JsonElement chronicle)
    {
        RequireExactObjectWithProperties(
            chronicle,
            "Version 7 Chronicle",
            "Seed", "Tick", "Address", "Speed", "Intent", "Codex", "Loadout",
            "Attunement", "IncarnationId", "IncarnationLife", "WorldGrammarVersion",
            "Combat", "PowerHome", "RetainedDurables");
        ValidateV6Address(chronicle.GetProperty("Address"), "Chronicle Address");

        var codex = chronicle.GetProperty("Codex");
        RequireExactObjectWithProperties(codex, "Codex", "Words");
        ValidateCodexWords(codex.GetProperty("Words"));
        foreach (var wordElement in codex.GetProperty("Words").EnumerateArray())
        {
            var word = ReadKnownWordId(wordElement, "Version 7 Codex Word");
            if (WordCatalogue.Get(word).Kind == WordKind.Noun)
            {
                throw new InvalidOperationException(
                    $"Version 7 Codex cannot retain predecessor Noun '{word}'.");
            }
        }

        var loadout = chronicle.GetProperty("Loadout");
        RequireExactObjectWithProperties(loadout, "Version 7 Loadout", "Verb", "Modifiers");
        RequireArray(loadout.GetProperty("Modifiers"), "Version 7 Loadout Modifiers");
        if (loadout.GetProperty("Modifiers").GetArrayLength() > 2)
        {
            throw new InvalidOperationException("Version 7 Loadout supports at most two Modifiers.");
        }

        var modifierIds = loadout.GetProperty("Modifiers")
            .EnumerateArray()
            .Select(element => ReadKnownWordId(element, "Version 7 Loadout Modifier"))
            .ToArray();
        if (modifierIds.Any(id => WordCatalogue.Get(id).Kind != WordKind.Modifier) ||
            modifierIds.Distinct().Count() != modifierIds.Length ||
            !WordCatalogue.Canonicalize(modifierIds).SequenceEqual(modifierIds))
        {
            throw new InvalidOperationException(
                "Version 7 Loadout Modifiers must be known, unique, and in canonical order.");
        }

        var attunement = chronicle.GetProperty("Attunement");
        if (attunement.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(attunement, "Attunement", "Capacity", "Tick");
        }

        ValidateV6Combat(chronicle.GetProperty("Combat"), expressionHasSecondModifier: true);
        ValidateV7PowerHome(chronicle.GetProperty("PowerHome"));
        ValidateV6RetainedDurables(chronicle.GetProperty("RetainedDurables"));
    }

    private static void ValidateV7PowerHome(JsonElement power)
    {
        if (power.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        RequireExactObjectWithProperties(
            power,
            "Power Comes Home state",
            "Lode", "ExtractionProgress", "Resonator", "Commitment");
        var lode = power.GetProperty("Lode");
        RequireExactObjectWithProperties(
            lode,
            "Resonant Lode",
            "Identity", "OriginAddress", "Disposition", "Address", "CarrierIncarnationId");
        ValidateV6Address(lode.GetProperty("OriginAddress"), "Resonant Lode origin Address");
        if (lode.GetProperty("Address").ValueKind != JsonValueKind.Null)
        {
            ValidateV6Address(lode.GetProperty("Address"), "Resonant Lode Address");
        }

        var source = power.GetProperty("Resonator");
        if (source.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                source,
                "Hearth Resonator",
                "Identity", "Address", "Phase", "Progress");
            ValidateV6Address(source.GetProperty("Address"), "Hearth Resonator Address");
        }

        var commitment = power.GetProperty("Commitment");
        if (commitment.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                commitment,
                "Power Comes Home commitment",
                "Kind", "ActorIncarnationId", "SubjectIdentity", "Address", "CompletedTicks", "TotalTicks");
            ValidateV6Address(commitment.GetProperty("Address"), "Power commitment Address");
        }
    }

    private static void ValidateV6Loadout(JsonElement loadout, string name)
    {
        RequireExactObjectWithProperties(
            loadout,
            name,
            "Verb",
            "Modifier");
    }

    private static void ValidateV6Address(JsonElement address, string name)
    {
        RequireExactObjectWithProperties(address, name, "Stratum", "X", "Y");
    }

    private static void ValidateV6RetainedDurables(JsonElement retained)
    {
        if (retained.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        RequireExactObjectWithProperties(
            retained,
            "retained predecessor durables",
            "LooseStoneAddress",
            "BellAddress",
            "Home",
            "RivenCairn");
        ValidateV6Address(retained.GetProperty("LooseStoneAddress"), "retained loose-Stone Address");
        ValidateV6Address(retained.GetProperty("BellAddress"), "retained Bell Address");
        var home = retained.GetProperty("Home");
        if (home.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                home,
                "retained Home",
                "HoldingId",
                "DisplayName",
                "Address",
                "FoundedTick",
                "FoundingIncarnationId",
                "Material");
            ValidateV6Address(home.GetProperty("Address"), "retained Home Address");
        }

        var cairn = retained.GetProperty("RivenCairn");
        if (cairn.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                cairn,
                "retained Riven Cairn",
                "Address",
                "ResolvedTick",
                "ResolvingIncarnationId");
            ValidateV6Address(cairn.GetProperty("Address"), "retained Riven Cairn Address");
        }
    }

    private static void ValidateV6Combat(JsonElement combat, bool expressionHasSecondModifier)
    {
        if (combat.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        RequireExactObjectWithProperties(
            combat,
            "Goal 6A Combat",
            "IncarnationHitPoints",
            "Equipment",
            "EngagementPlan",
            "WeaponStanceActive",
            "WeaponTicksUntilReady",
            "EngagementActive",
            "MireBrute",
            "PendingAction",
            "Preparation",
            "OngoingBurn",
            "RecoveryRemaining",
            "Scorch");
        RequireExactObjectWithProperties(
            combat.GetProperty("Equipment"),
            "Goal 6A Equipment",
            "WeaponIdentity",
            "WeaponName",
            "ArmorIdentity",
            "ArmorName",
            "AccessoryIdentity",
            "AccessoryName",
            "MaximumHitPointBonus",
            "PhysicalDamageReduction");
        RequireExactObjectWithProperties(
            combat.GetProperty("EngagementPlan"),
            "Engagement Plan",
            "OpenWithWeaponStance");
        var brute = combat.GetProperty("MireBrute");
        RequireExactObjectWithProperties(
            brute,
            "Mire Brute",
            "Identity",
            "OriginAddress",
            "Address",
            "HitPoints",
            "SwingTicksRemaining",
            "DefeatedTick");
        ValidateV6Address(brute.GetProperty("OriginAddress"), "Mire Brute origin Address");
        ValidateV6Address(brute.GetProperty("Address"), "Mire Brute Address");

        var pending = combat.GetProperty("PendingAction");
        if (pending.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                pending,
                "pending tactical action",
                "Kind",
                "DeltaX",
                "DeltaY",
                "WeaponStanceActive",
                "Target");
            if (pending.GetProperty("Target").ValueKind != JsonValueKind.Null)
            {
                ValidateV6Address(pending.GetProperty("Target"), "pending tactical Target");
            }
        }

        var preparation = combat.GetProperty("Preparation");
        if (preparation.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                preparation,
                "Burn Preparation",
                "ActorIncarnationId",
                "TargetIdentity",
                "TargetAddressAtPreparation",
                "Expression",
                "RemainingTicks");
            ValidateV6Address(
                preparation.GetProperty("TargetAddressAtPreparation"),
                "Burn Preparation Target Address");
            if (expressionHasSecondModifier)
            {
                RequireExactObjectWithProperties(
                    preparation.GetProperty("Expression"),
                    "Burn Preparation Expression",
                    "Verb",
                    "Noun",
                    "Modifier",
                    "Modifier2");
            }
            else
            {
                RequireExactObjectWithProperties(
                    preparation.GetProperty("Expression"),
                    "Burn Preparation Expression",
                    "Verb",
                    "Noun",
                    "Modifier");
            }
        }

        var burn = combat.GetProperty("OngoingBurn");
        if (burn.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                burn,
                "ongoing Burn",
                "TargetIdentity",
                "Damage",
                "RemainingTicks");
        }

        var scorch = combat.GetProperty("Scorch");
        if (scorch.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(scorch, "scorched ground", "Address", "CreatedTick");
            ValidateV6Address(scorch.GetProperty("Address"), "scorched ground Address");
        }
    }

    private static void ValidatePreVersion5LoadoutCompatibility(
        JsonElement chronicle,
        string saveName)
    {
        var loadout = chronicle.GetProperty("Loadout");
        for (var index = 1; index <= LoadoutState.SlotCount; index++)
        {
            var slot = loadout.GetProperty($"Slot{index}");
            var verb = slot.GetProperty("Verb");
            var noun = slot.GetProperty("Noun");
            if (verb.ValueKind == JsonValueKind.String &&
                noun.ValueKind == JsonValueKind.String)
            {
                var verbId = new WordId(verb.GetString()!);
                var nounId = new WordId(noun.GetString()!);
                if (!PreVersion5CompatibleNouns.TryGetValue(verbId, out var compatibleNouns) ||
                    !compatibleNouns.Contains(nounId))
                {
                    throw new InvalidOperationException(
                        $"{saveName} saves cannot contain later fitted compatibility '{verbId}[{nounId}]'.");
                }
            }
        }
    }

    private static void ValidateConflictDocument(JsonElement conflict)
    {
        if (conflict.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        RequireExactObjectWithProperties(
            conflict,
            "First Conflict",
            "SubjectId",
            "Address",
            "ThreatenedTick",
            "PendingAction",
            "Outcome",
            "ResolvedTick",
            "ResolvingIncarnationId");
        RequireExactObjectWithProperties(
            conflict.GetProperty("Address"),
            "First Conflict Address",
            "Stratum",
            "X",
            "Y");

        var pendingAction = conflict.GetProperty("PendingAction");
        if (pendingAction.ValueKind != JsonValueKind.Null)
        {
            RequireExactObjectWithProperties(
                pendingAction,
                "First Conflict pending Loadout action",
                "Verb",
                "Noun");
        }

        RequireNullableNumber(conflict.GetProperty("Outcome"), "First Conflict outcome");
        RequireNullableNumber(conflict.GetProperty("ResolvedTick"), "First Conflict resolution tick");
        RequireNullableNumber(
            conflict.GetProperty("ResolvingIncarnationId"),
            "First Conflict resolving Incarnation identity");
    }

    private static void ValidateAllowedWordIdentities(
        JsonElement chronicle,
        string saveName,
        params WordId[] allowedWords)
    {
        void ValidateWord(JsonElement element, string field)
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                return;
            }

            var word = ReadKnownWordId(element, $"{saveName} {field}");
            if (!allowedWords.Contains(word))
            {
                throw new InvalidOperationException(
                    $"{saveName} saves cannot contain later Word identity '{word}'.");
            }
        }

        foreach (var word in chronicle
                     .GetProperty("Codex")
                     .GetProperty("Words")
                     .EnumerateArray())
        {
            ValidateWord(word, "Codex Word");
        }

        var study = chronicle.GetProperty("Study");
        foreach (var entry in study.GetProperty("Understanding").EnumerateArray())
        {
            ValidateWord(entry.GetProperty("Word"), "Study Understanding Word");
        }

        ValidateWord(study.GetProperty("ActiveWord"), "active Study Word");

        var loadout = chronicle.GetProperty("Loadout");
        for (var index = 1; index <= LoadoutState.SlotCount; index++)
        {
            var slot = loadout.GetProperty($"Slot{index}");
            foreach (var property in new[] { "Verb", "Noun" })
            {
                ValidateWord(slot.GetProperty(property), $"Loadout {property}");
            }
        }
    }

    private static void ValidateVersion2Document(JsonElement chronicle)
    {
        RequireObjectWithProperties(
            chronicle,
            "Chronicle",
            "Seed",
            "Tick",
            "Address",
            "Speed",
            "Intent",
            "Codex",
            "Study",
            "Loadout",
            "LooseStoneAddress",
            "IncarnationId",
            "IncarnationLife",
            "WorldGrammarVersion");
        RequireObjectWithProperties(
            chronicle.GetProperty("Address"),
            "Chronicle Address",
            "Stratum",
            "X",
            "Y");
        RequireObjectWithProperties(
            chronicle.GetProperty("LooseStoneAddress"),
            "loose-Stone Address",
            "Stratum",
            "X",
            "Y");

        var loadout = chronicle.GetProperty("Loadout");
        RequireObjectWithProperties(
            loadout,
            "Loadout",
            "Slot1",
            "Slot2",
            "Slot3",
            "Slot4",
            "Slot5",
            "Slot6",
            "Slot7",
            "Slot8");
        for (var index = 1; index <= LoadoutState.SlotCount; index++)
        {
            RequireObjectWithProperties(
                loadout.GetProperty($"Slot{index}"),
                $"Loadout slot {index}",
                "Verb",
                "Noun");
        }

        ValidateAllowedWordIdentities(
            chronicle,
            "Version 2",
            WordIds.Fly,
            WordIds.Stone,
            WordIds.Bell);
    }

    private static void ValidatePredecessorDocument(
        JsonElement chronicle,
        string name,
        bool requireIntentAndGrammar)
    {
        RequireObjectWithProperties(
            chronicle,
            name,
            "Seed",
            "Tick",
            "Address",
            "Speed");
        if (requireIntentAndGrammar)
        {
            RequireObjectWithProperties(
                chronicle,
                name,
                "Intent",
                "WorldGrammarVersion");
        }

        RequireObjectWithProperties(
            chronicle.GetProperty("Address"),
            $"{name} Address",
            "Stratum",
            "X",
            "Y");

        if (chronicle.TryGetProperty("LooseStoneAddress", out var looseStone) &&
            looseStone.ValueKind != JsonValueKind.Null)
        {
            RequireObjectWithProperties(
                looseStone,
                $"{name} loose-Stone Address",
                "Stratum",
                "X",
                "Y");
        }
    }

    private static void RequireObjectWithProperties(
        JsonElement element,
        string name,
        params string[] properties)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"{name} must be a JSON object.");
        }

        foreach (var property in properties)
        {
            if (!element.TryGetProperty(property, out _))
            {
                throw new InvalidOperationException(
                    $"{name} was missing required field '{property}'.");
            }
        }
    }

    private static void RequireExactObjectWithProperties(
        JsonElement element,
        string name,
        params string[] properties)
    {
        RequireObjectWithProperties(element, name, properties);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!properties.Contains(property.Name, StringComparer.Ordinal) ||
                !seen.Add(property.Name))
            {
                throw new InvalidOperationException(
                    $"{name} contained unexpected field '{property.Name}'.");
            }
        }
    }

    private static void RequireArray(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"{name} must be a JSON array.");
        }
    }

    private static void RequireNullableNumber(JsonElement element, string name)
    {
        if (element.ValueKind is not (JsonValueKind.Null or JsonValueKind.Number))
        {
            throw new InvalidOperationException($"{name} must be a number or null.");
        }
    }

    private static void ValidateCodexWords(JsonElement words)
    {
        RequireArray(words, "Codex Words");
        var seen = new HashSet<WordId>();
        var canonicalWords = WordCatalogue.Words.Select(word => word.Id).ToArray();
        var previousCatalogueIndex = -1;
        foreach (var element in words.EnumerateArray())
        {
            var word = ReadKnownWordId(element, "Codex Word");
            if (!seen.Add(word))
            {
                throw new InvalidOperationException(
                    $"Codex Words contained duplicate identity '{word}'.");
            }

            var catalogueIndex = Array.IndexOf(canonicalWords, word);
            if (catalogueIndex <= previousCatalogueIndex)
            {
                throw new InvalidOperationException(
                    "Codex Words must follow Word Catalogue canonical order.");
            }

            previousCatalogueIndex = catalogueIndex;
        }
    }

    private static WordId ReadKnownWordId(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(element.GetString()))
        {
            throw new InvalidOperationException($"{name} must be a non-empty Word identity string.");
        }

        var word = new WordId(element.GetString()!);
        if (!WordCatalogue.TryGet(word, out _))
        {
            throw new InvalidOperationException($"{name} used unknown identity '{word}'.");
        }

        return word;
    }

    private static void ValidateUnderstandingEntries(JsonElement understanding)
    {
        RequireArray(understanding, "Study Understanding");
        var seen = new HashSet<WordId>();
        var canonicalWords = WordCatalogue.Words.Select(word => word.Id).ToArray();
        var previousCatalogueIndex = -1;
        foreach (var entry in understanding.EnumerateArray())
        {
            RequireExactObjectWithProperties(
                entry,
                "Study Understanding entry",
                "Word",
                "Amount");
            var word = ReadKnownWordId(
                entry.GetProperty("Word"),
                "Study Understanding Word");
            if (!seen.Add(word))
            {
                throw new InvalidOperationException(
                    $"Study Understanding contained duplicate identity '{word}'.");
            }

            var catalogueIndex = Array.IndexOf(canonicalWords, word);
            if (catalogueIndex <= previousCatalogueIndex)
            {
                throw new InvalidOperationException(
                    "Study Understanding must follow Word Catalogue canonical order.");
            }

            previousCatalogueIndex = catalogueIndex;

            var amountElement = entry.GetProperty("Amount");
            var definition = WordCatalogue.Get(word);
            if (!amountElement.TryGetInt32(out var amount) ||
                amount <= 0 ||
                amount > definition.UnderstandingRequired)
            {
                throw new InvalidOperationException(
                    $"{definition.DisplayName} Understanding must be positive and no greater than " +
                    $"its authored threshold of {definition.UnderstandingRequired} in a current save.");
            }
        }
    }
}
