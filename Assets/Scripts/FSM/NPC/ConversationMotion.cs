using UnityEngine;

/// <summary>
/// Shared procedural talk/listen choreography for conversation participants
/// (NPCs through NpcAnimationDriver, Elijah through ConversationActorVisual).
/// Pure math: callers own events, smoothing and how the pose is applied.
/// Poses are Euler degrees in the character's frame: x = pitch (down),
/// y = yaw, z = roll. Variation is deterministic per line (index parity and
/// the line's punctuation), never random per frame.
/// </summary>
public static class ConversationMotion
{
    public enum LineMood { Statement, Question, Exclaim, Hesitant }

    private static readonly string[] HesitantOpeners = { "um ", "um,", "uh ", "uh,", "hmm", "well," };

    public static LineMood MoodOf(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return LineMood.Statement;
        string trimmed = text.Trim();
        if (trimmed.EndsWith("...") || trimmed.EndsWith("…")) return LineMood.Hesitant;
        if (trimmed.EndsWith("?")) return LineMood.Question;
        if (trimmed.EndsWith("!")) return LineMood.Exclaim;
        string start = trimmed.ToLowerInvariant();
        foreach (string filler in HesitantOpeners)
            if (start.StartsWith(filler)) return LineMood.Hesitant;
        return LineMood.Statement;
    }

    /// <summary>One smooth up-and-back pulse over the duration.</summary>
    public static float Beat(float time, float duration) =>
        time <= 0f || time >= duration ? 0f : Mathf.Sin(time / duration * Mathf.PI);

    public static float Ease(float time, float duration) =>
        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));

    /// <summary>
    /// Adds the speaking or listening motion for the current line.
    /// <paramref name="sinceReveal"/> is negative while the text is still typing.
    /// </summary>
    public static void Apply(bool speaking, float t, float sinceLine, float sinceReveal, LineMood mood,
        int lineIndex, float seed, float speechDegrees, float strength, ref Vector3 head, ref Vector3 body)
    {
        float s = strength;
        float side = (lineIndex & 1) == 0 ? 1f : -1f;
        float lineSeed = seed + lineIndex * 7.31f;

        if (speaking)
        {
            // Speech cadence while the text types, settling once it is shown.
            float talk = sinceReveal < 0f ? 1f : Mathf.Clamp01(1f - sinceReveal * 2f);
            head.x += (Mathf.PerlinNoise(lineSeed, t * 2.6f) - 0.5f) * 2f * speechDegrees * 1.3f * talk * s;
            head.y += (Mathf.PerlinNoise(lineSeed + 3f, t * 1.3f) - 0.5f) * 6f * talk * s;
            body.x += 1.5f * s;
            switch (mood)
            {
                case LineMood.Question:
                    head.z += 7f * side * Ease(sinceLine, 0.4f) * s;
                    head.x -= 3f * Ease(sinceLine, 0.4f) * s;
                    break;
                case LineMood.Exclaim:
                    head.x += 9f * Beat(sinceLine, 0.5f) * s;
                    body.x += 2.5f * Beat(sinceLine, 0.6f) * s;
                    break;
                case LineMood.Hesitant:
                    head.y += 12f * side * Beat(sinceLine, 1.4f) * s;
                    head.x += 4f * Beat(sinceLine, 1.4f) * s;
                    break;
                default:
                    head.x += 5f * Beat(sinceLine, 0.55f) * s;
                    break;
            }
            return;
        }

        head.z += 4f * side * Ease(sinceLine, 0.6f) * s;
        body.x -= 0.8f * s;
        // Acknowledging nods once the other line is shown, repeating on long reads.
        float nodTime = sinceReveal >= 0f ? sinceReveal : sinceLine - 1.2f;
        if (nodTime > 0f)
            head.x += 5f * Beat(Mathf.Repeat(nodTime, 2.6f), 0.7f) * s;
        if (mood == LineMood.Question)
        {
            // Considering a question: a brief look aside.
            head.y += 10f * side * Beat(sinceLine, 1.6f) * s;
            head.x -= 4f * Beat(sinceLine, 1.6f) * s;
        }
    }

    /// <summary>
    /// Rotates a bone by a character-frame Euler offset. Call after the pose
    /// for this frame was written (Animator or sampled clip).
    /// </summary>
    public static void RotateBone(Transform bone, Quaternion character, Vector3 euler)
    {
        if (bone == null) return;
        bone.rotation = character * Quaternion.Euler(euler) * Quaternion.Inverse(character) * bone.rotation;
    }
}
