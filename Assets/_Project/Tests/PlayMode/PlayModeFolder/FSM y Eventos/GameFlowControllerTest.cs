using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class GameFlowControllerTest
{
    private GameObject gameObject;
    private GameFlowController controller;

    private bool movementEventReceived;

    [SetUp]
    public void SetUp()
    {
        gameObject = new GameObject("GameFlowController");
        controller = gameObject.AddComponent<GameFlowController>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(gameObject);
    }

    [Test]
    public void NewController_StartsInInitializing()
    {
        Assert.AreEqual(GameState.Initializing, controller.CurrentState);
    }

    [Test]
    public void Controller_CanChangeState()
    {
        bool changed = controller.TryChangeState(GameState.Aiming);

        Assert.IsTrue(changed);
        Assert.AreEqual(GameState.Aiming, controller.CurrentState);
    }

    [Test]
    public void Controller_RejectsInvalidTransition()
    {
        bool changed = controller.TryChangeState(GameState.Collecting);

        Assert.IsFalse(changed);
        Assert.AreEqual(GameState.Initializing, controller.CurrentState);
    }
    [Test]
    public void BallLaunchedEvent_ChangesStateToResolving()
    {
        controller.TryChangeState(GameState.Aiming);

        GameEvents.RaiseBallLaunched();

        Assert.AreEqual(GameState.Resolving, controller.CurrentState);
    }

    [Test]
    public void AllBallsStoppedEvent_DoesNotChangeStatePrematurely()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        GameEvents.RaiseAllBallsStopped();

        Assert.AreEqual(GameState.Resolving, controller.CurrentState);
    }

    [Test]
    public void EffectsResolvedEvent_DoesNotChangeStatePrematurely()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        GameEvents.RaiseEffectsResolved();

        Assert.AreEqual(GameState.Resolving, controller.CurrentState);
    }

    [Test]
    public void StabilityConditions_NotMetUntilBothEventsAreReceived()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        GameEvents.RaiseAllBallsStopped();

        Assert.IsFalse(GetStabilityCondition());

        GameEvents.RaiseEffectsResolved();

        Assert.IsTrue(GetStabilityCondition());
    }

    [Test]
    public void StabilityConditions_WorkRegardlessOfEventOrder()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        GameEvents.RaiseEffectsResolved();

        Assert.IsFalse(GetStabilityCondition());

        GameEvents.RaiseAllBallsStopped();

        Assert.IsTrue(GetStabilityCondition());
    }

    [Test]
    public void NewMovementStartedEvent_ResetsStabilityConditions()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        GameEvents.RaiseAllBallsStopped();
        GameEvents.RaiseEffectsResolved();

        Assert.IsTrue(controller.CanStartStabilityCountdown());

        GameEvents.RaiseNewMovementStarted();

        Assert.IsFalse(controller.CanStartStabilityCountdown());
        Assert.AreEqual(GameState.Resolving, controller.CurrentState);
    }

    [UnityTest]
    public IEnumerator BallStartsMoving_DuringResolving_RaisesNewMovementEvent()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        BallData data = new BallData(
            "TestBall",
            1f,
            BallAbilityType.None
        );

        GameObject ballObject = new GameObject("TestBall");

        ballObject.AddComponent<CircleCollider2D>();
        ballObject.AddComponent<Rigidbody2D>();

        BallController ball = ballObject.AddComponent<BallController>();

        // Inicializamos la bolita.
        ball.Initialize(data);

        movementEventReceived = false;
        GameEvents.NewMovementStarted += OnMovementStarted;

        yield return new WaitForFixedUpdate();

        Rigidbody2D rb = ballObject.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.right * 2f;

        yield return new WaitForFixedUpdate();

        Assert.IsTrue(movementEventReceived);

        GameEvents.NewMovementStarted -= OnMovementStarted;

        Object.Destroy(ballObject);
    }

    [Test]
    public void NewMovementStartedEvent_ResetsStabilityTimer()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        GameEvents.RaiseAllBallsStopped();
        GameEvents.RaiseEffectsResolved();

        Assert.IsTrue(controller.CanStartStabilityCountdown());

        GameEvents.RaiseNewMovementStarted();

        Assert.IsFalse(controller.CanStartStabilityCountdown());
    }

    [UnityTest]
    public IEnumerator Resolving_TransitionsToCollecting_AfterThreeSeconds()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        GameEvents.RaiseAllBallsStopped();
        GameEvents.RaiseEffectsResolved();

        yield return new WaitForSeconds(2f);

        Assert.AreEqual(GameState.Resolving, controller.CurrentState);

        yield return new WaitForSeconds(1.2f);

        Assert.AreEqual(GameState.Collecting, controller.CurrentState);
    }

    [Test]
    public void Collecting_CanReturnToAiming()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        GameEvents.RaiseAllBallsStopped();
        GameEvents.RaiseEffectsResolved();

        controller.TryChangeState(GameState.Collecting);

        bool changed = controller.TryChangeState(GameState.Aiming);

        Assert.IsTrue(changed);
        Assert.AreEqual(GameState.Aiming, controller.CurrentState);
    }

    [Test]
    public void Collecting_CanTransitionToVictory()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        controller.TryChangeState(GameState.Collecting);

        bool changed = controller.TryChangeState(GameState.Victory);

        Assert.IsTrue(changed);
        Assert.AreEqual(GameState.Victory, controller.CurrentState);
    }

    [Test]
    public void Collecting_CanTransitionToDefeat()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        controller.TryChangeState(GameState.Collecting);

        bool changed = controller.TryChangeState(GameState.Defeat);

        Assert.IsTrue(changed);
        Assert.AreEqual(GameState.Defeat, controller.CurrentState);
    }

    [Test]
    public void Victory_DoesNotAllowFurtherTransitions()
    {
        controller.TryChangeState(GameState.Aiming);
        controller.TryChangeState(GameState.Resolving);
        controller.TryChangeState(GameState.Collecting);
        controller.TryChangeState(GameState.Victory);

        bool changed = controller.TryChangeState(GameState.Aiming);

        Assert.IsFalse(changed);
        Assert.AreEqual(GameState.Victory, controller.CurrentState);
    }

    [Test]
    public void Defeat_DoesNotAllowFurtherTransitions()
    {
        controller.TryChangeState(GameState.Aiming);
        controller.TryChangeState(GameState.Defeat);

        bool changed = controller.TryChangeState(GameState.Aiming);

        Assert.IsFalse(changed);
        Assert.AreEqual(GameState.Defeat, controller.CurrentState);
    }

    [UnityTest]
    public IEnumerator NewMovementStarted_ResetsStabilityCountdown()
    {
        controller.TryChangeState(GameState.Aiming);
        GameEvents.RaiseBallLaunched();

        GameEvents.RaiseAllBallsStopped();
        GameEvents.RaiseEffectsResolved();

        yield return new WaitForSeconds(2f);

        GameEvents.RaiseNewMovementStarted();

        Assert.AreEqual(GameState.Resolving, controller.CurrentState);

        GameEvents.RaiseAllBallsStopped();

        yield return new WaitForSeconds(2.5f);

        Assert.AreEqual(GameState.Resolving, controller.CurrentState);

        yield return new WaitForSeconds(0.7f);

        Assert.AreEqual(GameState.Collecting, controller.CurrentState);
    }

    private bool GetStabilityCondition()
    {
        return controller.CanStartStabilityCountdown();
    }

    private void OnMovementStarted()
    {
        movementEventReceived = true;
    }
}
