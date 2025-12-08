using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreFaker : Subscriber
{
    public int DayIndex;
    public int IncorrectGuesses;
    public int QuotaAmount = 1125;

    public List<int> CorrectGuesses;

    [Header("Event Listeners")]
    public UnitGameEvent CurrentDaySummaryRequest;

    [Header("Events")]
    public DaySummaryGameEvent DisplayCurrentDaySummary;

    public override void Subscribe()
    {
        int managerCount = FindObjectsOfType<ScoreManager>().Count();
        if (managerCount > 0)
        {
            Destroy(gameObject);
            return;
        }
        CurrentDaySummaryRequest?.Subscribe(OnCurrentDaySummaryRequest);
    }

    private void OnCurrentDaySummaryRequest()
    {
        int total = CorrectGuesses.Sum();
        DaySummary summary = new DaySummary(DayIndex);
        foreach (int score in CorrectGuesses)
        {
            summary.Scores.Add(score);
        }
        for (int _ = 0; _ < IncorrectGuesses; _++)
        {
            summary.Scores.Add(0);
        }
        summary.completedAppeals = summary.Scores.Count;
        summary.correctAppeals = CorrectGuesses.Count;
        DisplayCurrentDaySummary?.Emit((summary, QuotaAmount, total > QuotaAmount, total));
    }
}
