using System.Collections.Generic;
using UnityEngine;

// Saves and loads the last 5 session results using PlayerPrefs
public class LeaderboardManager : MonoBehaviour
{
    private const string SaveKey = "Leaderboard";
    private const int MaxEntries = 5;

    public void SaveSession(int score, int enemiesDefeated, float timeSurvived)
    {
        List<SessionRecord> records = LoadSessions();

        records.Insert(0, new SessionRecord(score, enemiesDefeated, timeSurvived));

        // keep only the newest 5
        if (records.Count > MaxEntries)
        {
            records.RemoveRange(MaxEntries, records.Count - MaxEntries);
        }

        SessionRecordList wrapper = new SessionRecordList();
        wrapper.records = records;

        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public List<SessionRecord> LoadSessions()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            return new List<SessionRecord>();
        }

        string json = PlayerPrefs.GetString(SaveKey);
        SessionRecordList wrapper = JsonUtility.FromJson<SessionRecordList>(json);
        return wrapper.records;
    }
}