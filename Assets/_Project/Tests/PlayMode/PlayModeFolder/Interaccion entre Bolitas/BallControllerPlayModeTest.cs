using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BallControllerPlayModeTest
{
    [UnityTest]
    public IEnumerator BallController_InitializesRuntimeFromBallData()
    {
        BallData data = new BallData(
            "TestBall",
            2f,
            BallAbilityType.None
        );

        GameObject ballObject = new GameObject("TestBall");

        ballObject.AddComponent<Rigidbody2D>();
        ballObject.AddComponent<CircleCollider2D>();

        BallController controller =
            ballObject.AddComponent<BallController>();

        controller.Initialize(data);

        yield return null;

        Assert.IsNotNull(controller.Runtime);
        Assert.AreEqual("TestBall", controller.Runtime.Data.Id);
        Assert.AreEqual(2f, controller.Runtime.Data.Mass);

        Assert.AreEqual(
            BallRuntimeState.Available,
            controller.Runtime.State
        );

        Assert.AreEqual(
            2f,
            ballObject.GetComponent<Rigidbody2D>().mass
        );

        Object.Destroy(ballObject);

        yield return null;
    }
}
