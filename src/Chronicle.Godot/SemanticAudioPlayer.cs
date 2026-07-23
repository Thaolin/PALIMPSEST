using Chronicle.Visuals;
using Godot;

/// <summary>
/// Tiny bounded cue renderer for Goal 7C. Cue identity is semantic and
/// deterministic; disabling sound suppresses playback only.
/// </summary>
public sealed partial class SemanticAudioPlayer : Node
{
    private const int MixRate = 22_050;
    private readonly AudioStreamPlayer _player = new();

    public bool Enabled { get; set; } = true;
    public IReadOnlyList<SemanticCueFamily> LastCuePlan { get; private set; } = [];

    public override void _Ready()
    {
        AddChild(_player);
    }

    public void Play(ExperienceFeedbackPlan? plan)
    {
        LastCuePlan = plan?.Cues.ToArray() ?? [];
        if (!Enabled || plan is null || plan.Cues.Count == 0)
        {
            return;
        }

        _player.Stream = Render(plan.Cues[0], plan.Band);
        _player.Play();
    }

    private static AudioStreamWav Render(
        SemanticCueFamily cue,
        FeedbackTimingBand band)
    {
        var samples = band switch
        {
            FeedbackTimingBand.Routine => 1_760,
            FeedbackTimingBand.DecisionConsequence => 2_650,
            _ => 3_520,
        };
        var frequency = 150 + ((int)cue * 37 % 620);
        var bytes = new byte[samples * 2];
        for (var index = 0; index < samples; index++)
        {
            var envelope = 1.0 - index / (double)samples;
            var overtone = 0.35 * Math.Sin(Math.Tau * frequency * 2 * index / MixRate);
            var wave = Math.Sin(Math.Tau * frequency * index / MixRate) + overtone;
            var value = (short)(wave * envelope * 3_800);
            bytes[index * 2] = (byte)(value & 0xff);
            bytes[index * 2 + 1] = (byte)((value >> 8) & 0xff);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = MixRate,
            Stereo = false,
            Data = bytes,
        };
    }
}
