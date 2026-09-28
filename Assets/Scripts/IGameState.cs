using  UnityEngine;

// every game state (placement, play, end) follows this shape
public interface IGameState
{
    void Enter();
    void Tick();
    void Exit();
}