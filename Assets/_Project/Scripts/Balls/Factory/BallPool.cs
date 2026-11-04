
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallPool : MonoBehaviour
{
    private BallFactory factory;
    private BallController prefab;

    private int maxBalls = 100;

    private readonly List<BallController> availableBalls =
        new List<BallController>();

    private readonly HashSet<BallController> allBalls =
        new HashSet<BallController>();

    private readonly HashSet<BallController> returningBalls =
        new HashSet<BallController>();

    public int CreatedCount => allBalls.Count;
    public int AvailableCount => availableBalls.Count;

    public void Initialize(
        BallFactory factory,
        BallController prefab,
        int maxBalls = 100)
    {
        if (factory == null || prefab == null || maxBalls <= 0)
        {
            Debug.LogError("BallPool: configuración inválida.");
            return;
        }

        this.factory = factory;
        this.prefab = prefab;
        this.maxBalls = maxBalls;
    }

    public BallController GetBall(BallData data, Vector2 position)
    {
        if (data == null || factory == null || prefab == null)
            return null;

        if (availableBalls.Count > 0)
        {
            int lastIndex = availableBalls.Count - 1;
            BallController ball = availableBalls[lastIndex];

            if (!ball.ResetForReuse(data, position))
                return null;

            availableBalls.RemoveAt(lastIndex);
            ball.gameObject.SetActive(true);

            return ball;
        }

        if (allBalls.Count >= maxBalls)
            return null;

        BallController newBall = factory.Create(
            prefab,
            data,
            position
        );

        if (newBall == null)
            return null;

        newBall.SetPool(this);
        allBalls.Add(newBall);

        return newBall;
    }

    public void ReturnBall(BallController ball)
    {
        if (ball == null || !allBalls.Contains(ball))
            return;

        if (availableBalls.Contains(ball) ||
            returningBalls.Contains(ball))
            return;

        if (!ball.Runtime.TryReturnToPool())
            return;

        returningBalls.Add(ball);

        ball.PrepareForPool();
        ball.gameObject.SetActive(false);

        StartCoroutine(WaitUntilNeutral(ball));
    }

    private IEnumerator WaitUntilNeutral(BallController ball)
    {
        while (ball != null && !ball.IsNeutral())
            yield return null;

        if (ball == null)
            yield break;

        returningBalls.Remove(ball);
        Debug.Log("BallPool: instancia disponible: " + ball.name);
        availableBalls.Add(ball);
    }
}