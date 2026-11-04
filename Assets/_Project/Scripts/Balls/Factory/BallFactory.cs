using UnityEngine;

public class BallFactory : MonoBehaviour
{
    public BallController Create(
        BallController prefab,
        BallData data,
        Vector2 position)
    {
        if (prefab == null)
        {
            Debug.LogError("BallFactory: falta asignar el prefab.");
            return null;
        }

        if (data == null)
        {
            Debug.LogError("BallFactory: BallData es null.");
            return null;
        }

        if (prefab.gameObject.activeSelf)
        {
            Debug.LogError("BallFactory: el prefab debe estar desactivado.");
            return null;
        }

        BallController newBall = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        newBall.Initialize(data);

        if (newBall.Runtime == null || !newBall.Runtime.TryActivate())
        {
            Debug.LogError("BallFactory: no se pudo inicializar o activar la bolita.");
            Destroy(newBall.gameObject);
            return null;
        }

        newBall.gameObject.SetActive(true);

        return newBall;
    }
}