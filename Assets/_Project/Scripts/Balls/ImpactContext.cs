using UnityEngine;

public sealed class ImpactContext
{
    public BallRuntime Source { get; }
    public BallRuntime Target { get; }
    public float PositionX { get; }
    public float PositionY { get; }
    public float DirectionX { get; }
    public float DirectionY { get; }
    public float Strength { get; }

    public ImpactContext(
        BallRuntime source,
        BallRuntime target,
        float positionX,
        float positionY,
        float directionX,
        float directionY,
        float strength)
    {
        Source = source;
        Target = target;
        PositionX = positionX;
        PositionY = positionY;
        DirectionX = directionX;
        DirectionY = directionY;
        Strength = strength;
    }
}
