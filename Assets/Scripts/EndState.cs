using UnityEngine;

// The round is over: final stats are ready for the end screen
public class EndState : IGameState
{
    private GameManager manager;

    public EndState(GameManager manager)
    {
        this.manager = manager;
    }

    public void Enter()
    {
        Debug.Log("State: End | Score: " + manager.Score
            + " | Defeated: " + manager.EnemiesDefeated
            + " | Survived: " + manager.TimeSurvived.ToString("F1") + "s");

            Object.FindAnyObjectByType<LeaderboardManager>()
            .SaveSession(manager.Score, manager.EnemiesDefeated, manager.TimeSurvived);   
    }

    public void Tick()
    {
    }

    public void Exit()
    {
    }
}