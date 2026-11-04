using UnityEngine;
using System;

public class GameEvents
{
    public static event Action BallLaunched;
    public static event Action AllBallsStopped;
    public static event Action EffectsResolved;

    public static event Action NewMovementStarted;

    public static void RaiseBallLaunched()
    {
        BallLaunched?.Invoke();
    }

    public static void RaiseAllBallsStopped()
    {
        AllBallsStopped?.Invoke();
    }

    public static void RaiseEffectsResolved()
    {
        EffectsResolved?.Invoke();
    }

    public static void RaiseNewMovementStarted()
    {
        NewMovementStarted?.Invoke();
    }
}
