using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BallController : MonoBehaviour
{
    public BallRuntime Runtime { get; private set; }
    public bool IsMoving => rb != null &&
        rb.linearVelocity.sqrMagnitude > MovementThreshold * MovementThreshold;

    private Rigidbody2D rb;
    private InteractionResolver resolver;
    private BallEffectActivation effectActivation;
    private BallPool ownerPool;

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

        bool isMoving = IsMoving;

        if (isMoving && !wasMoving)
            GameEvents.RaiseNewMovementStarted();

        wasMoving = isMoving;
    }

    public void Initialize(BallData data)
    {
        Initialize(data, InteractionResolver.CreateDefault());
    }

    public void Initialize(BallData data, InteractionResolver interactionResolver)
    {
        if (data == null)
        {
            Debug.LogError("BallController: BallData es null.");
            return;
        }

        if (interactionResolver == null)
        {
            Debug.LogError("BallController: InteractionResolver es null.");
            return;
        }

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("BallController: falta Rigidbody2D.");
            return;
        }

        resolver = interactionResolver;
        ConfigureNewRuntime(data);
    }

    private void ConfigureNewRuntime(BallData data)
    {
        Runtime = new BallRuntime(
            Guid.NewGuid().ToString(),
            data
        );

        rb.mass = data.Mass;
        transform.localScale = Vector3.one * data.Scale;

        if (effectActivation == null)
            effectActivation = GetComponent<BallEffectActivation>();
        if (effectActivation == null)
            effectActivation = gameObject.AddComponent<BallEffectActivation>();

        effectActivation.Configure(Runtime);
    }

    public void PrepareForPool()
    {
        if (effectActivation != null)
            effectActivation.ResetForPool();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.mass = 1f;

        transform.localScale = Vector3.one;
        wasMoving = false;
    }

    public bool IsNeutral()
    {
        return effectActivation == null || effectActivation.IsNeutral;
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

        if (!IsNeutral())
        {
            Debug.LogError("BallController: la bolita aún no está limpia para reutilizarse.");
            return false;
        }

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

        if (GetInstanceID() > otherBall.GetInstanceID())
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

        List<EffectActivationRequest> requests = resolver.ResolveImpact(impact);

        foreach (EffectActivationRequest request in requests)
        {
            if (request.Owner == Runtime)
                ProcessActivationRequest(request);
            else if (request.Owner == otherBall.Runtime)
                otherBall.ProcessActivationRequest(request);
        }
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

    public bool Launch(Vector2 force, ForceMode2D forceMode)
    {
        if (Runtime == null || Runtime.State != BallRuntimeState.Active ||
            force.sqrMagnitude <= 0f)
            return false;

        rb.AddForce(force, forceMode);
        GameEvents.RaiseBallLaunched();
        return true;
    }

    public bool LaunchWithVelocity(Vector2 velocity)
    {
        if (Runtime == null || Runtime.State != BallRuntimeState.Active ||
            velocity.sqrMagnitude <= 0f)
            return false;

        rb.linearVelocity = velocity;
        GameEvents.RaiseBallLaunched();
        return true;
    }

    private void ProcessActivationRequest(
        EffectActivationRequest request)
    {
        if (request == null || request.Owner != Runtime)
            return;

        if (effectActivation == null || !effectActivation.Activate(request))
        {
            Debug.LogError(
                "BallController: no se pudo activar el efecto de " + gameObject.name,
                this
            );
        }
    }

    public void Despawn()
    {
        if (ownerPool == null)
        {
            if (effectActivation != null)
                effectActivation.Cancel();
            Destroy(gameObject);
            return;
        }

        if (Runtime.State == BallRuntimeState.PendingEffect)
        {
            if (!resolver.CanCancelPendingEffect(Runtime) ||
                !Runtime.TryCancelPendingEffect())
                return;
        }
        else if (Runtime.State != BallRuntimeState.BeingCollected &&
                 !Runtime.TryStartCollection())
        {
            return;
        }

        if (effectActivation != null)
            effectActivation.Cancel();
        ownerPool.ReturnBall(this);
    }
}
