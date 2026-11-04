using NUnit.Framework;
using UnityEngine;

public class GameEventsTests
{
    private bool eventReceived;

    [SetUp]
    public void SetUp()
    {
        eventReceived = false;
    }

    [TearDown]
    public void TearDown()
    {
        GameEvents.BallLaunched -= OnBallLaunched;
        GameEvents.AllBallsStopped -= OnAllBallsStopped;
        GameEvents.EffectsResolved -= OnEffectsResolved;
    }

    private void OnBallLaunched()
    {
        eventReceived = true;
    }

    private void OnAllBallsStopped()
    {
        eventReceived = true;
    }

    private void OnEffectsResolved()
    {
        eventReceived = true;
    }

    [Test]
    public void RaiseBallLaunched_NotifiesSubscribers()
    {
        GameEvents.BallLaunched += OnBallLaunched;

        GameEvents.RaiseBallLaunched();

        Assert.IsTrue(eventReceived);
    }

    [Test]
    public void RaiseAllBallsStopped_NotifiesSubscribers()
    {
        GameEvents.AllBallsStopped += OnAllBallsStopped;

        GameEvents.RaiseAllBallsStopped();

        Assert.IsTrue(eventReceived);
    }

    [Test]
    public void RaiseEffectsResolved_NotifiesSubscribers()
    {
        GameEvents.EffectsResolved += OnEffectsResolved;

        GameEvents.RaiseEffectsResolved();

        Assert.IsTrue(eventReceived);
    }
}
