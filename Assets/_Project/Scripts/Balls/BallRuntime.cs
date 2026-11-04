using UnityEngine;

public enum BallRuntimeState
{
    Available,
    Active,
    PendingEffect,
    EffectExecuted,
    BeingCollected
}

public sealed class BallRuntime
{
    public string InstanceId { get; }
    public BallData Data { get; }
    public BallRuntimeState State { get; private set; }

    public BallRuntime(string instanceId, BallData data)
    {
        InstanceId = instanceId;
        Data = data;
        State = BallRuntimeState.Available;
    }

    public bool TryActivate()
    {
        if (State != BallRuntimeState.Available)
            return false;

        State = BallRuntimeState.Active;
        return true;
    }

    public bool TryScheduleEffect()
    {
        if (State != BallRuntimeState.Active)
            return false;

        if (Data.AbilityType != BallAbilityType.Explosive)
            return false;

        State = BallRuntimeState.PendingEffect;
        return true;
    }

    public bool TryMarkEffectExecuted()
    {
        if (State != BallRuntimeState.PendingEffect)
            return false;

        State = BallRuntimeState.EffectExecuted;
        return true;
    }

    public bool TryResumeActive()
    {
        if (State != BallRuntimeState.EffectExecuted)
            return false;

        State = BallRuntimeState.Active;
        return true;
    }

    public bool TryStartCollection()
    {
        if (State != BallRuntimeState.Active &&
            State != BallRuntimeState.EffectExecuted)
            return false;

        State = BallRuntimeState.BeingCollected;
        return true;
    }

    public bool TryCancelPendingEffect()
    {
        if (State != BallRuntimeState.PendingEffect)
            return false;

        State = BallRuntimeState.BeingCollected;
        return true;
    }

    public bool TryReturnToPool()
    {
        if (State != BallRuntimeState.BeingCollected &&
            State != BallRuntimeState.EffectExecuted)
            return false;

        State = BallRuntimeState.Available;
        return true;
    }
}
