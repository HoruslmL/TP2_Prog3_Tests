using UnityEngine;

public class GameStateMachine
{
    public GameState CurrentState { get; private set; }

    public GameStateMachine()
    {
        CurrentState = GameState.Initializing;
    }

    public bool TryChangeState(GameState nextState)
    {
        if (!CanTransition(CurrentState, nextState))
            return false;

        CurrentState = nextState;
        return true;
    }

    private bool CanTransition(
        GameState currentState,
        GameState nextState)
    {
        switch (currentState)
        {
            case GameState.Initializing:
                return nextState == GameState.Aiming;

            case GameState.Aiming:
                return nextState == GameState.Resolving ||
                       nextState == GameState.Defeat;

            case GameState.Resolving:
                return nextState == GameState.Collecting;

            case GameState.Collecting:
                return nextState == GameState.Aiming ||
                       nextState == GameState.Victory ||
                       nextState == GameState.Defeat;

            default:
                return false;
        }
    }
}
