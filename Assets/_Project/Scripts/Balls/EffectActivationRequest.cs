using UnityEngine;

public sealed class EffectActivationRequest
{
    public BallRuntime Owner { get; }
    public float Delay { get; }
    public BallAbilityType AbilityType { get; }

    public EffectActivationRequest(BallRuntime owner)
    {
        if (owner == null)
            throw new System.ArgumentNullException(nameof(owner));

        Owner = owner;
        Delay = owner.Data.EffectDelay;
        AbilityType = owner.Data.AbilityType;
    }
}
