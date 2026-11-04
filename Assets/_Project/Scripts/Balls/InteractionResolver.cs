using System.Collections.Generic;

public sealed class InteractionResolver
{
    private readonly Dictionary<BallAbilityType, IBallAbility> abilities;

    public InteractionResolver(params IBallAbility[] availableAbilities)
    {
        abilities = new Dictionary<BallAbilityType, IBallAbility>();

        foreach (var ability in availableAbilities)
        {
            abilities.Add(ability.AbilityType, ability);
        }
    }

    public List<EffectActivationRequest> ResolveImpact(ImpactContext impact)
    {
        List<EffectActivationRequest> requests =
            new List<EffectActivationRequest>();

        if (impact == null ||
            impact.Source == null ||
            impact.Target == null)
        {
            return requests;
        }

        TryActivateAbility(impact.Source, impact, requests);
        TryActivateAbility(impact.Target, impact, requests);

        return requests;
    }

    public bool CanCancelPendingEffect(BallRuntime owner)
    {
        if (owner == null)
            return false;

        BallAbilityType type = owner.Data.AbilityType;

        if (type == BallAbilityType.None)
            return false;

        if (!abilities.TryGetValue(type, out IBallAbility ability))
            return false;

        return ability.CanCancelPendingEffect(owner);
    }

    private void TryActivateAbility(
        BallRuntime owner,
        ImpactContext impact,
        List<EffectActivationRequest> requests)
    {
        BallAbilityType type = owner.Data.AbilityType;

        if (type == BallAbilityType.None)
            return;

        if (!abilities.TryGetValue(type, out IBallAbility ability))
            return;

        bool activated = ability.TryActivate(owner, impact);

        if (!activated)
            return;

        requests.Add(new EffectActivationRequest(owner));
    }
}
