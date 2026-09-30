using UnityEngine;

// The main menu is showing: nothing runs until the player presses Start
public class MenuState : IGameState
{
    private GameManager manager;

    public MenuState(GameManager manager)
    {
        this.manager = manager;
    }

    public void Enter()
    {
        Debug.Log("State: Menu");
    }

    public void Tick()
    {
    }

    public void Exit()
    {
    }
}