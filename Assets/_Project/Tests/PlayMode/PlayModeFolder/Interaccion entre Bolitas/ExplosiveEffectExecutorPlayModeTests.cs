using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ExplosiveEffectExecutorPlayModeTests
{
    [UnityTest]
    public IEnumerator Explosion_AffectsOnlyNearbyBallsAndDestroysOwner()
    {
        BallData explosiveData = new BallData(
            "Explosive",
            1f,
            BallAbilityType.Explosive,
            3f,
            1f
        );

        BallData normalData = new BallData(
            "Normal",
            1f,
            BallAbilityType.None
        );

        BallController explosiveBall = CreateBall(
            "ExplosiveBall",
            explosiveData,
            Vector2.zero
        );

        BallController nearbyBall = CreateBall(
            "NearbyBall",
            normalData,
            Vector2.right
        );

        BallController distantBall = CreateBall(
            "DistantBall",
            normalData,
            new Vector2(5f, 0f)
        );

        Rigidbody2D explosiveRb =
            explosiveBall.GetComponent<Rigidbody2D>();

        Rigidbody2D nearbyRb =
            nearbyBall.GetComponent<Rigidbody2D>();

        Rigidbody2D distantRb =
            distantBall.GetComponent<Rigidbody2D>();

        explosiveBall.gameObject.SetActive(true);
        nearbyBall.gameObject.SetActive(true);
        distantBall.gameObject.SetActive(true);

        explosiveRb.gravityScale = 0f;
        nearbyRb.gravityScale = 0f;
        distantRb.gravityScale = 0f;

        yield return null;

        Assert.IsTrue(
            explosiveBall.Runtime.TryActivate(),
            "La bolita explosiva debería activarse."
        );

        Assert.IsTrue(
            explosiveBall.Runtime.TryScheduleEffect(),
            "La bolita debería programar su explosión."
        );

        Assert.IsTrue(
            explosiveBall.Runtime.TryMarkEffectExecuted(),
            "La bolita debería marcar su efecto como ejecutado."
        );

        explosiveBall.GetComponent<ExplosiveEffectExecutor>()
            .Execute(explosiveBall.Runtime);

        bool ownerDidNotReceiveImpulse =
            explosiveRb.linearVelocity.magnitude < 0.01f;

        yield return new WaitForFixedUpdate();

        bool nearbyReceivedImpulse =
            nearbyRb.linearVelocity.magnitude > 0.1f;

        bool distantRemainedStill =
            distantRb.linearVelocity.magnitude < 0.01f;

        bool ownerWasDestroyed = explosiveBall == null;

        Object.Destroy(nearbyBall.gameObject);
        Object.Destroy(distantBall.gameObject);

        yield return null;

        Assert.IsTrue(
            nearbyReceivedImpulse,
            "La bolita cercana debería recibir un impulso."
        );

        Assert.IsTrue(
            distantRemainedStill,
            "La bolita fuera del radio no debería recibir fuerza."
        );

        Assert.IsTrue(
            ownerDidNotReceiveImpulse,
            "La bolita explosiva no debería afectarse a sí misma."
        );

        Assert.IsTrue(
            ownerWasDestroyed,
            "La bolita explosiva debería retirarse después de ejecutar su efecto."
        );
    }

    [UnityTest]
    public IEnumerator Explosion_ActivatesNearbyExplosiveBall()
    {
        BallData firstData = new BallData(
            "ExplosiveA",
            1f,
            BallAbilityType.Explosive,
            3f,
            1f
        );

        BallData secondData = new BallData(
            "ExplosiveB",
            1f,
            BallAbilityType.Explosive,
            3f,
            1f
        );

        BallController firstBall = CreateBall(
            "ExplosiveA",
            firstData,
            Vector2.zero
        );

        BallController secondBall = CreateBall(
            "ExplosiveB",
            secondData,
            new Vector2(2f, 0f)
        );

        firstBall.gameObject.SetActive(true);
        secondBall.gameObject.SetActive(true);

        Assert.IsTrue(firstBall.Runtime.TryActivate());
        Assert.IsTrue(secondBall.Runtime.TryActivate());

        firstBall.GetComponent<Rigidbody2D>().gravityScale = 0f;
        secondBall.GetComponent<Rigidbody2D>().gravityScale = 0f;

        yield return null;

        Assert.IsTrue(firstBall.Runtime.TryScheduleEffect());
        Assert.IsTrue(firstBall.Runtime.TryMarkEffectExecuted());

        firstBall.GetComponent<ExplosiveEffectExecutor>()
            .Execute(firstBall.Runtime);

        bool chainReactionActivated =
            secondBall.Runtime.State ==
            BallRuntimeState.PendingEffect;

        Object.Destroy(secondBall.gameObject);

        yield return null;

        Assert.IsTrue(
            chainReactionActivated,
            "La explosión debería activar otra bolita explosiva dentro del radio."
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
        ballObject.AddComponent<ExplosiveEffectExecutor>();

        BallController controller =
            ballObject.AddComponent<BallController>();

        controller.Initialize(data);

        return controller;
    }
}
