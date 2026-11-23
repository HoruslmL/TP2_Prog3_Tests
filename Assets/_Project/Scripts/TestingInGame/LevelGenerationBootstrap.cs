using System.Collections;
using UnityEngine;

public class LevelGenerationBootstrap : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BallPool ballPool;
    [SerializeField] private BallFactory factory;
    [SerializeField] private BallController ballPrefab;
    [SerializeField] private LevelBallSpawner levelBallSpawner;

    private IEnumerator Start()
    {
        if (ballPool == null)
        {
            Debug.LogError(
                "LevelGenerationBootstrap: falta asignar BallPool.",
                this);
            yield break;
        }

        if (factory == null)
        {
            Debug.LogError(
                "LevelGenerationBootstrap: falta asignar BallFactory.",
                this);
            yield break;
        }

        if (ballPrefab == null)
        {
            Debug.LogError(
                "LevelGenerationBootstrap: falta asignar el prefab de bolita.",
                this);
            yield break;
        }

        if (levelBallSpawner == null)
        {
            Debug.LogError(
                "LevelGenerationBootstrap: falta asignar LevelBallSpawner.",
                this);
            yield break;
        }

        ballPool.Initialize(factory, ballPrefab, 100);

        yield return null;

        if (levelBallSpawner == null)
        {
            Debug.LogError(
                "LevelGenerationBootstrap: LevelBallSpawner dejó de estar disponible antes de generar el nivel.",
                this);
            yield break;
        }

        if (levelBallSpawner.TrySpawnLevel(out string error))
        {
            Debug.Log("Nivel generado correctamente.");
        }
        else
        {
            Debug.LogError($"No se pudo generar el nivel: {error}");
        }
    }
}
