using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BallCollisionPlayModeTest
{
    [UnityTest]
    public IEnumerator NormalBallHitsExplosiveBall_ActivatesExplosion()
    {
        BallData normalData = new BallData(
            "Normal",
            1f,
            BallAbilityType.None
        );

        BallData explosiveData = new BallData(
            "Explosive",
            1f,
            BallAbilityType.Explosive,
            3f,
            1f
        );

        BallController normalBall = CreateBall(
            "NormalBall",
            normalData,
            new Vector2(-2f, 0f)
        );

        BallController explosiveBall = CreateBall(
            "ExplosiveBall",
            explosiveData,
            Vector2.zero
        );

        Rigidbody2D normalRb =
            normalBall.GetComponent<Rigidbody2D>();

        Rigidbody2D explosiveRb =
            explosiveBall.GetComponent<Rigidbody2D>();

        normalRb.gravityScale = 0f;
        explosiveRb.gravityScale = 0f;

        normalBall.gameObject.SetActive(true);
        explosiveBall.gameObject.SetActive(true);

        Assert.IsTrue(normalBall.Runtime.TryActivate());
        Assert.IsTrue(explosiveBall.Runtime.TryActivate());

        normalRb.linearVelocity = Vector2.right * 5f;

        for (int i = 0; i < 120; i++)
        {
            if (explosiveBall.Runtime.State ==
                BallRuntimeState.PendingEffect)
            {
                break;
            }

            yield return new WaitForFixedUpdate();
        }

        bool explosionActivated =
            explosiveBall.Runtime.State ==
            BallRuntimeState.PendingEffect;

        BallRuntimeState normalState = normalBall.Runtime.State;

        Object.Destroy(normalBall.gameObject);
        Object.Destroy(explosiveBall.gameObject);

        yield return null;

        Assert.IsTrue(
            explosionActivated,
            "La colisión debería activar la habilidad explosiva."
        );

        Assert.AreEqual(
            BallRuntimeState.Active,
            normalState,
            "La bolita normal debería continuar activa."
        );
    }

    private BallController CreateBall(
        string objectName,
        BallData data,
        Vector2 position)
    {
        GameObject ballObject = new GameObject(objectName);

        ballObject.SetActive(false);
        ballObject.transform.position = position;

        ballObject.AddComponent<Rigidbody2D>();
        ballObject.AddComponent<CircleCollider2D>();

        BallController controller =
            ballObject.AddComponent<BallController>();

        controller.Initialize(data);

        return controller;
    }
}
