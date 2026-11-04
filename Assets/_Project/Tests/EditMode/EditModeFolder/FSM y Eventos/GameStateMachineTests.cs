using NUnit.Framework;
using UnityEngine;

public class GameStateMachineTests
{
    [Test]
    public void NewGameStateMachine_StartsInInitializing()
    {
        // ARRANGE
        var machine = new GameStateMachine();

        // ASSERT
        Assert.AreEqual(
            GameState.Initializing,
            machine.CurrentState
        );
    }

    [Test]
    public void Initializing_CanTransitionToAiming()
    {
        // ARRANGE
        var machine = new GameStateMachine();

        // ACT
        bool changed = machine.TryChangeState(GameState.Aiming);

        // ASSERT
        Assert.IsTrue(changed);
        Assert.AreEqual(GameState.Aiming, machine.CurrentState);
    }

    [Test]
    public void Aiming_CannotTransitionDirectlyToCollecting()
    {
        // ARRANGE
        var machine = new GameStateMachine();
        machine.TryChangeState(GameState.Aiming);

        // ACT
        bool changed = machine.TryChangeState(GameState.Collecting);

        // ASSERT
        Assert.IsFalse(changed);
        Assert.AreEqual(GameState.Aiming, machine.CurrentState);
    }

    [Test]
    public void Resolving_CanTransitionToCollecting()
    {
        // ARRANGE
        var machine = new GameStateMachine();
        machine.TryChangeState(GameState.Aiming);
        machine.TryChangeState(GameState.Resolving);

        // ACT
        bool changed = machine.TryChangeState(GameState.Collecting);

        // ASSERT
        Assert.IsTrue(changed);
        Assert.AreEqual(GameState.Collecting, machine.CurrentState);
    }

    [Test]
    public void Victory_CannotTransitionToAnotherState()
    {
        // ARRANGE
        var machine = new GameStateMachine();
        machine.TryChangeState(GameState.Aiming);
        machine.TryChangeState(GameState.Resolving);
        machine.TryChangeState(GameState.Collecting);
        machine.TryChangeState(GameState.Victory);

        // ACT
        bool changed = machine.TryChangeState(GameState.Aiming);

        // ASSERT
        Assert.IsFalse(changed);
        Assert.AreEqual(GameState.Victory, machine.CurrentState);
    }
    [Test]
    public void Aiming_CanTransitionToDefeat()
    {
        var machine = new GameStateMachine();
        machine.TryChangeState(GameState.Aiming);

        bool changed = machine.TryChangeState(GameState.Defeat);

        Assert.IsTrue(changed);
        Assert.AreEqual(GameState.Defeat, machine.CurrentState);
    }

    [Test]
    public void Collecting_CanTransitionToAiming()
    {
        var machine = new GameStateMachine();
        machine.TryChangeState(GameState.Aiming);
        machine.TryChangeState(GameState.Resolving);
        machine.TryChangeState(GameState.Collecting);

        bool changed = machine.TryChangeState(GameState.Aiming);

        Assert.IsTrue(changed);
        Assert.AreEqual(GameState.Aiming, machine.CurrentState);
    }

    [Test]
    public void Collecting_CanTransitionToVictory()
    {
        var machine = new GameStateMachine();
        machine.TryChangeState(GameState.Aiming);
        machine.TryChangeState(GameState.Resolving);
        machine.TryChangeState(GameState.Collecting);

        bool changed = machine.TryChangeState(GameState.Victory);

        Assert.IsTrue(changed);
        Assert.AreEqual(GameState.Victory, machine.CurrentState);
    }

    [Test]
    public void Collecting_CanTransitionToDefeat()
    {
        var machine = new GameStateMachine();
        machine.TryChangeState(GameState.Aiming);
        machine.TryChangeState(GameState.Resolving);
        machine.TryChangeState(GameState.Collecting);

        bool changed = machine.TryChangeState(GameState.Defeat);

        Assert.IsTrue(changed);
        Assert.AreEqual(GameState.Defeat, machine.CurrentState);
    }

    [Test]
    public void Defeat_CannotTransitionToAnotherState()
    {
        var machine = new GameStateMachine();
        machine.TryChangeState(GameState.Aiming);
        machine.TryChangeState(GameState.Defeat);

        bool changed = machine.TryChangeState(GameState.Aiming);

        Assert.IsFalse(changed);
        Assert.AreEqual(GameState.Defeat, machine.CurrentState);
    }
}
