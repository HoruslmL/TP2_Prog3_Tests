using UnityEngine;
using System.Collections;

public class BallSceneTest : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BallFactory factory;
    [SerializeField] private BallPool ballPool;
    [SerializeField] private BallSpawner ballSpawner;

    [Header("Prefab")]
    [SerializeField] private BallController ballPrefab;

    [Header("Lanzamiento")]
    [SerializeField] private float launchImpulse = 5f;
    [SerializeField] private float linearDamping = 0.5f;

    private BallController normalBall;
    private Inventory inventory;
    private bool launched;

    private void Start()
    {
        inventory = new Inventory();

        inventory.AddBall(BallAbilityType.None, 4);
        inventory.AddBall(BallAbilityType.Explosive, 5);

        ballPool.Initialize(factory, ballPrefab, 100);
        ballSpawner.Initialize(inventory);

        normalBall = ballSpawner.SpawnBall(
            BallAbilityType.None,
            new Vector2(-2f, 0f)
        );

        ballSpawner.SpawnBall(
            BallAbilityType.Explosive,
            Vector2.zero
        );

        ballSpawner.SpawnBall(
            BallAbilityType.None,
            new Vector2(3f, 0f)
        );

        ballSpawner.SpawnBall(
            BallAbilityType.Explosive,
            new Vector2(5f, 0f)
        );

        ballSpawner.SpawnBall(
            BallAbilityType.Explosive,
            new Vector2(0f, 2f)
        );

        if (normalBall == null)
        {
            Debug.LogError("BallSceneTest: no se pudo generar la bolita normal.");
            return;
        }

        normalBall.GetComponent<Rigidbody2D>().linearDamping =
            linearDamping;

        Debug.Log("Bolitas creadas. Presioná ESPACIO para lanzar.");
        Debug.Log("Inventario restante: " + inventory.BallCount);

        StartCoroutine(TestPoolReuse());
    }

    private IEnumerator TestPoolReuse()
    {
        // Creamos una bolita explosiva aislada.
        BallController explosiveBall = ballSpawner.SpawnBall(
            BallAbilityType.Explosive,
            new Vector2(20f, 0f)
        );

        if (explosiveBall == null)
        {
            Debug.LogError("No se pudo crear la bolita explosiva de prueba.");
            yield break;
        }

        yield return null;

        bool hadExplosiveExecutor =
            explosiveBall.GetComponent<ExplosiveEffectExecutor>() != null;

        Debug.Log("La bolita inicial tiene ejecutor explosivo: " + hadExplosiveExecutor);

        // La devolvemos al pool.
        explosiveBall.Despawn();

        // Esperamos a que el pool la deje disponible.
        while (ballPool.AvailableCount == 0)
            yield return null;

        // Solicitamos una bolita normal.
        BallController reusedBall = ballSpawner.SpawnBall(
            BallAbilityType.None,
            new Vector2(20f, 0f)
        );

        if (reusedBall == null)
        {
            Debug.LogError("No se pudo reutilizar la bolita.");
            yield break;
        }

        bool sameInstance = reusedBall == explosiveBall;

        bool stillHasExplosiveExecutor =
            reusedBall.GetComponent<ExplosiveEffectExecutor>() != null;

        Debug.Log("¿Se reutilizó el mismo GameObject?: " + sameInstance);
        Debug.Log("¿Conserva el ejecutor explosivo?: " + stillHasExplosiveExecutor);

        if (sameInstance && !stillHasExplosiveExecutor)
        {
            Debug.Log("PRUEBA EXITOSA: Explosiva → Normal.");

            // Devolvemos la bolita normal al pool.
            reusedBall.Despawn();

            // Esperamos a que esté disponible.
            while (ballPool.AvailableCount == 0)
                yield return null;

            // La solicitamos nuevamente como explosiva.
            BallController explosiveAgain = ballSpawner.SpawnBall(
                BallAbilityType.Explosive,
                new Vector2(20f, 0f)
            );

            if (explosiveAgain == null)
            {
                Debug.LogError("No se pudo reutilizar como explosiva.");
                yield break;
            }

            bool sameInstanceAgain = explosiveAgain == reusedBall;

            bool hasExplosiveExecutor =
                explosiveAgain.GetComponent<ExplosiveEffectExecutor>() != null;

            Debug.Log("¿Se reutilizó el mismo GameObject?: " + sameInstanceAgain);
            Debug.Log("¿Tiene ejecutor explosivo?: " + hasExplosiveExecutor);

            if (sameInstanceAgain && hasExplosiveExecutor)
            {
                Debug.Log("PRUEBA EXITOSA: Normal → Explosiva.");

                // Alteramos la física para simular una vida anterior.
                Rigidbody2D rb = explosiveAgain.GetComponent<Rigidbody2D>();

                rb.linearVelocity = new Vector2(10f, 5f);
                rb.angularVelocity = 180f;
                rb.mass = 99f;

                explosiveAgain.transform.localScale = Vector3.one * 5f;

                // La devolvemos al pool.
                explosiveAgain.Despawn();

                while (ballPool.AvailableCount == 0)
                    yield return null;

                // La solicitamos nuevamente como Normal.
                BallController normalAgain = ballSpawner.SpawnBall(
                    BallAbilityType.None,
                    new Vector2(20f, 0f)
                );

                if (normalAgain == null)
                {
                    Debug.LogError("No se pudo reutilizar como Normal para comprobar el reinicio.");
                    yield break;
                }

                Rigidbody2D newRb = normalAgain.GetComponent<Rigidbody2D>();

                bool velocityReset = newRb.linearVelocity == Vector2.zero;
                bool angularVelocityReset = newRb.angularVelocity == 0f;

                bool massReset = Mathf.Approximately(
                    newRb.mass,
                    normalAgain.Runtime.Data.Mass
                );

                bool scaleReset = Vector3.Distance(
                    normalAgain.transform.localScale,
                    Vector3.one * normalAgain.Runtime.Data.Scale
                ) < 0.001f;

                bool stateReset =
                    normalAgain.Runtime.State == BallRuntimeState.Active;

                bool executorRemoved =
                    normalAgain.GetComponent<ExplosiveEffectExecutor>() == null;

                Debug.Log("Velocidad reiniciada: " + velocityReset);
                Debug.Log("Velocidad angular reiniciada: " + angularVelocityReset);
                Debug.Log("Masa reiniciada: " + massReset);
                Debug.Log("Escala reiniciada: " + scaleReset);
                Debug.Log("Estado lógico reiniciado: " + stateReset);
                Debug.Log("Ejecutor anterior eliminado: " + executorRemoved);

                if (velocityReset && angularVelocityReset &&
                    massReset && scaleReset && stateReset && executorRemoved)
                {
                    Debug.Log("PRUEBA EXITOSA: reinicio completo del ciclo de vida.");
                }
                else
                {
                    Debug.LogError("PRUEBA FALLIDA: algún dato no se reinició correctamente.");
                }
            }
            else
            {
                Debug.LogError("PRUEBA FALLIDA: Normal → Explosiva.");
            }
        }
        else
        {
            Debug.LogError("PRUEBA FALLIDA: Explosiva → Normal.");
        }
    }

    private void Update()
    {
        if (launched || normalBall == null)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Rigidbody2D rb = normalBall.GetComponent<Rigidbody2D>();

            rb.AddForce(
                Vector2.right * launchImpulse,
                ForceMode2D.Impulse
            );

            launched = true;
            Debug.Log("¡Lanzamiento!");
        }
    }
}
