using System;

// One saved leaderboard entry
[Serializable]
public class SessionRecord
{
    public int score;
    public int enemiesDefeated;
    public float timeSurvived;
    public string date;

    public SessionRecord(int score, int enemiesDefeated, float timeSurvived)
    {
        this.score = score;
        this.enemiesDefeated = enemiesDefeated;
        this.timeSurvived = timeSurvived;
        this.date = DateTime.Now.ToString("MMM d, HH:mm");
    }
}