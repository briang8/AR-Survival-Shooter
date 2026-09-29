using UnityEngine;

// The round is running: enemies spawn and the timer counts down
public class PlayState : IGameState
{
    private GameManager manager;

    public PlayState(GameManager manager)
    {
        this.manager = manager;
    }

    public void Enter()
    {
        Debug.Log("State: Play");
        manager.StartRound();
    }

    public void Tick()
    {
        manager.UpdateTimer();
    }

    public void Exit()
    {
        // stop spawning and remove every enemy when the round ends
        manager.StopRound();
    }
}