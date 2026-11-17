using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallEffectActivation : MonoBehaviour
{
    private static readonly Dictionary<BallAbilityType, System.Type> ExecutorTypes =
        new Dictionary<BallAbilityType, System.Type>
        {
            { BallAbilityType.Explosive, typeof(ExplosiveEffectExecutor) }
        };

    private BallController owner;
    private MonoBehaviour executorComponent;
    private IEffectExecutor executor;
    private Coroutine activationCoroutine;

    public bool IsNeutral => executorComponent == null;

    private void Awake()
    {
        owner = GetComponent<BallController>();
    }

    public void Configure(BallRuntime runtime)
    {
        Cancel();
        RemoveExecutor();

        if (runtime == null ||
            !ExecutorTypes.TryGetValue(runtime.Data.AbilityType, out System.Type executorType))
            return;

        executorComponent = GetComponent(executorType) as MonoBehaviour;
        if (executorComponent == null)
            executorComponent = gameObject.AddComponent(executorType) as MonoBehaviour;

        executor = executorComponent as IEffectExecutor;
        if (executor == null)
            RemoveExecutor();
    }

    public bool Activate(EffectActivationRequest request)
    {
        if (request == null || owner == null || request.Owner != owner.Runtime ||
            request.Owner.State != BallRuntimeState.PendingEffect ||
            request.AbilityType != request.Owner.Data.AbilityType ||
            executor == null || activationCoroutine != null)
            return false;

        activationCoroutine = StartCoroutine(WaitAndExecute(request));
        return true;
    }

    public void Cancel()
    {
        if (activationCoroutine == null)
            return;

        StopCoroutine(activationCoroutine);
        activationCoroutine = null;
    }

    public void ResetForPool()
    {
        Cancel();
        RemoveExecutor();
    }

    private IEnumerator WaitAndExecute(EffectActivationRequest request)
    {
        yield return new WaitForSeconds(request.Delay);
        activationCoroutine = null;

        BallRuntime runtime = request.Owner;
        IEffectExecutor currentExecutor = executor;

        if (runtime.State != BallRuntimeState.PendingEffect ||
            !runtime.TryMarkEffectExecuted())
            yield break;

        currentExecutor.Execute(runtime);

        if (owner != null && runtime.State == BallRuntimeState.EffectExecuted)
            owner.Despawn();
    }

    private void RemoveExecutor()
    {
        executor = null;

        if (executorComponent != null)
            Destroy(executorComponent);

        executorComponent = null;
    }
}