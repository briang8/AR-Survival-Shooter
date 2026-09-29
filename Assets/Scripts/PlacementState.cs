using UnityEngine;

// Waiting for the player to tap a detected plane to place the world
public class PlacementState : IGameState
{
    private GameManager manager;

    public PlacementState(GameManager manager)
    {
        this.manager = manager;
    }

    public void Enter()
    {
        Debug.Log("State: Placement");
    }

    public void Tick()
    {
    }

    public void Exit()
    {
    }
}