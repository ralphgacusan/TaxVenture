using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// T03 contract tests for the production pure-logic pieces DialogueUI and
/// DialogueCameraDirector are built on: terminal handoff ordering, the single
/// gesture gate, typewriter timing and shot/reverse-shot geometry.
/// </summary>
public static class PresentationTests
{
    public static void Run(Action<bool, string> check)
    {
        Handoff(check);
        HandoffWithSession(check);
        Gate(check);
        GateConversationWalkthrough(check);
        Reveal(check);
        Shots(check);
        Placement(check);
        CloseUpPolicy(check);
        CameraHintSurvivesSnapshot(check);
    }

    // ------------------------------------------------------------------
    private static void Handoff(Action<bool, string> check)
    {
        var handoff = new DialogueTerminalHandoff();
        int runs = 0;
        check(!handoff.IsPending, "A new handoff must not be pending.");
        check(!handoff.Complete(1), "Completing with nothing pending must be rejected.");
        check(!handoff.Begin(1, null), "A handoff needs a completion action.");
        check(handoff.Begin(7, () => runs++) && handoff.IsPending && handoff.PendingVersion == 7, "Begin must arm the handoff for its version.");
        check(!handoff.Begin(8, () => runs += 100), "A second handoff must be rejected while one is pending.");
        check(!handoff.Complete(6) && runs == 0 && handoff.IsPending, "A stale version must not complete the pending handoff.");
        check(handoff.Complete(7) && runs == 1 && !handoff.IsPending, "The matching version must complete exactly once.");
        check(!handoff.Complete(7) && runs == 1, "A repeated completion must be ignored.");
        check(!handoff.ForceComplete() && runs == 1, "Forcing with nothing pending must do nothing.");

        // The action may start the next conversation's handoff (state cleared first).
        bool rearmed = false;
        check(handoff.Begin(9, () => rearmed = handoff.Begin(10, () => runs++)), "Re-entrancy case must arm.");
        check(handoff.Complete(9) && rearmed && handoff.IsPending && handoff.PendingVersion == 10,
            "A completion action must be able to arm the next handoff.");
        check(handoff.ForceComplete() && runs == 2 && !handoff.IsPending, "Teardown must force the pending handoff once.");
        check(!handoff.ForceComplete() && runs == 2, "Forcing twice must not rerun the action.");
    }

    // Mirrors DialogueUI.Finish: claim → hide → presentation return → release → callback.
    private static void HandoffWithSession(Action<bool, string> check)
    {
        foreach (DialogueEndReason reason in new[] { DialogueEndReason.Completed, DialogueEndReason.Cancelled })
        {
            var log = new List<string>();
            var session = new DialogueSession();
            var handoff = new DialogueTerminalHandoff();
            Action onReturned = null;

            check(session.TryStart(new[] { new DialogueLine(DialogueSpeaker.Npc, "Only line") },
                () => log.Add("completed"), () => log.Add("cancelled")), $"{reason}: session must start.");
            check(session.TryEnd(reason, out Action callback), $"{reason}: terminal claim must succeed.");
            log.Add("hide");
            const int version = 3;
            check(handoff.Begin(version, () => { log.Add("release"); callback?.Invoke(); log.Add("ended"); }),
                $"{reason}: handoff must arm.");
            onReturned = () => handoff.Complete(version);
            log.Add("return-started");

            check(!session.IsActive && handoff.IsPending, $"{reason}: during the return the session is claimed but still pending.");
            check(!log.Contains("release") && !log.Contains("completed") && !log.Contains("cancelled"),
                $"{reason}: controls and outcome callbacks must wait for the return blend.");
            // DialogueUI.IsDialogueActive = session.IsActive || handoff.IsPending.
            bool countsAsActive = session.IsActive || handoff.IsPending;
            check(countsAsActive, $"{reason}: the conversation must still count as active while returning.");

            onReturned();
            onReturned(); // a late duplicate from the presentation must be harmless
            string expectedOutcome = reason == DialogueEndReason.Completed ? "completed" : "cancelled";
            string other = reason == DialogueEndReason.Completed ? "cancelled" : "completed";
            check(string.Join(",", log) == $"hide,return-started,release,{expectedOutcome},ended",
                $"{reason}: order must be hide → return → release → {expectedOutcome} → ended, once (got {string.Join(",", log)}).");
            check(!log.Contains(other), $"{reason}: the other terminal callback must never run.");
        }
    }

    // ------------------------------------------------------------------
    private static void Gate(Action<bool, string> check)
    {
        var gate = new DialogueAdvanceGate { OpeningGrace = 0.15f, MinimumInterval = 0.12f };
        check(gate.Evaluate(1, 1f, false) == DialogueGesture.Ignored, "A closed gate must ignore gestures.");

        gate.Open(100, 10f);
        check(gate.IsOpen, "Open must open the gate.");
        check(gate.Evaluate(100, 10.5f, true) == DialogueGesture.Ignored, "The opening frame must be ignored even after the grace time.");
        check(gate.Evaluate(101, 10.1f, true) == DialogueGesture.Ignored, "Gestures inside the opening grace must be ignored.");
        check(gate.Evaluate(110, 10.2f, true) == DialogueGesture.RevealLine, "A gesture while typing must reveal the line only.");
        check(gate.Evaluate(110, 10.6f, false) == DialogueGesture.Ignored, "A second event in the same frame must be ignored.");
        check(gate.Evaluate(111, 10.25f, false) == DialogueGesture.Ignored, "A near-simultaneous duplicate (tap + button) must be ignored.");
        check(gate.Evaluate(120, 10.40f, false) == DialogueGesture.Advance, "A distinct later gesture on a revealed line must advance.");
        check(gate.Evaluate(121, 10.45f, false) == DialogueGesture.Ignored, "A repeated submit inside the interval must not advance again.");
        check(gate.Evaluate(130, 10.60f, true) == DialogueGesture.RevealLine, "The next line's first gesture must reveal it.");
        gate.Close();
        check(gate.Evaluate(200, 20f, false) == DialogueGesture.Ignored, "A closed gate must ignore gestures after the conversation ends.");

        gate.Open(300, 30f);
        check(gate.Evaluate(301, 30.2f, false) == DialogueGesture.Advance, "Re-opening must reset the last-gesture history.");
    }

    // Three typed lines, rapid tapping: every line is revealed before it can be
    // advanced, nothing is skipped, and the close needs its own gesture.
    private static void GateConversationWalkthrough(Action<bool, string> check)
    {
        var session = new DialogueSession();
        var gate = new DialogueAdvanceGate { OpeningGrace = 0.15f, MinimumInterval = 0.12f };
        var reveal = new DialogueRevealSchedule();
        string[] texts = { "First line of the talk.", "Second, shorter.", "Last." };
        var lines = new List<DialogueLine>();
        foreach (string t in texts) lines.Add(new DialogueLine(DialogueSpeaker.Npc, t));
        int completed = 0;
        check(session.TryStart(lines, () => completed++), "Walkthrough must start.");

        int frame = 1000;
        float time = 50f;
        gate.Open(frame, time);
        reveal.Begin(texts[0], 40f, 0.22f, 0.08f);
        var seen = new List<string>();
        int reveals = 0, advances = 0;

        for (int tap = 0; tap < 40 && session.IsActive; tap++)
        {
            frame += 8;            // a tap every 8 frames…
            time += 0.13f;         // …about 0.13 s apart
            reveal.Tick(0.13f);
            switch (gate.Evaluate(frame, time, reveal.IsTyping))
            {
                case DialogueGesture.RevealLine:
                    reveals++;
                    check(reveal.IsTyping, "RevealLine must only be issued while typing.");
                    reveal.RevealAll();
                    break;
                case DialogueGesture.Advance:
                    advances++;
                    check(!reveal.IsTyping, "Advance must only be issued on a fully revealed line.");
                    seen.Add(session.CurrentLine.Text);
                    check(session.TryAdvance(out bool done), "Advance must be accepted by the session.");
                    if (done)
                    {
                        check(session.TryEnd(DialogueEndReason.Completed, out Action cb), "The last advance must allow completion.");
                        cb();
                    }
                    else reveal.Begin(session.CurrentLine.Text, 40f, 0.22f, 0.08f);
                    break;
            }
        }

        check(completed == 1, "Rapid tapping must complete the conversation exactly once.");
        check(string.Join("|", seen) == string.Join("|", texts), "Every line must be shown, in order, none skipped.");
        check(advances == texts.Length, "One advance per line, including the separate close.");
        check(reveals >= 1, "Tapping during typing must reveal rather than skip.");
    }

    // ------------------------------------------------------------------
    private static void Reveal(Action<bool, string> check)
    {
        var r = new DialogueRevealSchedule();
        r.Begin("", 40f, 0.2f, 0.1f);
        check(!r.IsTyping && r.Total == 0, "Empty text must not type.");

        r.Begin("Hello", 10f, 0f, 0f);
        check(r.Total == 5 && r.Visible == 0 && r.IsTyping, "A new line must start fully hidden.");
        check(Near(r.RevealTimeOf(0), 0.1f) && Near(r.RevealTimeOf(4), 0.5f), "Characters must appear at the configured rate.");
        check(r.Tick(0.05f) == 0 && r.Tick(0.06f) == 1, "Characters must appear exactly at their threshold.");
        check(r.Tick(10f) == 5 && !r.IsTyping, "A long tick must reveal everything.");
        check(r.Tick(1f) == 5, "Ticks after completion must not overflow.");

        r.Begin("Hi. There", 10f, 0.5f, 0.2f);
        check(Near(r.RevealTimeOf(3) - r.RevealTimeOf(2), 0.1f + 0.5f), "A sentence break must pause after the full stop.");
        r.Begin("3.5 kg", 10f, 0.5f, 0.2f);
        check(Near(r.RevealTimeOf(2) - r.RevealTimeOf(1), 0.1f), "A decimal point must not pause.");
        r.Begin("Wait... now", 10f, 0.5f, 0.2f);
        check(Near(r.RevealTimeOf(4) - r.RevealTimeOf(3), 0.1f) && Near(r.RevealTimeOf(5) - r.RevealTimeOf(4), 0.1f) &&
              Near(r.RevealTimeOf(7) - r.RevealTimeOf(6), 0.1f + 0.5f), "An ellipsis must pause once, after its last mark.");
        r.Begin("Yes, sir", 10f, 0.5f, 0.2f);
        check(Near(r.RevealTimeOf(4) - r.RevealTimeOf(3), 0.1f + 0.2f), "A comma must add the clause pause.");
        r.Begin("Done.", 10f, 0.5f, 0.2f);
        check(Near(r.RevealTimeOf(4), 0.5f), "Final punctuation must not delay the end of the line.");
        r.Begin("\"Go!\" she said", 10f, 0.5f, 0.2f);
        check(Near(r.RevealTimeOf(4) - r.RevealTimeOf(3), 0.1f + 0.5f), "A sentence mark before a closing quote must pause.");

        // Rich text: TMP passes only visible characters, so tags are never timed.
        string tagged = "<b>Bold</b> text";
        string visible = "Bold text";
        r.Begin(visible, 20f, 0f, 0f);
        check(r.Total == visible.Length && r.Total < tagged.Length, "Only visible characters are scheduled; tags are never typed.");

        r.Begin(new string('a', 300), 100f, 0f, 0f);
        bool monotonic = true;
        for (int i = 1; i < r.Total; i++) monotonic &= r.RevealTimeOf(i) > r.RevealTimeOf(i - 1);
        check(r.Total == 300 && monotonic, "Long lines must grow the buffer and keep strictly increasing times.");
        r.Tick(0.5f);
        r.RevealAll();
        check(r.Visible == r.Total && !r.IsTyping, "RevealAll must show the whole line at once.");
        r.Clear();
        check(r.Total == 0 && !r.IsTyping, "Clear must stop typing.");

        char[] buffer = "abc".ToCharArray();
        r.Begin(buffer, 99, 10f, 0f, 0f);
        check(r.Total == 3, "A count larger than the buffer must be clamped.");
        r.Begin((char[])null, 5, 10f, 0f, 0f);
        check(r.Total == 0, "A null buffer must not type.");
    }

    // ------------------------------------------------------------------
    private static void Shots(Action<bool, string> check)
    {
        DialogueShotSettings s = DialogueShotSettings.Default;
        check(Near(DialogueShotMath.YawDegrees(new Vector3(0, 0, 1)), 0f) &&
              Near(DialogueShotMath.YawDegrees(new Vector3(1, 0, 0)), 90f), "Yaw must follow Unity's +Z forward, +X right convention.");
        check(Near(DialogueShotMath.Ease(0f), 0f) && Near(DialogueShotMath.Ease(1f), 1f) &&
              Near(DialogueShotMath.Ease(0.5f), 0.5f) && DialogueShotMath.Ease(2f) == 1f && DialogueShotMath.Ease(-1f) == 0f,
            "Easing must start at 0, end at 1 and clamp.");

        float[] heights = { 1.7f, 3.5f, 0.9f };
        int configurations = 0;
        foreach (float h in heights)
            for (int angle = 0; angle < 360; angle += 45)
            {
                configurations++;
                double rad = angle * Math.PI / 180.0;
                Vector3 elijahFloor = new Vector3(2f, 0.5f, -1f);
                Vector3 dir = new Vector3((float)Math.Sin(rad), 0f, (float)Math.Cos(rad));
                Vector3 npcFloor = elijahFloor + dir * (0.7f * h);
                Vector3 elijahHead = elijahFloor + new Vector3(0, 0.9f * h, 0);
                Vector3 npcHead = npcFloor + new Vector3(0, 0.88f * h, 0);
                string where = $"(height {h}, angle {angle})";

                DialogueShotPose npcShot = DialogueShotMath.NpcLineShot(elijahHead, npcHead, h, s);
                DialogueShotPose playerShot = DialogueShotMath.PlayerLineShot(elijahHead, npcHead, h, s);
                DialogueShotPose two = DialogueShotMath.TwoShot(elijahHead, npcHead, h, s, +1f);

                check(Vector3.Dot(DialogueShotMath.Horizontal(npcShot.Position - elijahHead), dir) < 0f,
                    $"NPC-line camera must sit behind Elijah {where}.");
                check(Vector3.Dot(DialogueShotMath.Horizontal(playerShot.Position - npcHead), -dir) < 0f,
                    $"Player-line camera must sit behind the NPC {where}.");
                check(Near(npcShot.Position.Y - elijahHead.Y, s.HeightOffset * h) &&
                      Near(playerShot.Position.Y - npcHead.Y, s.HeightOffset * h),
                    $"Cameras must sit just above the foreground head {where}.");

                int npcSide = DialogueShotMath.SideOfAxis(elijahHead, npcHead, npcShot.Position);
                int playerSide = DialogueShotMath.SideOfAxis(elijahHead, npcHead, playerShot.Position);
                int twoSide = DialogueShotMath.SideOfAxis(elijahHead, npcHead, two.Position);
                check(npcSide == 1 && playerSide == 1 && twoSide == 1,
                    $"All shots must stay on the same side of the Elijah→NPC axis (180° rule) {where}.");

                // Screen consistency: Elijah stays screen-left, the NPC screen-right, in both shots.
                check(ScreenX(npcShot, npcHead) > ScreenX(npcShot, elijahHead) &&
                      ScreenX(playerShot, npcHead) > ScreenX(playerShot, elijahHead),
                    $"Elijah must stay screen-left of the NPC across shot and reverse shot {where}.");

                // The speaker is framed near the centre; the shoulder is in frame, off to the side.
                check(AngleDeg(npcShot.LookAt - npcShot.Position, npcHead - npcShot.Position) < 15f &&
                      AngleDeg(playerShot.LookAt - playerShot.Position, elijahHead - playerShot.Position) < 15f,
                    $"The speaking face must be close to the frame centre {where}.");
                check(AngleDeg(npcShot.LookAt - npcShot.Position, elijahHead - npcShot.Position) < 50f,
                    $"Elijah's shoulder must remain within a normal field of view in the NPC shot {where}.");

                float gap = DialogueShotMath.Horizontal(npcHead - elijahHead).Length();
                Vector3 mid = (elijahHead + npcHead) * 0.5f;
                check(Near(Vector3.Dot(DialogueShotMath.Horizontal(two.Position - mid), dir), 0f, 1e-3f) &&
                      DialogueShotMath.Horizontal(two.Position - mid).Length() >= gap * 1.1f - 1e-4f &&
                      Vector3.Distance(two.LookAt, mid) < 1e-4f,
                    $"The two-shot must look at the midpoint from the side, far enough to hold both {where}.");

                // Wider side angle: from the OTS camera the speaker's head is well clear
                // of the foreground listener's head (old defaults gave about 12.7°).
                check(AngleDeg(npcHead - npcShot.Position, elijahHead - npcShot.Position) >= 18f &&
                      AngleDeg(elijahHead - playerShot.Position, npcHead - playerShot.Position) >= 18f,
                    $"The listener must not hide the speaker: at least 18° between their heads {where}.");

                // Solo close-ups: only the speaker is in frame, same side of the axis.
                DialogueShotPose npcClose = DialogueShotMath.NpcCloseUp(elijahHead, npcHead, h, s);
                DialogueShotPose playerClose = DialogueShotMath.PlayerCloseUp(elijahHead, npcHead, h, s);
                check(DialogueShotMath.IsBehindCamera(npcClose, elijahHead, 0.08f * h) &&
                      DialogueShotMath.IsBehindCamera(playerClose, npcHead, 0.08f * h),
                    $"Close-ups must leave the listener behind the camera (out of frame) {where}.");
                check(!DialogueShotMath.IsBehindCamera(npcClose, npcHead) && !DialogueShotMath.IsBehindCamera(playerClose, elijahHead) &&
                      Vector3.Distance(npcClose.LookAt, npcHead) < 1e-4f && Vector3.Distance(playerClose.LookAt, elijahHead) < 1e-4f,
                    $"Close-ups must look straight at the speaking face {where}.");
                check(Near(Vector3.Distance(npcClose.Position, npcHead), (float)Math.Sqrt(0.3 * 0.3 + 0.1 * 0.1) * h, 1e-3f * h),
                    $"Close-up distance must follow the settings and scale with height {where}.");
                check(DialogueShotMath.SideOfAxis(elijahHead, npcHead, npcClose.Position) == 1 &&
                      DialogueShotMath.SideOfAxis(elijahHead, npcHead, playerClose.Position) == 1,
                    $"Close-ups must stay on the same side of the axis as the other shots {where}.");

                DialogueShotPose tight = DialogueShotMath.NpcLineShot(elijahHead, npcHead, h, s, 0.5f);
                check(Vector3.Distance(tight.Position, elijahHead) < Vector3.Distance(npcShot.Position, elijahHead),
                    $"A shortened pull-back must move the camera closer for tight spaces {where}.");
            }
        check(configurations == 24, "Shot geometry must be checked across heights and directions.");

        // Scale invariance: the same framing at double size.
        Vector3 e = new Vector3(0, 0.9f, 0), n = new Vector3(0, 0.88f, 0.7f);
        DialogueShotPose small = DialogueShotMath.NpcLineShot(e, n, 1f, s);
        DialogueShotPose big = DialogueShotMath.NpcLineShot(e * 2f, n * 2f, 2f, s);
        check(Vector3.Distance(big.Position, small.Position * 2f) < 1e-4f, "Framing must scale with character height.");

        // Degenerate: heads on the same vertical line must still produce a finite shot.
        DialogueShotPose degenerate = DialogueShotMath.NpcLineShot(new Vector3(1, 1, 1), new Vector3(1, 1.2f, 1), 1.7f, s);
        check(IsFinite(degenerate.Position) && IsFinite(degenerate.LookAt), "Coincident positions must not produce NaN shots.");
    }

    private static void Placement(Action<bool, string> check)
    {
        Vector3 npc = new Vector3(0, 0, 0);
        Vector3 p = DialogueShotMath.PlaceListener(new Vector3(0, 0.2f, 0.5f), npc, 0.8f, 1.5f, out bool moved);
        check(moved && Near(DialogueShotMath.Horizontal(p - npc).Length(), 0.8f) && Near(p.Y, 0.2f) && p.Z > 0,
            "Too close: Elijah moves back along the line to the minimum separation, keeping his floor height.");
        p = DialogueShotMath.PlaceListener(new Vector3(3f, 0, 0), npc, 0.8f, 1.5f, out moved);
        check(moved && Near(p.X, 1.5f) && Near(p.Z, 0f), "Too far: Elijah is drawn in to the maximum separation.");
        Vector3 inRange = new Vector3(0.6f, 0.1f, 0.9f);
        p = DialogueShotMath.PlaceListener(inRange, npc, 0.8f, 1.5f, out moved);
        check(!moved && p == inRange, "In range: Elijah stands exactly where the player is.");
        p = DialogueShotMath.PlaceListener(npc, npc, 0.8f, 1.5f, out moved);
        check(moved && Near(DialogueShotMath.Horizontal(p - npc).Length(), 0.8f), "Coincident positions must still separate.");
        p = DialogueShotMath.PlaceListener(new Vector3(0, 0, 5f), npc, 2f, 1f, out moved);
        check(moved && Near(p.Z, 2f), "Swapped min/max must be handled.");
    }

    private static void CloseUpPolicy(Action<bool, string> check)
    {
        // Scores for real lines from the game.
        check(DialogueCloseUpPolicy.Score("Wow.") >= 2f, "\"Wow.\" must call for a close-up.");
        check(DialogueCloseUpPolicy.Score("BUSINESS.") >= 2f, "A shouted word must call for a close-up.");
        check(DialogueCloseUpPolicy.Score("I HAVE BEEN SENT TO THIS PLANET FOR A TEMPORARY COMMERCIAL PROJECT.") >= 2f,
            "A shouted line must call for a close-up.");
        check(DialogueCloseUpPolicy.Score("...Right.") >= 2f, "A short dramatic beat must call for a close-up.");
        check(DialogueCloseUpPolicy.Score("Excellent work! Every profile is in its correct spot.") >= 2f,
            "An emphatic congratulation must call for a close-up.");
        check(DialogueCloseUpPolicy.Score("Farther away?") >= 2f, "A short pointed question must call for a close-up.");
        check(DialogueCloseUpPolicy.Score("Good afternoon. Mind if I ask you a few questions? I'm the new tax consultant assigned to the town.") < 1f,
            "An ordinary long line must stay on the over-the-shoulder shot.");
        check(DialogueCloseUpPolicy.Score("I work at the BPO office nearby. I've been there for a few years now.") < 1f,
            "Short acronyms such as BPO must not count as shouting.");
        float yes = DialogueCloseUpPolicy.Score("Yes.");
        check(yes >= 1f && yes < 2f, "A one-word reply is borderline: close-up only by chance.");
        check(DialogueCloseUpPolicy.Score("<b>WOW</b>") >= DialogueCloseUpPolicy.Score("WOW") - 1e-4f &&
              DialogueCloseUpPolicy.Score("<color=#FF0000>calm words here for you</color>") < 1f,
            "Rich-text tags must be ignored when scoring.");
        check(DialogueCloseUpPolicy.Score("") == 0f && DialogueCloseUpPolicy.Score(null) == 0f, "Empty text must score zero.");

        DialogueLine L(string text, DialogueCameraHint hint = DialogueCameraHint.Auto) =>
            new DialogueLine(DialogueSpeaker.Npc, text) { CameraHint = hint };

        var policy = new DialogueCloseUpPolicy { RandomChance = 0f };
        policy.Reset(1);
        check(!policy.Decide(L("WOW!"), 0), "The first line must establish the space, never a close-up.");
        check(policy.Decide(L("WOW!"), 1), "An emphatic line after the first must be a close-up.");
        check(!policy.Decide(L("WOW!"), 2) && !policy.Decide(L("WOW!"), 3), "Automatic close-ups must be spaced out.");
        check(policy.Decide(L("WOW!"), 4), "After the gap, another emphatic line may cut close.");
        check(!policy.Decide(L("Just an ordinary sentence that keeps going."), 8), "Ordinary lines stay wide.");
        check(policy.Decide(L("Just an ordinary sentence that keeps going.", DialogueCameraHint.CloseUp), 9),
            "An intentional CloseUp hint must always cut close.");
        check(policy.Decide(L("Another ordinary line for you.", DialogueCameraHint.CloseUp), 0),
            "An intentional CloseUp hint must work even on the first line.");
        check(!policy.Decide(L("WOW!", DialogueCameraHint.OverShoulder), 20), "An OverShoulder hint must forbid a close-up.");
        check(!policy.Decide(null, 5), "A missing line must not cut close.");

        // Random close-ups on borderline lines: roughly the configured rate, reproducible per seed.
        var random = new DialogueCloseUpPolicy { RandomChance = 0.15f, MinLinesBetween = 0 };
        int hits = 0;
        random.Reset(12345);
        for (int i = 1; i <= 2000; i++) if (random.Decide(L("Yes."), i)) hits++;
        check(hits > 200 && hits < 400, $"Borderline lines must cut close about 15% of the time (got {hits}/2000).");
        var again = new DialogueCloseUpPolicy { RandomChance = 0.15f, MinLinesBetween = 0 };
        again.Reset(12345);
        int hits2 = 0;
        for (int i = 1; i <= 2000; i++) if (again.Decide(L("Yes."), i)) hits2++;
        check(hits == hits2, "The same seed must give the same random close-ups.");
        var never = new DialogueCloseUpPolicy { RandomChance = 0.15f, MinLinesBetween = 0 };
        never.Reset(7);
        bool any = false;
        for (int i = 1; i <= 500; i++) any |= never.Decide(L("Good afternoon, how has your week been so far?"), i);
        check(!any, "Lines below the random floor must never get a random close-up.");
    }

    private static void CameraHintSurvivesSnapshot(Action<bool, string> check)
    {
        var lines = new DialogueBuilder("NPC").Npc("Plain.").Npc("Look at me.").CloseUp().Player("Fine.").OverShoulder().Build();
        check(lines[0].CameraHint == DialogueCameraHint.Auto && lines[1].CameraHint == DialogueCameraHint.CloseUp &&
              lines[2].CameraHint == DialogueCameraHint.OverShoulder, "DialogueBuilder must mark the previous line's camera hint.");
        var session = new DialogueSession();
        check(session.TryStart(lines, () => { }), "Hinted lines must be admitted.");
        lines[1].CameraHint = DialogueCameraHint.Auto; // mutate the source after admission
        session.TryAdvance(out _);
        check(session.CurrentLine.CameraHint == DialogueCameraHint.CloseUp, "The session snapshot must keep each line's camera hint.");
        check(new DialogueBuilder().CloseUp().Build().Count == 0, "A hint with no previous line must be ignored.");
    }

    // ------------------------------------------------------------------
    private static float ScreenX(DialogueShotPose shot, Vector3 point)
    {
        Vector3 forward = DialogueShotMath.HorizontalDirection(shot.Position, shot.LookAt, new Vector3(0, 0, 1));
        return Vector3.Dot(point - shot.Position, DialogueShotMath.RightOf(forward));
    }

    private static float AngleDeg(Vector3 a, Vector3 b)
    {
        float d = Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b));
        return (float)(Math.Acos(Math.Max(-1f, Math.Min(1f, d))) * 180.0 / Math.PI);
    }

    private static bool Near(float a, float b, float eps = 1e-4f) => Math.Abs(a - b) <= eps;

    private static bool IsFinite(Vector3 v) =>
        !float.IsNaN(v.X) && !float.IsNaN(v.Y) && !float.IsNaN(v.Z) &&
        !float.IsInfinity(v.X) && !float.IsInfinity(v.Y) && !float.IsInfinity(v.Z);
}
