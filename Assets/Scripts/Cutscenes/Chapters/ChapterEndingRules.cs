using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>Presentation only: a durable handoff after service, before the existing chapter result.</summary>
public static class ChapterEndingRules
{
    public static string SceneFor(string chapter) => chapter == "Level_01" ? "CS_Chapter01_End"
        : chapter == "Level_02" ? "CS_Chapter02_End" : chapter == "Level_03" ? "CS_Chapter03_End" : null;

    public static bool Eligible(PlayerProgressData data, string chapter)
    {
        if (data == null || !data.HasCampaign) return false;
        if (chapter == "Level_01") return CaseResultRules.WrapUpPending(data);
        if (chapter == "Level_02")
        {
            RosaEthicsRecord ethics = data.campaign.rosaEthics;
            return Level2CampaignRules.ChapterComplete(data) &&
                !(ethics != null && ethics.committed && ethics.followThroughRequired && !ethics.followThroughDone);
        }
        return chapter == "Level_03" && Level3ClosingRules.StageOf(data.campaign.levelThree) == Level3ClosingRules.Stage.Result;
    }

    public static bool Pending(PlayerProgressData data, string chapter = null) => data != null && data.HasCampaign &&
        data.campaign.ending != null && SceneFor(data.campaign.ending.chapterId) != null &&
        (chapter == null || data.campaign.ending.chapterId == chapter);

    public static string RosaVariant(RosaEthicsRecord ethics)
    {
        if (ethics == null || !ethics.committed || string.IsNullOrEmpty(ethics.choiceId)) return "rosa.neutral";
        if (ethics.followThroughDone && ethics.choiceId.EndsWith("/responsible", StringComparison.Ordinal)) return "rosa.responsible_completed";
        if (ethics.followThroughDone && ethics.choiceId.EndsWith("/unsupported-promise", StringComparison.Ordinal)) return "rosa.promise_resolved";
        return ethics.choiceId.EndsWith("/dismissive-hold", StringComparison.Ordinal) ? "rosa.dismissive_completed" : "rosa.neutral";
    }

    public static bool Begin(PlayerProgressData data, string chapter, string resultJson = "")
    {
        if (!Eligible(data, chapter) || Pending(data)) return false;
        data.campaign.ending = new ChapterEndingRecord
        {
            chapterId = chapter,
            variant = chapter == "Level_02" ? RosaVariant(data.campaign.rosaEthics) : "",
            resultJson = resultJson ?? ""
        };
        return true;
    }

    public static bool Consume(PlayerProgressData data, string chapter, ChapterCinematicOutcome outcome)
    {
        if (!Pending(data, chapter) || outcome == ChapterCinematicOutcome.Aborted) return false;
        data.campaign.ending.consumed = true;
        return true;
    }

    public static void Acknowledge(PlayerProgressData data, string chapter)
    {
        if (Pending(data, chapter)) data.campaign.ending = new ChapterEndingRecord();
    }

    /// <summary>
    /// Runs a coroutine body (and any coroutines it yields) so an exception ends that body
    /// instead of killing the caller: Unity abandons a coroutine that throws, with no way back.
    /// </summary>
    public static IEnumerator Guarded(IEnumerator body, Action<Exception> failed)
    {
        var running = new Stack<IEnumerator>();
        running.Push(body);
        while (running.Count > 0)
        {
            object step = null;
            bool advanced = false;
            try
            {
                advanced = running.Peek().MoveNext();
                if (advanced) step = running.Peek().Current;
            }
            catch (Exception error)
            {
                failed?.Invoke(error);
                yield break;
            }
            if (!advanced) running.Pop();
            else if (step is IEnumerator nested) running.Push(nested);
            else yield return step;
        }
    }
}
