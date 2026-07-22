namespace Chronicle.Core;

/// <summary>
/// Composes the authored effect of an Expression from catalogue data alone.
/// Adding a Modifier is an authoring change: no rule in this Module names any
/// individual Word.
/// </summary>
public static class WordEffects
{
    public static WordEffect BaseFor(WordId verb) => WordCatalogue.Get(verb).AuthoredEffect;

    public static WordEffect Compose(
        WordDefinition verb,
        IEnumerable<WordDefinition> modifiers)
    {
        ArgumentNullException.ThrowIfNull(verb);
        ArgumentNullException.ThrowIfNull(modifiers);

        var composed = verb.AuthoredEffect;
        foreach (var modifier in modifiers)
        {
            var delta = modifier.AuthoredEffect;
            composed = new WordEffect(
                composed.Preparation + delta.Preparation,
                composed.Consequence + delta.Consequence,
                composed.Recovery + delta.Recovery,
                composed.Damage + delta.Damage);
        }

        return new WordEffect(
            Math.Max(0, composed.Preparation),
            Math.Max(0, composed.Consequence),
            Math.Max(0, composed.Recovery),
            Math.Max(0, composed.Damage));
    }

    internal static WordEffect Compose(LoadoutSlot expression) =>
        expression.Verb is { } verb
            ? Compose(
                WordCatalogue.Get(verb),
                expression.Modifiers.Select(WordCatalogue.Get))
            : WordEffect.None;
}
