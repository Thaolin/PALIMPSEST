using Chronicle.Core;
using Godot;

/// <summary>
/// Session-local editor for one proposed Expression. It renders catalogue and
/// preview facts, but only AttuneRequested may ask the simulation to mutate.
/// </summary>
public sealed partial class CodexLoadoutPanel : Control
{
    private static readonly Color Ink = new(0.025f, 0.022f, 0.026f, 0.99f);
    private static readonly Color Page = new(0.085f, 0.068f, 0.058f, 0.99f);
    private static readonly Color Raised = new(0.13f, 0.095f, 0.072f, 1f);
    private static readonly Color Bone = new(0.92f, 0.87f, 0.76f);
    private static readonly Color Muted = new(0.63f, 0.59f, 0.53f);
    private static readonly Color Ember = new(0.96f, 0.39f, 0.22f);
    private static readonly Color Verdigris = new(0.38f, 0.80f, 0.68f);
    private static readonly Color Ochre = new(0.91f, 0.72f, 0.30f);

    private readonly Label _active = new();
    private readonly Label _proposal = new();
    private readonly Label _preview = new();
    private readonly Label _availability = new();
    private readonly Label _capacity = new();
    private readonly VBoxContainer _verbs = new();
    private readonly VBoxContainer _modifiers = new();
    private readonly Button _attune = new();
    private readonly List<Button> _wordButtons = [];
    private readonly Dictionary<Button, WordDefinition> _wordDefinitions = [];
    private Func<WordId?, IReadOnlyList<WordId>, AttunementPreviewSnapshot>? _previewProvider;
    private ChronicleState? _state;
    private WordId? _proposedVerb;
    private readonly List<WordId> _proposedModifiers = [];

    public event Action<AttuneExpression>? AttuneRequested;
    public event Action<bool>? CloseRequested;

    public WordId? ProposedVerb => _proposedVerb;
    public IReadOnlyList<WordId> ProposedModifiers => _proposedModifiers;
    public Button AttuneButton => _attune;
    public Label AvailabilityReadout => _availability;
    public Label PreviewReadout => _preview;
    public IReadOnlyList<Button> WordButtons => _wordButtons;
    public Button ButtonFor(WordId word) =>
        _wordDefinitions.Single(pair => pair.Value.Id == word).Key;

    public override void _Ready()
    {
        Position = Vector2.Zero;
        Size = new Vector2(ChronicleHud.CanvasWidth, ChronicleHud.CanvasHeight);
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 200;
        Visible = false;

        AddChild(new ColorRect
        {
            Position = Vector2.Zero,
            Size = Size,
            Color = Ink,
            MouseFilter = MouseFilterEnum.Stop,
        });
        AddPanel(new Vector2(28, 24), new Vector2(1544, 852), Page);
        AddLabel(
            "THE CODEX",
            new Vector2(58, 45),
            new Vector2(500, 50),
            32,
            Bone);
        AddLabel(
            "Known Words become power only when you deliberately Attune them.",
            new Vector2(58, 94),
            new Vector2(780, 32),
            15,
            Muted);
        AddLabel(
            "PAUSED · C / ESC CLOSES · SPACE CLOSES + RESUMES",
            new Vector2(920, 54),
            new Vector2(610, 34),
            14,
            Ochre,
            HorizontalAlignment.Right);

        AddPanel(new Vector2(50, 142), new Vector2(440, 670), new Color(0.055f, 0.045f, 0.042f, 1f));
        AddLabel("KNOWN WORDS", new Vector2(72, 162), new Vector2(390, 28), 15, Ochre);
        AddLabel("VERBS", new Vector2(72, 204), new Vector2(180, 24), 12, Muted);
        _verbs.Position = new Vector2(72, 232);
        _verbs.Size = new Vector2(396, 292);
        _verbs.AddThemeConstantOverride("separation", 8);
        AddChild(_verbs);
        AddLabel("MODIFIERS", new Vector2(72, 542), new Vector2(180, 24), 12, Muted);
        _modifiers.Position = new Vector2(72, 570);
        _modifiers.Size = new Vector2(396, 212);
        _modifiers.AddThemeConstantOverride("separation", 8);
        AddChild(_modifiers);

        AddPanel(new Vector2(510, 142), new Vector2(500, 670), new Color(0.065f, 0.050f, 0.044f, 1f));
        AddLabel("ONE ACTIVE EXPRESSION", new Vector2(536, 162), new Vector2(440, 28), 15, Ochre);
        ConfigureLabel(_active, new Vector2(536, 204), new Vector2(448, 86), 18, Bone);
        _active.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddLabel("PROPOSED — NOT YET ATTUNED", new Vector2(536, 316), new Vector2(440, 26), 12, Muted);
        ConfigureLabel(_proposal, new Vector2(536, 350), new Vector2(448, 84), 22, Bone);
        _proposal.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        ConfigureLabel(_capacity, new Vector2(536, 448), new Vector2(448, 56), 17, Verdigris);
        _capacity.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        ConfigureLabel(_availability, new Vector2(536, 520), new Vector2(448, 132), 15, Bone);
        _availability.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        _attune.Position = new Vector2(536, 700);
        _attune.Size = new Vector2(448, 72);
        _attune.Text = "ATTUNE THIS EXPRESSION";
        _attune.FocusMode = FocusModeEnum.All;
        _attune.AddThemeFontSizeOverride("font_size", 18);
        _attune.Pressed += CommitProposal;
        AddChild(_attune);

        AddPanel(new Vector2(1030, 142), new Vector2(520, 670), new Color(0.045f, 0.040f, 0.042f, 1f));
        AddLabel("WHAT THIS POWER DOES", new Vector2(1056, 162), new Vector2(460, 28), 15, Ochre);
        ConfigureLabel(_preview, new Vector2(1056, 204), new Vector2(464, 548), 16, Bone);
        _preview.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddLabel(
            "Selecting Words changes only this proposal. Attune is the commitment.",
            new Vector2(1056, 750),
            new Vector2(464, 44),
            13,
            Muted);
    }

    public void Open(
        ChronicleState state,
        Func<WordId?, IReadOnlyList<WordId>, AttunementPreviewSnapshot> previewProvider)
    {
        _state = state;
        _previewProvider = previewProvider;
        _proposedVerb = state.ActiveLoadout[0].Verb;
        _proposedModifiers.Clear();
        _proposedModifiers.AddRange(state.ActiveLoadout[0].Modifiers);
        RebuildWords();
        RefreshProposal();
        Visible = true;
        (_wordButtons.FirstOrDefault() ?? _attune).GrabFocus();
    }

    public void Close(bool resume)
    {
        Visible = false;
        _state = null;
        _previewProvider = null;
        CloseRequested?.Invoke(resume);
    }

    public void RefreshState(ChronicleState state)
    {
        if (!Visible || _previewProvider is null)
        {
            return;
        }

        _state = state;
        RefreshProposal();
    }

    private void RebuildWords()
    {
        foreach (var child in _verbs.GetChildren())
        {
            child.QueueFree();
        }
        foreach (var child in _modifiers.GetChildren())
        {
            child.QueueFree();
        }
        _wordButtons.Clear();
        _wordDefinitions.Clear();

        var known = WordCatalogue.Words
            .Where(word => _state!.Codex.Contains(word.Id))
            .Where(word => word.Kind is WordKind.Verb or WordKind.Modifier)
            .ToArray();
        foreach (var word in known)
        {
            var button = new Button
            {
                Name = $"CodexWord_{word.DisplayName}",
                Text = WordLabel(word),
                CustomMinimumSize = new Vector2(396, 58),
                FocusMode = FocusModeEnum.All,
                Alignment = HorizontalAlignment.Left,
                TooltipText = word.Meaning,
                ClipText = true,
            };
            button.AddThemeFontSizeOverride("font_size", 15);
            button.Pressed += () => Toggle(word);
            (word.Kind == WordKind.Verb ? _verbs : _modifiers).AddChild(button);
            _wordButtons.Add(button);
            _wordDefinitions[button] = word;
        }
    }

    private string WordLabel(WordDefinition word)
    {
        var active = _state!.ActiveLoadout[0];
        var marker = active.Verb == word.Id || active.Modifiers.Contains(word.Id)
            ? "◆ ATTUNED"
            : "KNOWN";
        return $"{word.DisplayName.ToUpperInvariant()}  ·  {marker}  ·  LOAD {word.Load}\n{FantasyLine(word)}";
    }

    private static string FantasyLine(WordDefinition word) =>
        word.Id == WordIds.Burn ? "Set a living target burning."
        : word.Id == WordIds.Quickly ? "Make preparation brief and exposed for less time."
        : word.Id == WordIds.Lasting ? "Make the consequence endure."
        : word.Id == WordIds.Suggest ? "Offer an intent without demanding obedience."
        : word.Id == WordIds.Command ? "Press authority; the listener still chooses."
        : word.Id == WordIds.Fly ? "Cross between sky and surface at the same place."
        : word.Id == WordIds.Found ? "Raise one place as Home."
        : word.Id == WordIds.Smash ? "Break resisting matter by direct force."
        : word.Meaning;

    private void Toggle(WordDefinition word)
    {
        if (word.Kind == WordKind.Verb)
        {
            _proposedVerb = _proposedVerb == word.Id ? null : word.Id;
            if (_proposedVerb is { } verb)
            {
                _proposedModifiers.RemoveAll(modifier =>
                    !WordCatalogue.Get(modifier).SupportedVerbs.Contains(verb));
            }
        }
        else if (_proposedModifiers.Remove(word.Id))
        {
            // Removed from the transient proposal.
        }
        else if (_proposedModifiers.Count < 2)
        {
            _proposedModifiers.Add(word.Id);
        }

        RefreshProposal();
    }

    private void RefreshProposal()
    {
        var active = _state!.ActiveLoadout[0];
        _active.Text = active.IsEmpty
            ? "Nothing Attuned.\nKnown Words cannot yet be invoked."
            : $"{ExpressionName(active)}\nThis remains active until another Attunement.";

        var preview = _previewProvider!(_proposedVerb, _proposedModifiers);
        _proposal.Text = _proposedVerb is null
            ? "Choose one Verb."
            : ExpressionName(preview.Expression);
        _capacity.Text =
            $"LOAD  {preview.UsedLoad} / {preview.LoadCapacity}     " +
            $"LINKS  {preview.UsedLinks} / {preview.LinkCapacity}\n" +
            $"Current: {(active.IsEmpty ? "none" : ExpressionName(active))}";
        _availability.Text = AvailabilityText(preview);
        _preview.Text = PreviewText(preview);
        _attune.Disabled = !preview.Available;
        _attune.Text = preview.Available
            ? "ATTUNE THIS EXPRESSION"
            : "ATTUNEMENT UNAVAILABLE";
        _attune.AddThemeColorOverride(
            "font_color",
            preview.Available ? Verdigris : Muted);

        foreach (var button in _wordButtons)
        {
            var word = _wordDefinitions[button];
            var proposed = _proposedVerb == word.Id || _proposedModifiers.Contains(word.Id);
            button.Modulate = proposed ? Colors.White : new Color(0.78f, 0.76f, 0.72f);
            button.AddThemeColorOverride("font_color", proposed ? Verdigris : Bone);
        }
    }

    private static string PreviewText(AttunementPreviewSnapshot preview)
    {
        if (preview.Expression.Verb is not { } verbId)
        {
            return "Choose a Verb to see its consequence, timing, constraints, and cost.";
        }

        var verb = WordCatalogue.Get(verbId);
        var effect = preview.Effect;
        var lines = new List<string>
        {
            FantasyLine(verb),
            string.Empty,
        };
        if (verbId == WordIds.Burn)
        {
            lines.Add($"Prepare for {effect.Preparation} active Heartbeat{Plural(effect.Preparation)}.");
            lines.Add($"Then burn the chosen flammable target for {effect.Damage} now.");
            lines.Add($"The consequence persists for {effect.Consequence} Heartbeats.");
            lines.Add($"Recovery lasts {effect.Recovery}; safe waiting may be skipped.");
            lines.Add(string.Empty);
            lines.Add("Movement, a strike, lost range, death, or cancellation can break Preparation.");
            lines.Add("While preparing, you cannot move, strike, or begin another Invocation.");
        }
        else if (verbId is var social && (social == WordIds.Suggest || social == WordIds.Command))
        {
            lines.Add("The intent reaches one known person and one concrete objective.");
            lines.Add("Their identity, relationship, and circumstances decide the answer.");
            lines.Add("Attunement makes the request expressible; it never guarantees obedience.");
        }
        else
        {
            lines.Add(verb.Meaning);
        }

        return string.Join("\n", lines);
    }

    private static string AvailabilityText(AttunementPreviewSnapshot preview) =>
        preview.Availability switch
        {
            AttunementAvailabilityReason.Available =>
                "Ready. Attuning replaces the current Loadout immediately; time does not advance.",
            AttunementAvailabilityReason.NoVerb =>
                "Choose a Verb first.",
            AttunementAvailabilityReason.ImmediateDanger =>
                "Leave immediate danger before Attuning.",
            AttunementAvailabilityReason.CarryingLode =>
                "Set down the Resonant Lode; carrying occupies focused Attunement.",
            AttunementAvailabilityReason.PhysicalCommitment =>
                "Finish or cancel the physical work first.",
            AttunementAvailabilityReason.VerbNotLearned or
            AttunementAvailabilityReason.ModifierNotLearned =>
                "This Word must be understood before it can be Attuned.",
            AttunementAvailabilityReason.IncompatibleModifier =>
                "That Modifier cannot shape this Verb. Remove it or choose Burn.",
            AttunementAvailabilityReason.LinkCapacityExceeded =>
                $"Remove a Modifier: this body supports {preview.LinkCapacity} linked Words.",
            AttunementAvailabilityReason.LoadCapacityExceeded =>
                $"Needs {preview.UsedLoad} Load; the next Attunement has {preview.LoadCapacity}. " +
                SourceRoute(preview.SourcePhase),
            AttunementAvailabilityReason.LinkAndLoadExceeded =>
                $"Too many links and too much Load. Remove a Modifier; capacity is {preview.LoadCapacity}.",
            _ => "This Chronicle cannot Attune that proposal.",
        };

    private static string SourceRoute(HearthResonatorPhase? phase) =>
        phase switch
        {
            HearthResonatorPhase.Destroyed => "Rebuild the Hearth Resonator to restore 12.",
            HearthResonatorPhase.Rebuilding => "The Hearth Resonator contributes when rebuilding finishes.",
            HearthResonatorPhase.UnderConstruction => "Finish the Hearth Resonator to raise it to 12.",
            _ => "An intact Hearth Resonator at Home raises the next capacity to 12.",
        };

    private void CommitProposal()
    {
        if (_proposedVerb is not { } verb)
        {
            return;
        }

        AttuneRequested?.Invoke(new AttuneExpression(verb, _proposedModifiers.ToArray()));
    }

    private static string ExpressionName(LoadoutSlot slot)
    {
        if (slot.Verb is not { } verb)
        {
            return "No Expression";
        }

        var words = new List<string> { WordCatalogue.Get(verb).DisplayName };
        words.AddRange(slot.Modifiers.Select(modifier => WordCatalogue.Get(modifier).DisplayName));
        return string.Join(" + ", words);
    }

    private static string Plural(int value) => value == 1 ? string.Empty : "s";

    private void AddPanel(Vector2 position, Vector2 size, Color color)
    {
        AddChild(new ColorRect
        {
            Position = position,
            Size = size,
            Color = color,
            MouseFilter = MouseFilterEnum.Stop,
        });
    }

    private Label AddLabel(
        string text,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color,
        HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var label = new Label { Text = text, HorizontalAlignment = alignment };
        ConfigureLabel(label, position, size, fontSize, color);
        return label;
    }

    private void ConfigureLabel(
        Label label,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color)
    {
        label.Position = position;
        label.Size = size;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(label);
    }
}
