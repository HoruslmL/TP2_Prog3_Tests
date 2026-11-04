using NUnit.Framework;
using UnityEngine;

public class InventoryTests
{
    private Inventory inventory;

    [SetUp]
    public void SetUp()
    {
        inventory = new Inventory();
    }

    [Test]
    public void AddBall_IncreasesQuantity()
    {
        inventory.AddBall(BallAbilityType.None, 3);

        Assert.AreEqual(3, inventory.GetBallCount(BallAbilityType.None));
        Assert.AreEqual(3, inventory.BallCount);
    }

    [Test]
    public void TryUseBall_DecreasesQuantity()
    {
        inventory.AddBall(BallAbilityType.Explosive, 2);

        bool result = inventory.TryUseBall(BallAbilityType.Explosive);

        Assert.IsTrue(result);
        Assert.AreEqual(1, inventory.GetBallCount(BallAbilityType.Explosive));
    }

    [Test]
    public void TryUseBall_WhenInventoryIsEmpty_ReturnsFalse()
    {
        bool result = inventory.TryUseBall(BallAbilityType.None);

        Assert.IsFalse(result);
        Assert.AreEqual(0, inventory.BallCount);
    }


    [Test]
    public void HasBall_ReturnsCorrectAvailability()
    {
        inventory.AddBall(BallAbilityType.Explosive, 2);

        Assert.IsTrue(inventory.HasBall(BallAbilityType.Explosive));
        Assert.IsFalse(inventory.HasBall(BallAbilityType.None));
    }
}
