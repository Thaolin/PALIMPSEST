using System.Text.Json.Serialization;

namespace Chronicle.Core;

public enum ChronicleVerb
{
    Fly = 1,
}

public enum ChronicleNoun
{
    Stone = 1,
}

public readonly record struct LoadoutSlot(
    ChronicleVerb? Verb = null,
    ChronicleNoun? Noun = null)
{
    [JsonIgnore]
    public bool IsEmpty => Verb is null;

    [JsonIgnore]
    public bool IsIntrinsicFly => Verb == ChronicleVerb.Fly && Noun is null;

    [JsonIgnore]
    public bool IsFlyStone => Verb == ChronicleVerb.Fly && Noun == ChronicleNoun.Stone;

    [JsonIgnore]
    public string DisplayName => this switch
    {
        { IsIntrinsicFly: true } => "FLY",
        { IsFlyStone: true } => "FLY[STONE]",
        _ when IsEmpty => "—",
        _ => "?",
    };
}

public readonly record struct LoadoutState(
    LoadoutSlot Slot1 = default,
    LoadoutSlot Slot2 = default,
    LoadoutSlot Slot3 = default,
    LoadoutSlot Slot4 = default,
    LoadoutSlot Slot5 = default,
    LoadoutSlot Slot6 = default,
    LoadoutSlot Slot7 = default,
    LoadoutSlot Slot8 = default)
{
    public const int SlotCount = 8;

    public static LoadoutState Empty => new();

    [JsonIgnore]
    public IReadOnlyList<LoadoutSlot> Slots =>
    [
        Slot1,
        Slot2,
        Slot3,
        Slot4,
        Slot5,
        Slot6,
        Slot7,
        Slot8,
    ];

    public LoadoutSlot this[int index] => index switch
    {
        0 => Slot1,
        1 => Slot2,
        2 => Slot3,
        3 => Slot4,
        4 => Slot5,
        5 => Slot6,
        6 => Slot7,
        7 => Slot8,
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Loadout slots are numbered 0 through 7."),
    };

    internal static LoadoutState InitialFor(CodexState codex) => codex.HasFly
        ? Empty.WithSlot(0, new LoadoutSlot(ChronicleVerb.Fly))
        : Empty;

    internal LoadoutState WithSlot(int index, LoadoutSlot slot) => index switch
    {
        0 => this with { Slot1 = slot },
        1 => this with { Slot2 = slot },
        2 => this with { Slot3 = slot },
        3 => this with { Slot4 = slot },
        4 => this with { Slot5 = slot },
        5 => this with { Slot6 = slot },
        6 => this with { Slot7 = slot },
        7 => this with { Slot8 = slot },
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Loadout slots are numbered 0 through 7."),
    };

    internal void Validate(CodexState codex)
    {
        foreach (var slot in Slots)
        {
            if (slot.Verb is null && slot.Noun is not null)
            {
                throw new InvalidOperationException("A Loadout Noun requires a Verb.");
            }

            if (slot.Verb is { } verb && verb != ChronicleVerb.Fly)
            {
                throw new InvalidOperationException($"Unknown Loadout Verb value '{verb}'.");
            }

            if (slot.Noun is { } noun && noun != ChronicleNoun.Stone)
            {
                throw new InvalidOperationException($"Unknown Loadout Noun value '{noun}'.");
            }

            if (slot.Verb == ChronicleVerb.Fly && !codex.HasFly)
            {
                throw new InvalidOperationException("A Loadout cannot contain Fly before it is in the Codex.");
            }

            if (slot.Noun == ChronicleNoun.Stone && !codex.HasStone)
            {
                throw new InvalidOperationException("A Loadout cannot contain Stone before it is in the Codex.");
            }
        }

        if (Slots.Count(slot => slot.Verb == ChronicleVerb.Fly) > 1)
        {
            throw new InvalidOperationException("Fly cannot occupy more than one Loadout slot.");
        }
    }
}
