using System.Collections.Generic;

public class Inventory
{
    private readonly Dictionary<BallAbilityType, int> ballCounts =
        new Dictionary<BallAbilityType, int>();

    private const int MaxCapacity = 100;

    public int BallCount
    {
        get
        {
            int total = 0;

            foreach (int amount in ballCounts.Values)
                total += amount;

            return total;
        }
    }

    public bool AddBall(BallAbilityType type, int amount = 1)
    {
        if (amount <= 0 || BallCount + amount > MaxCapacity)
            return false;

        if (!ballCounts.ContainsKey(type))
            ballCounts[type] = 0;

        ballCounts[type] += amount;

        return true;
    }

    public bool TryUseBall(BallAbilityType type)
    {
        if (!HasBall(type))
            return false;

        ballCounts[type]--;

        return true;
    }

    public int GetBallCount(BallAbilityType type)
    {
        return ballCounts.TryGetValue(type, out int amount)
            ? amount
            : 0;
    }

    public bool HasBall(BallAbilityType type)
    {
        return GetBallCount(type) > 0;
    }
}
