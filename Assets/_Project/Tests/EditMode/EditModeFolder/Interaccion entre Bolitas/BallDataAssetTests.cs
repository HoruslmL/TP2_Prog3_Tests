using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class BallDataAssetTests
{
    [Test]
    public void ToBallData_ConvertsExplosiveConfigurationCorrectly()
    {
        var asset = ScriptableObject.CreateInstance<BallDataAsset>();

        var serializedAsset = new SerializedObject(asset);
        serializedAsset.FindProperty("id").stringValue = "Explosive";
        serializedAsset.FindProperty("mass").floatValue = 2f;
        serializedAsset.FindProperty("abilityType").enumValueIndex =
            (int)BallAbilityType.Explosive;
        serializedAsset.FindProperty("effectRadius").floatValue = 3f;
        serializedAsset.FindProperty("effectDelay").floatValue = 1f;
        serializedAsset.ApplyModifiedPropertiesWithoutUndo();

        BallData data = asset.ToBallData();

        Assert.That(data.Id, Is.EqualTo("Explosive"));
        Assert.That(data.Mass, Is.EqualTo(2f));
        Assert.That(data.AbilityType, Is.EqualTo(BallAbilityType.Explosive));
        Assert.That(data.EffectRadius, Is.EqualTo(3f));
        Assert.That(data.EffectDelay, Is.EqualTo(1f));

        Object.DestroyImmediate(asset);
    }
}
