using UnityEngine;

/// <summary>
/// A scene-owned sound on the presentation clock: a looping room tone with
/// eased volume, or a one-shot cue (paper placement) at an authored time.
/// Volume follows the player's AudioScale (the existing Effects setting, and
/// the closing fade of a Skip). Pauses with the presentation; stops on release.
/// A one-shot is never fired late after a resume or a hitch.
/// </summary>
public sealed class ChapterCinematicSound : ChapterCinematicElement
{
    private const float LateTolerance = 0.25f;

    [SerializeField] private AudioSource source;
    [Tooltip("Existing clip assigned to the source when the presentation starts.")]
    [SerializeField] private AudioClip clip;
    [SerializeField] private float start;
    [SerializeField] private float end = 15f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.3f;
    [SerializeField, Min(0f)] private float fadeIn = 0.5f;
    [SerializeField, Min(0f)] private float fadeOut = 0.5f;
    [SerializeField] private bool loop;

    private bool fired;
    private bool paused;

    public float Start => start;
    public float End => end;

    public override void Prepare(ChapterCinematicPlayer player)
    {
        if (source == null) return;
        if (clip != null) source.clip = clip;
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.Stop();
    }

    public override void Evaluate(in ChapterCinematicFrame frame)
    {
        if (source == null || source.clip == null || paused) return;
        float t = frame.Time;
        bool inside = t >= start && t < end;

        if (inside && !fired)
        {
            fired = true;
            if (loop || t - start <= LateTolerance)
            {
                if (loop) source.time = Mathf.Repeat(t - start, Mathf.Max(0.01f, source.clip.length));
                source.Play();
            }
        }
        if (!inside && source.isPlaying && t >= end) source.Stop();

        float envelope = 1f;
        if (fadeIn > 0f) envelope = Mathf.Min(envelope, Progress(t, start, start + fadeIn));
        if (fadeOut > 0f) envelope = Mathf.Min(envelope, 1f - Progress(t, end - fadeOut, end));
        source.volume = volume * envelope * frame.AudioScale;
    }

    public override void SetPaused(bool value)
    {
        paused = value;
        if (source == null) return;
        if (value) source.Pause();
        else source.UnPause();
    }

    public override void Release()
    {
        if (source != null) source.Stop();
    }
}
