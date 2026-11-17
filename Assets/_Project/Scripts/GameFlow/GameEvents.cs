using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action BallLaunched;
    public static event Action AllBallsStopped;
    public static event Action EffectsResolved;

    public static event Action NewMovementStarted;

    public static void RaiseBallLaunched()
    {
        Debug.Log("GameEvents: BallLaunched");
        BallLaunched?.Invoke();
    }

    public static void RaiseAllBallsStopped()
    {
        Debug.Log("GameEvents: AllBallsStopped");
        AllBallsStopped?.Invoke();
    }

    public static void RaiseEffectsResolved()
    {
        Debug.Log("GameEvents: EffectsResolved");
        EffectsResolved?.Invoke();
    }

    public static void RaiseNewMovementStarted()
    {
        Debug.Log("GameEvents: NewMovementStarted");
        NewMovementStarted?.Invoke();
    }
}
