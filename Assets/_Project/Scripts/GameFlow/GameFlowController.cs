using System.Text;
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

    private void OnGUI()
    {
        StringBuilder summary = new StringBuilder();
        summary.AppendLine($"Game State: {CurrentState}");
        summary.AppendLine($"AllBallsStopped: {allBallsStopped}");
        summary.AppendLine($"EffectsResolved: {effectsResolved}");
        summary.AppendLine($"CanStartStabilityCountdown: {CanStartStabilityCountdown()}");
        summary.AppendLine($"StabilityTimer: {stabilityTimer:F2}s / {StabilityDuration:F2}s");
        summary.AppendLine("Balls:");

        BallController[] balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);
        if (balls.Length == 0)
        {
            summary.AppendLine("- none");
        }
        else
        {
            foreach (BallController ball in balls)
            {
                BallRuntime runtime = ball != null ? ball.Runtime : null;
                string stateText = runtime != null ? runtime.State.ToString() : "Uninitialized";
                summary.AppendLine($"- {ball.name}: {stateText}");
            }
        }

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = new GUIStyleState { textColor = Color.white },
            wordWrap = true
        };

        GUI.Label(new Rect(20f, 20f, 500f, 260f), summary.ToString(), style);
    }

    private void Start()
    {
        if (CurrentState == GameState.Initializing)
            TryChangeState(GameState.Aiming);
    }

    private void Update()
    {
        if (CurrentState != GameState.Resolving)
            return;

        UpdateResolutionEvents();

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

    private void UpdateResolutionEvents()
    {
        bool anyBallMoving = false;
        bool anyEffectPending = false;

        BallController[] balls = FindObjectsByType<BallController>(
            FindObjectsSortMode.None
        );

        foreach (BallController ball in balls)
        {
            BallRuntime runtime = ball.Runtime;
            if (runtime == null)
                continue;

            if (runtime.State == BallRuntimeState.Active ||
                runtime.State == BallRuntimeState.PendingEffect ||
                runtime.State == BallRuntimeState.EffectExecuted)
            {
                anyBallMoving |= ball.IsMoving;
                anyEffectPending |=
                    runtime.State == BallRuntimeState.PendingEffect;
            }
        }

        if (anyBallMoving)
        {
            allBallsStopped = false;
            stabilityTimer = 0f;
        }
        else if (!allBallsStopped)
        {
            GameEvents.RaiseAllBallsStopped();
        }

        if (anyEffectPending)
            effectsResolved = false;
        else if (!effectsResolved)
            GameEvents.RaiseEffectsResolved();
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
        effectsResolved = false;
        stabilityTimer = 0f;
    }
}
