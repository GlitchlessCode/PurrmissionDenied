using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public struct DaySummary
{
    public readonly int DayIndex;

    public List<int> Scores;
    public int TotalScore
    {
        get => Scores.Sum();
    }

    public int completedAppeals;
    public int correctAppeals;

    public DaySummary(int day)
    {
        DayIndex = day;
        Scores = new List<int>();
        completedAppeals = 0;
        correctAppeals = 0;
    }
}

public class ScoreManager : Subscriber
{
    [Header("Score Calculation Details")]
    [SerializeField]
    private int SuccessScore = 100;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    // This is the ratio of Quota = SuccessScore * AppealCount * QuotaRatio
    private float QuotaRatio = 0.75f;

    public int StreakStart = 3;
    public float StreakStartMultiplier = 1.05f;
    public int StreakEnd = 5;
    public float StreakEndMultiplier = 1.35f;

    private int activeStreak = 0;

    private List<DaySummary> gameSummary = new List<DaySummary>();
    private DaySummary? currentDay;

    [Header("Event Listeners")]
    public IntGameEvent DayStart;
    public UnitGameEvent DayFinished;
    public UnitGameEvent CurrentDaySummaryRequest;
    public BoolGameEvent AfterAppeal;

    [Header("Events")]
    public DaySummaryGameEvent DisplayCurrentDaySummary;

    private bool setup = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void Subscribe()
    {
        if (!setup)
        {
            int managerCount = FindObjectsOfType<ScoreManager>().Count();
            if (managerCount > 1)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                setup = true;
            }
        }
        AfterAppeal?.Subscribe(OnAfterAppeal);
        DayStart?.Subscribe(OnDayStart);
        DayFinished?.Subscribe(OnDayFinished);
        CurrentDaySummaryRequest?.Subscribe(OnCurrentDaySummaryRequest);
        activeStreak = 0;
    }

    private void OnAfterAppeal(bool correct)
    {
        if (currentDay != null)
        {
            DaySummary current = currentDay.Value;
            int score = ComputeScore(correct);
            current.Scores.Add(score);

            current.completedAppeals += 1;
            if (correct)
            {
                current.correctAppeals += 1;
            }
            currentDay = current;
        }
    }

    private int ComputeScore(bool correct)
    {
        if (!correct)
        {
            activeStreak = 0;
            return 0;
        }

        activeStreak++;
        if (activeStreak < StreakStart)
        {
            return SuccessScore;
        }
        if (activeStreak > StreakEnd)
        {
            return (int)Math.Round(SuccessScore * StreakEndMultiplier);
        }
        float ratio = (float)(activeStreak - StreakStart) / (float)(StreakEnd - StreakStart);
        return (int)
            Math.Round(
                SuccessScore
                    * ((1.0f - ratio) * StreakStartMultiplier + ratio * StreakEndMultiplier)
            );
    }

    private void OnDayFinished()
    {
        if (currentDay != null)
        {
            gameSummary.Add(currentDay.Value);
        }
    }

    private void OnDayStart(int dayIndex)
    {
        currentDay = new DaySummary(dayIndex);
    }

    private void OnCurrentDaySummaryRequest()
    {
        if (currentDay != null)
        {
            DaySummary current = currentDay.Value;
            int totalScore = 0;
            foreach (DaySummary summary in gameSummary)
            {
                totalScore += summary.TotalScore;
            }
            int quota = (int)(current.completedAppeals * SuccessScore * QuotaRatio);
            bool passedQuota = current.TotalScore >= quota;
            DisplayCurrentDaySummary?.Emit((current, quota, passedQuota, totalScore));
        }
    }
}
