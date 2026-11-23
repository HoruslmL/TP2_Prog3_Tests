using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum BallColliderGeometryIssue
{
    None = 0,
    MissingBallDataAsset = 1 << 0,
    MissingBallPrefab = 1 << 1,
    UnsupportedChildCircleCollider = 1 << 2,
    UnsupportedMultipleRootCircleColliders = 1 << 3,
    MissingCircleCollider = 1 << 4,
    InvalidBallScale = 1 << 5,
    InvalidColliderRadius = 1 << 6,
    InvalidEffectiveGeometry = 1 << 7
}

public static class BallColliderGeometry
{
    public static bool TryGetEffectiveGeometry(
        BallDataAsset ballData,
        out float effectiveRadius,
        out Vector2 effectiveCenterOffset,
        out BallColliderGeometryIssue issues)
    {
        effectiveRadius = 0f;
        effectiveCenterOffset = Vector2.zero;
        issues = BallColliderGeometryIssue.None;

        if (ballData == null)
        {
            issues = BallColliderGeometryIssue.MissingBallDataAsset;
            return false;
        }

        GameObject prefab = ballData.BallPrefab;
        if (prefab == null)
        {
            issues = BallColliderGeometryIssue.MissingBallPrefab;
            return false;
        }

        CircleCollider2D[] colliders =
            prefab.GetComponentsInChildren<CircleCollider2D>(true);
        int rootColliderCount = 0;
        bool hasChildCollider = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
                continue;

            if (colliders[i].transform == prefab.transform)
                rootColliderCount++;
            else
                hasChildCollider = true;
        }

        if (hasChildCollider)
        {
            issues |=
                BallColliderGeometryIssue.UnsupportedChildCircleCollider;
        }

        if (rootColliderCount > 1)
        {
            issues |=
                BallColliderGeometryIssue.UnsupportedMultipleRootCircleColliders;
        }

        if (issues != BallColliderGeometryIssue.None)
            return false;

        if (rootColliderCount == 0)
        {
            issues = BallColliderGeometryIssue.MissingCircleCollider;
            return false;
        }

        CircleCollider2D collider = prefab.GetComponent<CircleCollider2D>();
        float runtimeScale = ballData.ToBallData().Scale;
        if (!IsFinite(runtimeScale) || runtimeScale <= 0f)
        {
            issues = BallColliderGeometryIssue.InvalidBallScale;
            return false;
        }

        float colliderRadius = collider.radius;
        Vector2 colliderOffset = collider.offset;
        if (!IsFinite(colliderRadius) ||
            colliderRadius < 0f ||
            !IsFinite(colliderOffset.x) ||
            !IsFinite(colliderOffset.y))
        {
            issues = BallColliderGeometryIssue.InvalidColliderRadius;
            return false;
        }

        effectiveRadius = colliderRadius * runtimeScale;
        effectiveCenterOffset = colliderOffset * runtimeScale;
        if (!IsFinite(effectiveRadius) ||
            !IsFinite(effectiveCenterOffset.x) ||
            !IsFinite(effectiveCenterOffset.y))
        {
            effectiveRadius = 0f;
            effectiveCenterOffset = Vector2.zero;
            issues = BallColliderGeometryIssue.InvalidEffectiveGeometry;
            return false;
        }

        return true;
    }

    public static bool TryGetMaximumSelectableGeometry(
        BallSpawnPointData spawnPoint,
        out float maximumRadius,
        out Vector2 maximumCenterOffset)
    {
        maximumRadius = 0f;
        maximumCenterOffset = Vector2.zero;

        if (spawnPoint == null ||
            spawnPoint.SpawnOptions == null ||
            spawnPoint.SpawnOptions.Count == 0)
            return false;

        bool hasPositiveOption = false;
        float computedMaximumRadius = 0f;
        Vector2 computedCenterOffset = Vector2.zero;
        IReadOnlyList<BallSpawnOption> options = spawnPoint.SpawnOptions;
        for (int i = 0; i < options.Count; i++)
        {
            BallSpawnOption option = options[i];
            if (option == null)
                return false;

            float probability = option.SpawnProbability;
            if (!IsFinite(probability) || probability < 0f || probability > 100f)
                return false;

            if (probability <= 0f)
                continue;

            if (option.BallData == null)
                return false;

            if (!TryGetEffectiveGeometry(
                    option.BallData,
                    out float radius,
                    out Vector2 centerOffset,
                    out _))
                return false;

            if (!hasPositiveOption || radius > computedMaximumRadius)
            {
                computedMaximumRadius = radius;
                computedCenterOffset = centerOffset;
            }

            hasPositiveOption = true;
        }

        if (!hasPositiveOption)
            return false;

        maximumRadius = computedMaximumRadius;
        maximumCenterOffset = computedCenterOffset;
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
