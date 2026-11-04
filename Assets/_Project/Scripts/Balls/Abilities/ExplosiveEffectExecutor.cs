using System.Collections.Generic;
using UnityEngine;

public class ExplosiveEffectExecutor : MonoBehaviour, IEffectExecutor
{
    [SerializeField, Min(0f)]
    private float explosionForce = 5f;

    public void Execute(BallRuntime owner)
    {
        if (owner == null)
            return;

        Vector2 explosionPosition = transform.position;
        float radius = owner.Data.EffectRadius;

        Collider2D[] affectedColliders =
            Physics2D.OverlapCircleAll(explosionPosition, radius);

        // Evita aplicar fuerza más de una vez
        // si una bolita tiene varios colliders.
        HashSet<Rigidbody2D> affectedBodies =
            new HashSet<Rigidbody2D>();

        foreach (Collider2D affectedCollider in affectedColliders)
        {
            BallController affectedBall =
                affectedCollider.GetComponentInParent<BallController>();

            // Ignoramos objetos que no sean bolitas.
            if (affectedBall == null)
                continue;

            // Ignoramos la propia bolita explosiva.
            if (affectedBall == GetComponent<BallController>())
                continue;

            Rigidbody2D affectedRb =
                affectedCollider.attachedRigidbody;

            if (affectedRb == null)
                continue;

            // Evitamos afectar dos veces al mismo Rigidbody.
            if (!affectedBodies.Add(affectedRb))
                continue;

            Vector2 direction =
                affectedRb.position - explosionPosition;

            // Evitamos una dirección nula.
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.up;
            else
                direction.Normalize();

            // Creamos un impacto lógico provocado por la explosión.
            ImpactContext impact = new ImpactContext(
                owner,
                affectedBall.Runtime,
                explosionPosition.x,
                explosionPosition.y,
                direction.x,
                direction.y,
                explosionForce
            );

            // La bolita afectada procesa el impacto.
            affectedBall.ReceiveImpact(impact);

            // Aplicamos el impulso físico.
            affectedRb.AddForce(
                direction * explosionForce,
                ForceMode2D.Impulse
            );
        }

        Debug.Log(
            $"¡EXPLOSIÓN! Bolita: {owner.Data.Id}. " +
            $"Posición: {explosionPosition}. Radio: {radius}",
            this
        );

        // Destruimos la bolita después de ejecutar la explosión.
        BallController ownerController = GetComponent<BallController>();

        if (ownerController != null)
        {
            ownerController.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
