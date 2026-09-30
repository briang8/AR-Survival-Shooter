using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Fills the leaderboard panel with the last 5 saved sessions
public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TMP_Text entriesText;
    [SerializeField] private LeaderboardManager leaderboardManager;

    public void Open()
    {
        List<SessionRecord> records = leaderboardManager.LoadSessions();

        if (records.Count == 0)
        {
            entriesText.text = "No sessions yet.";
        }
        else
        {
            entriesText.text = "";
            for (int i = 0; i < records.Count; i++)
            {
                SessionRecord r = records[i];
                entriesText.text += (i + 1) + ". Score " + r.score
                    + "   Defeated " + r.enemiesDefeated
                    + "   Time " + r.timeSurvived.ToString("F1") + "s"
                    + "   (" + r.date + ")\n";
            }
        }

        leaderboardPanel.SetActive(true);
    }

    public void Close()
    {
        leaderboardPanel.SetActive(false);
    }
}