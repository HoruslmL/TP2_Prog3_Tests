using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    private GameStateMachine stateMachine;

    private bool allBallsStopped;
    private bool effectsResolved;
    private float stabilityTimer;
    private const float StabilityDuration = 3f;

    public GameState CurrentState => stateMachine.CurrentState;

    private void Awake()
    {
        stateMachine = new GameStateMachine();
    }

    private void Update()
    {
        if (CurrentState != GameState.Resolving)
            return;

        if (!CanStartStabilityCountdown())
        {
            stabilityTimer = 0f;
            return;
        }

        stabilityTimer += Time.deltaTime;

        if (stabilityTimer >= StabilityDuration)
        {
            TryChangeState(GameState.Collecting);
            stabilityTimer = 0f;
        }
    }

    private void OnEnable()
    {
        GameEvents.BallLaunched += OnBallLaunched;
        GameEvents.AllBallsStopped += OnAllBallsStopped;
        GameEvents.EffectsResolved += OnEffectsResolved;
        GameEvents.NewMovementStarted += OnNewMovementStarted;
    }

    private void OnDisable()
    {
        GameEvents.BallLaunched -= OnBallLaunched;
        GameEvents.AllBallsStopped -= OnAllBallsStopped;
        GameEvents.EffectsResolved -= OnEffectsResolved;
        GameEvents.NewMovementStarted -= OnNewMovementStarted;
    }

    public bool TryChangeState(GameState nextState)
    {
        return stateMachine.TryChangeState(nextState);
    }

    private void OnBallLaunched()
    {
        if (CurrentState != GameState.Aiming)
            return;

        allBallsStopped = false;
        effectsResolved = false;
        stabilityTimer = 0f;

        TryChangeState(GameState.Resolving);
    }

    private void OnAllBallsStopped()
    {
        if (CurrentState != GameState.Resolving)
            return;

        allBallsStopped = true;
    }

    private void OnEffectsResolved()
    {
        if (CurrentState != GameState.Resolving)
            return;

        effectsResolved = true;
    }
    public bool CanStartStabilityCountdown()
    {
        return allBallsStopped && effectsResolved;
    }

    private void OnNewMovementStarted()
    {
        if (CurrentState != GameState.Resolving)
            return;

        allBallsStopped = false;
        stabilityTimer = 0f;
    }
}
