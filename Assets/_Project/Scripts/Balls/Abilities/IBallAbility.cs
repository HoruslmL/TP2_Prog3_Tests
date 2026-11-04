using UnityEngine;

public interface IBallAbility
{
    BallAbilityType AbilityType { get; }

    bool TryActivate(BallRuntime owner, ImpactContext impact);

    bool CanCancelPendingEffect(BallRuntime owner);
}
