using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BallController : MonoBehaviour
{
    public BallRuntime Runtime { get; private set; }

    private Rigidbody2D rb;
    private InteractionResolver resolver;
    private IEffectExecutor effectExecutor;
    private BallPool ownerPool;
    private Coroutine activationCoroutine;

    private bool wasMoving;
    private const float MovementThreshold = 0.05f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (Runtime == null)
            return;

        bool isMoving =
            rb.linearVelocity.sqrMagnitude >
            MovementThreshold * MovementThreshold;

        if (isMoving && !wasMoving)
        {
            if (GameFlowControllerExistsInResolving())
                GameEvents.RaiseNewMovementStarted();
        }

        wasMoving = isMoving;
    }

    public void Initialize(BallData data)
    {
        if (data == null)
        {
            Debug.LogError("BallController: BallData es null.");
            return;
        }

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("BallController: falta Rigidbody2D.");
            return;
        }

        ConfigureNewRuntime(data);
    }

    private void ConfigureNewRuntime(BallData data)
    {
        Runtime = new BallRuntime(
            Guid.NewGuid().ToString(),
            data
        );

        resolver = new InteractionResolver(
            new ExplosiveAbility()
        );

        rb.mass = data.Mass;
        transform.localScale = Vector3.one * data.Scale;

        FindEffectExecutor();
    }

    private void FindEffectExecutor()
    {
        effectExecutor = null;

        if (Runtime.Data.AbilityType != BallAbilityType.Explosive)
            return;

        ExplosiveEffectExecutor executor =
            GetComponent<ExplosiveEffectExecutor>();

        if (executor == null)
        {
            executor = gameObject.AddComponent<ExplosiveEffectExecutor>();
        }

        effectExecutor = executor;
    }

    private void RemoveEffectExecutor()
    {
        effectExecutor = null;

        MonoBehaviour[] components = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IEffectExecutor)
                Destroy(component);
        }
    }

    public void PrepareForPool()
    {
        CancelActivationCoroutine();

        effectExecutor = null;
        resolver = null;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.mass = 1f;

        transform.localScale = Vector3.one;
        wasMoving = false;

        // Eliminamos todos los ejecutores temporales.
        MonoBehaviour[] components = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IEffectExecutor)
                Destroy(component);
        }
    }

    public bool IsNeutral()
    {
        MonoBehaviour[] components = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IEffectExecutor)
                return false;
        }

        return true;
    }

    public bool ResetForReuse(BallData data, Vector2 position)
    {
        if (data == null)
        {
            Debug.LogError("BallController: no se puede reutilizar con BallData null.");
            return false;
        }

        if (Runtime == null || rb == null)
        {
            Debug.LogError("BallController: no se puede reutilizar una bolita sin inicializar.");
            return false;
        }

        if (Runtime.State != BallRuntimeState.Available)
        {
            Debug.LogError("BallController: la bolita no está disponible para reutilizarse.");
            return false;
        }

        CancelActivationCoroutine();

        // Limpiamos los componentes de la vida anterior.
        RemoveEffectExecutor();

        // Reiniciamos la física.
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.mass = 1f;

        transform.position = position;
        transform.localScale = Vector3.one;

        wasMoving = false;

        // Creamos una nueva vida lógica con los datos solicitados.
        ConfigureNewRuntime(data);

        if (!Runtime.TryActivate())
        {
            Debug.LogError("BallController: no se pudo activar la nueva instancia lógica.");
            return false;
        }

        return true;
    }

    public void SetPool(BallPool pool)
    {
        ownerPool = pool;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Runtime == null)
            return;

        BallController otherBall =
            collision.gameObject.GetComponent<BallController>();

        if (otherBall == null || otherBall.Runtime == null)
            return;

        ContactPoint2D contact = collision.GetContact(0);

        Vector2 direction =
            otherBall.transform.position - transform.position;

        if (direction.sqrMagnitude > 0f)
            direction.Normalize();

        ImpactContext impact = new ImpactContext(
            Runtime,
            otherBall.Runtime,
            contact.point.x,
            contact.point.y,
            direction.x,
            direction.y,
            collision.relativeVelocity.magnitude
        );

        ReceiveImpact(impact);

        ImpactContext reverseImpact = new ImpactContext(
            otherBall.Runtime,
            Runtime,
            contact.point.x,
            contact.point.y,
            -direction.x,
            -direction.y,
            collision.relativeVelocity.magnitude
        );

        otherBall.ReceiveImpact(reverseImpact);
    }

    public void ReceiveImpact(ImpactContext impact)
    {
        if (Runtime == null || resolver == null || impact == null)
            return;

        var requests = resolver.ResolveImpact(impact);

        foreach (EffectActivationRequest request in requests)
        {
            if (request.Owner == Runtime)
                ProcessActivationRequest(request);
        }
    }

    private void ProcessActivationRequest(
        EffectActivationRequest request)
    {
        if (request == null || request.Owner != Runtime)
            return;

        if (effectExecutor == null)
        {
            Debug.LogError(
                "BallController: no hay IEffectExecutor en " + gameObject.name,
                this
            );
            return;
        }

        activationCoroutine = StartCoroutine(
            WaitForActivation(request)
        );
    }

    private IEnumerator WaitForActivation(
        EffectActivationRequest request)
    {
        yield return new WaitForSeconds(request.Delay);

        activationCoroutine = null;

        if (request.Owner.State != BallRuntimeState.PendingEffect)
            yield break;

        if (!request.Owner.TryMarkEffectExecuted())
            yield break;

        effectExecutor.Execute(request.Owner);
    }

    public void Despawn()
    {
        CancelActivationCoroutine();

        if (ownerPool != null)
        {
            if (Runtime.State == BallRuntimeState.PendingEffect)
            {
                if (resolver == null ||
                    !resolver.CanCancelPendingEffect(Runtime))
                    return;

                if (!Runtime.TryCancelPendingEffect())
                    return;
            }
            else if (Runtime.State != BallRuntimeState.BeingCollected)
            {
                if (!Runtime.TryStartCollection())
                    return;
            }

            ownerPool.ReturnBall(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CancelActivationCoroutine()
    {
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }
    }

    private bool GameFlowControllerExistsInResolving()
    {
        GameFlowController controller =
            FindFirstObjectByType<GameFlowController>();

        return controller != null &&
               controller.CurrentState == GameState.Resolving;
    }
}
