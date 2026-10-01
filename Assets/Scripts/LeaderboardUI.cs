using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Fills the leaderboard panel with the last 5 saved sessions
public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TMP_Text entriesText;
    [SerializeField] private LeaderboardManager leaderboardManager;

    public GameObject Panel => leaderboardPanel;

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

        UIManager uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.OpenExclusivePopup(leaderboardPanel);
        }
        else
        {
            leaderboardPanel.SetActive(true);
        }

        Transform closeButton = leaderboardPanel.transform.Find("CloseButton");
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
        }
    }

    public void Close()
    {
        UIManager uiManager = FindAnyObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ClosePopup(leaderboardPanel);
        }
        else
        {
            leaderboardPanel.SetActive(false);
        }
    }
}