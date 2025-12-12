using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RecordKeeper : Subscriber
{
    public enum AppealChoice
    {
        Approved,
        Denied,
    }

    public struct AppealRecord
    {
        public int Index;
        public UserEntry User;
        public AppealChoice Choice;
        public bool Correct;
        public string MistakesText;
        public int Score;
        public int Streak;
    }

    public struct Solidified
    {
        public int RecordCount;
        public string RuleText;

        public Solidified(int records, string rules)
        {
            RecordCount = records;
            RuleText = rules;
        }
    }

    [Header("Event Listeners")]
    public StringGameEvent ValidatorReady;
    public UserEntryGameEvent UserLoaded;
    public BoolGameEvent ResolveAppeal;
    public BoolGameEvent AfterAppeal;
    public StringGameEvent MistakesText;
    public DaySummaryGameEvent DaySummary;
    public UnitGameEvent Solidify;
    public IntGameEvent RequestRecord;

    [Header("Events")]
    public AppealRecordGameEvent FoundRecord;
    public SolidifiedGameEvent RecordsSolidified;

    private bool setup = false;

    private bool solid = false;

    // Unsolid
    private string ruleText;
    private List<UserEntry> userEntries;
    private List<AppealChoice> choices;
    private List<bool> correctnesses;
    private List<string> bubbledMistakeTexts;
    private List<int> scores;

    // Solid
    private List<AppealRecord> records;

    public override void Subscribe()
    {
        if (!setup)
        {
            int managerCount = FindObjectsOfType<RecordKeeper>().Count();
            if (managerCount > 1)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                Reset();
                DontDestroyOnLoad(gameObject);
                setup = true;
            }
        }

        ValidatorReady?.Subscribe(OnValidatorReady);
        UserLoaded?.Subscribe(OnUserLoaded);
        ResolveAppeal?.Subscribe(OnResolveAppeal);
        AfterAppeal?.Subscribe(OnAfterAppeal);
        MistakesText?.Subscribe(OnMistakesText);
        DaySummary?.Subscribe(OnDaySummary);
        Solidify?.Subscribe(OnSolidify);
        RequestRecord?.Subscribe(OnRequestRecord);

        ValidatorReady?.Subscribe(OnUnsolidify);
        UserLoaded?.Subscribe(OnUnsolidify);
        ResolveAppeal?.Subscribe(OnUnsolidify);
        AfterAppeal?.Subscribe(OnUnsolidify);
        MistakesText?.Subscribe(OnUnsolidify);
        DaySummary?.Subscribe(OnUnsolidify);
    }

    private void Reset()
    {
        ruleText = "No rules loaded...";
        userEntries = new List<UserEntry>();
        choices = new List<AppealChoice>();
        correctnesses = new List<bool>();
        bubbledMistakeTexts = new List<string>();
        scores = new List<int>();
    }

    private void OnValidatorReady(string rules)
    {
        ruleText = rules;
    }

    private void OnUserLoaded(UserEntry user)
    {
        if (!userEntries.Contains(user))
            userEntries.Add(user);
    }

    private void OnResolveAppeal(bool approve)
    {
        if (approve)
        {
            choices.Add(AppealChoice.Approved);
        }
        else
        {
            choices.Add(AppealChoice.Denied);
        }
    }

    private void OnAfterAppeal(bool correct)
    {
        if (correct)
            bubbledMistakeTexts.Add("");

        correctnesses.Add(correct);
    }

    private void OnMistakesText(string mistakeText)
    {
        bubbledMistakeTexts.Add(mistakeText);
    }

    private void OnDaySummary((DaySummary, int, bool, int) summaryTuple)
    {
        var (summary, _, _, _) = summaryTuple;
        scores = summary.Scores;
    }

    private void OnUnsolidify<T>(T _)
    {
        solid = false;
    }

    private void OnSolidify()
    {
        if (solid)
            return;

        records = new List<AppealRecord>();

        int minLen = new int[]
        {
            userEntries.Count,
            choices.Count,
            correctnesses.Count,
            bubbledMistakeTexts.Count,
            scores.Count,
        }.Min();

        int streak = 0;
        for (int idx = 0; idx < minLen; idx++)
        {
            if (correctnesses[idx])
                streak++;
            else
                streak = 0;

            AppealRecord record = new AppealRecord()
            {
                Index = idx,
                User = userEntries[idx],
                Choice = choices[idx],
                Correct = correctnesses[idx],
                MistakesText = bubbledMistakeTexts[idx],
                Score = scores[idx],
                Streak = streak,
            };

            records.Add(record);
        }

        solid = true;
        RecordsSolidified?.Emit(new Solidified(records.Count, ruleText));
        Reset();
    }

    private void OnRequestRecord(int recordIdx)
    {
        if ((!solid) || (recordIdx >= records.Count))
            return;

        FoundRecord?.Emit(records[recordIdx]);
    }
}
