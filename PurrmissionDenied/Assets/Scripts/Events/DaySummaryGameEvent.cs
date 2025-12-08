using UnityEngine;

[CreateAssetMenu(menuName = "Game Events/DaySummaryGameEvent")]
// Summary, quota, passedQuota, totalScore
public class DaySummaryGameEvent : GameEvent<(DaySummary, int, bool, int)> { }
