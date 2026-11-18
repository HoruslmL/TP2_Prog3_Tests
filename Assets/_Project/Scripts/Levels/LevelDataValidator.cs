using System;
using System.Collections.Generic;
using UnityEngine;

public enum LevelDataValidationErrorCode
{
    MissingLevelData,
    MissingLevelRoot,
    NoSpawnPoints,
    NullSpawnPoint,
    InvalidSpawnPointWorldPosition,
    EmptySpawnPointId,
    DuplicateSpawnPointId,
    NoSpawnOptions,
    NullSpawnOption,
    NullBallDataAsset,
    ProbabilityOutOfRange,
    ProbabilityTotalNot100,
    NoPositiveProbabilityOption,
    MissingBallPrefab,
    MissingCircleCollider,
    UnsupportedChildCircleCollider,
    UnsupportedMultipleRootCircleColliders,
    InvalidBallScale,
    InvalidColliderRadius,
    OverlappingSpawnPoints
}

public sealed class LevelDataValidationIssue
{
    public LevelDataValidationErrorCode Code { get; }
    public int? SpawnPointIndex { get; }
    public string SpawnPointId { get; }
    public int? RelatedSpawnPointIndex { get; }
    public string RelatedSpawnPointId { get; }
    public string Message { get; }

    internal LevelDataValidationIssue(
        LevelDataValidationErrorCode code,
        int? spawnPointIndex,
        string spawnPointId,
        int? relatedSpawnPointIndex,
        string relatedSpawnPointId,
        string message)
    {
        Code = code;
        SpawnPointIndex = spawnPointIndex;
        SpawnPointId = spawnPointId;
        RelatedSpawnPointIndex = relatedSpawnPointIndex;
        RelatedSpawnPointId = relatedSpawnPointId;
        Message = message;
    }
}

public sealed class LevelDataValidationResult
{
    public IReadOnlyList<LevelDataValidationIssue> Issues { get; }
    public bool IsValid => Issues.Count == 0;

    internal LevelDataValidationResult(
        List<LevelDataValidationIssue> issues)
    {
        Issues = issues.AsReadOnly();
    }
}

public static class LevelDataValidator
{
    private const float ProbabilityTolerance = 0.01f;

    public static LevelDataValidationResult Validate(
        LevelDataAsset levelData,
        Transform levelRoot)
    {
        var issues = new List<LevelDataValidationIssue>();

        if (levelData == null)
        {
            AddIssue(
                issues,
                LevelDataValidationErrorCode.MissingLevelData,
                null,
                null,
                null,
                null,
                "No se asignó un LevelDataAsset.");
            return new LevelDataValidationResult(issues);
        }

        if (levelRoot == null)
        {
            AddIssue(
                issues,
                LevelDataValidationErrorCode.MissingLevelRoot,
                null,
                null,
                null,
                null,
                "Se debe asignar el Transform de LevelRoot para validar las posiciones.");
            return new LevelDataValidationResult(issues);
        }

        IReadOnlyList<BallSpawnPointData> spawnPoints = levelData.SpawnPoints;
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            AddIssue(
                issues,
                LevelDataValidationErrorCode.NoSpawnPoints,
                null,
                null,
                null,
                null,
                "El nivel debe tener al menos un punto de aparición.");
            return new LevelDataValidationResult(issues);
        }

        var pointIds = new HashSet<string>(StringComparer.Ordinal);
        var pointStates = new SpawnPointValidationState[spawnPoints.Count];

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            BallSpawnPointData spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {
                AddIssue(
                    issues,
                    LevelDataValidationErrorCode.NullSpawnPoint,
                    i,
                    null,
                    null,
                    null,
                    "El punto de aparición no está asignado.");
                continue;
            }

            var state = new SpawnPointValidationState(spawnPoint);
            Vector3 worldPosition = levelRoot.TransformPoint(
                new Vector3(
                    spawnPoint.LocalPosition.x,
                    spawnPoint.LocalPosition.y,
                    0f));
            state.WorldPosition = new Vector2(worldPosition.x, worldPosition.y);
            pointStates[i] = state;
            if (!IsFinite(worldPosition.x) || !IsFinite(worldPosition.y))
            {
                state.ConfigurationsResolved = false;
                AddIssue(
                    issues,
                    LevelDataValidationErrorCode.InvalidSpawnPointWorldPosition,
                    i,
                    spawnPoint.Id,
                    null,
                    null,
                    "La posición mundial del punto debe tener coordenadas X e Y finitas.");
            }

            ValidateIdentifier(spawnPoint, i, pointIds, issues);
            ValidateOptions(spawnPoint, i, state, issues);
        }

        ValidateOverlaps(pointStates, issues);
        return new LevelDataValidationResult(issues);
    }

    private static void ValidateIdentifier(
        BallSpawnPointData spawnPoint,
        int index,
        HashSet<string> pointIds,
        List<LevelDataValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(spawnPoint.Id))
        {
            AddIssue(
                issues,
                LevelDataValidationErrorCode.EmptySpawnPointId,
                index,
                spawnPoint.Id,
                null,
                null,
                "El identificador del punto de aparición no puede estar vacío.");
            return;
        }

        if (!pointIds.Add(spawnPoint.Id))
        {
            AddIssue(
                issues,
                LevelDataValidationErrorCode.DuplicateSpawnPointId,
                index,
                spawnPoint.Id,
                null,
                null,
                "El identificador '" + spawnPoint.Id + "' está repetido.");
        }
    }

    private static void ValidateOptions(
        BallSpawnPointData spawnPoint,
        int pointIndex,
        SpawnPointValidationState state,
        List<LevelDataValidationIssue> issues)
    {
        IReadOnlyList<BallSpawnOption> options = spawnPoint.SpawnOptions;
        if (options == null || options.Count == 0)
        {
            AddIssue(
                issues,
                LevelDataValidationErrorCode.NoSpawnOptions,
                pointIndex,
                spawnPoint.Id,
                null,
                null,
                "El punto debe tener al menos una opción de aparición.");
        }

        float probabilityTotal = 0f;
        if (options != null)
        {
            for (int i = 0; i < options.Count; i++)
            {
                BallSpawnOption option = options[i];
                if (option == null)
                {
                    AddIssue(
                        issues,
                        LevelDataValidationErrorCode.NullSpawnOption,
                        pointIndex,
                        spawnPoint.Id,
                        null,
                        null,
                        "El punto contiene una opción de aparición no asignada.");
                    continue;
                }

                float probability = option.SpawnProbability;
                bool finiteProbability = IsFinite(probability);
                if (!finiteProbability || probability < 0f || probability > 100f)
                {
                    AddIssue(
                        issues,
                        LevelDataValidationErrorCode.ProbabilityOutOfRange,
                        pointIndex,
                        spawnPoint.Id,
                        null,
                        null,
                        "La probabilidad de cada opción debe estar entre 0 y 100.");
                }
                else
                {
                    probabilityTotal += probability;
                }

                if (option.BallData == null)
                {
                    AddIssue(
                        issues,
                        LevelDataValidationErrorCode.NullBallDataAsset,
                        pointIndex,
                        spawnPoint.Id,
                        null,
                        null,
                        "La opción de aparición debe referenciar un BallDataAsset.");
                }

                if (probability > 0f)
                {
                    state.HasPositiveProbability = true;
                    ValidatePhysicalRadius(
                        option,
                        pointIndex,
                        spawnPoint.Id,
                        state,
                        issues);
                }
            }
        }

        if (Mathf.Abs(probabilityTotal - 100f) > ProbabilityTolerance)
        {
            AddIssue(
                issues,
                LevelDataValidationErrorCode.ProbabilityTotalNot100,
                pointIndex,
                spawnPoint.Id,
                null,
                null,
                "La suma de probabilidades del punto debe ser 100% (tolerancia: " +
                ProbabilityTolerance + ").");
        }

        if (!state.HasPositiveProbability)
        {
            AddIssue(
                issues,
                LevelDataValidationErrorCode.NoPositiveProbabilityOption,
                pointIndex,
                spawnPoint.Id,
                null,
                null,
                "El punto debe tener al menos una opción con probabilidad mayor que cero.");
        }
    }

    private static void ValidatePhysicalRadius(
        BallSpawnOption option,
        int pointIndex,
        string pointId,
        SpawnPointValidationState state,
        List<LevelDataValidationIssue> issues)
    {
        BallDataAsset ballData = option.BallData;
        if (ballData == null)
        {
            state.ConfigurationsResolved = false;
            return;
        }

        GameObject prefab = ballData.BallPrefab;
        if (prefab == null)
        {
            AddPhysicalDataIssue(
                issues,
                LevelDataValidationErrorCode.MissingBallPrefab,
                pointIndex,
                pointId,
                "El BallDataAsset no tiene un prefab de bolita asignado.");
            state.ConfigurationsResolved = false;
            return;
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

        bool hasUnsupportedColliderConfiguration = false;
        if (hasChildCollider)
        {
            AddPhysicalDataIssue(
                issues,
                LevelDataValidationErrorCode.UnsupportedChildCircleCollider,
                pointIndex,
                pointId,
                "El validador no admite CircleCollider2D en objetos hijo del prefab.");
            hasUnsupportedColliderConfiguration = true;
        }

        if (rootColliderCount > 1)
        {
            AddPhysicalDataIssue(
                issues,
                LevelDataValidationErrorCode.UnsupportedMultipleRootCircleColliders,
                pointIndex,
                pointId,
                "El validador admite un solo CircleCollider2D en la raíz del prefab.");
            hasUnsupportedColliderConfiguration = true;
        }

        if (hasUnsupportedColliderConfiguration)
        {
            state.ConfigurationsResolved = false;
            return;
        }

        if (rootColliderCount == 0)
        {
            AddPhysicalDataIssue(
                issues,
                LevelDataValidationErrorCode.MissingCircleCollider,
                pointIndex,
                pointId,
                "El prefab debe tener un CircleCollider2D en su objeto raíz.");
            state.ConfigurationsResolved = false;
            return;
        }

        CircleCollider2D collider = prefab.GetComponent<CircleCollider2D>();
        float runtimeScale = ballData.ToBallData().Scale;
        if (!IsFinite(runtimeScale) || runtimeScale <= 0f)
        {
            AddPhysicalDataIssue(
                issues,
                LevelDataValidationErrorCode.InvalidBallScale,
                pointIndex,
                pointId,
                "La escala de la bolita debe ser un valor finito mayor que cero.");
            state.ConfigurationsResolved = false;
            return;
        }

        float colliderRadius = collider.radius;
        Vector2 colliderOffset = collider.offset;
        if (!IsFinite(colliderRadius) ||
            colliderRadius < 0f ||
            !IsFinite(colliderOffset.x) ||
            !IsFinite(colliderOffset.y))
        {
            AddPhysicalDataIssue(
                issues,
                LevelDataValidationErrorCode.InvalidColliderRadius,
                pointIndex,
                pointId,
                "El radio o el offset del CircleCollider2D no son válidos.");
            state.ConfigurationsResolved = false;
            return;
        }

        // BallController reemplaza la escala raíz del prefab por BallData.Scale.
        float effectiveRadius = colliderRadius * runtimeScale;
        Vector2 effectiveCenterOffset = colliderOffset * runtimeScale;
        if (!IsFinite(effectiveRadius) ||
            !IsFinite(effectiveCenterOffset.x) ||
            !IsFinite(effectiveCenterOffset.y))
        {
            AddPhysicalDataIssue(
                issues,
                LevelDataValidationErrorCode.InvalidColliderRadius,
                pointIndex,
                pointId,
                "No se pudo determinar una configuración física finita para la bolita.");
            state.ConfigurationsResolved = false;
            return;
        }

        state.ActiveConfigurations.Add(
            new ActiveColliderConfiguration(
                effectiveCenterOffset,
                effectiveRadius));
    }

    private static void ValidateOverlaps(
        SpawnPointValidationState[] pointStates,
        List<LevelDataValidationIssue> issues)
    {
        for (int i = 0; i < pointStates.Length; i++)
        {
            SpawnPointValidationState pointA = pointStates[i];
            if (pointA == null ||
                !pointA.ConfigurationsResolved ||
                pointA.ActiveConfigurations.Count == 0)
                continue;

            for (int j = i + 1; j < pointStates.Length; j++)
            {
                SpawnPointValidationState pointB = pointStates[j];
                if (pointB == null ||
                    !pointB.ConfigurationsResolved ||
                    pointB.ActiveConfigurations.Count == 0)
                    continue;

                float minimumClearance = float.PositiveInfinity;
                for (int optionA = 0;
                     optionA < pointA.ActiveConfigurations.Count;
                     optionA++)
                {
                    ActiveColliderConfiguration configurationA =
                        pointA.ActiveConfigurations[optionA];
                    Vector2 centerA =
                        pointA.WorldPosition +
                        configurationA.CenterOffset;

                    for (int optionB = 0;
                         optionB < pointB.ActiveConfigurations.Count;
                         optionB++)
                    {
                        ActiveColliderConfiguration configurationB =
                            pointB.ActiveConfigurations[optionB];
                        Vector2 centerB =
                            pointB.WorldPosition +
                            configurationB.CenterOffset;

                        float clearance =
                            Vector2.Distance(centerA, centerB) -
                            configurationA.Radius -
                            configurationB.Radius;
                        minimumClearance =
                            Mathf.Min(minimumClearance, clearance);
                    }
                }

                if (minimumClearance >= 0f)
                    continue;

                AddIssue(
                    issues,
                    LevelDataValidationErrorCode.OverlappingSpawnPoints,
                    i,
                    pointA.SpawnPoint.Id,
                    j,
                    pointB.SpawnPoint.Id,
                    "El punto '" + pointA.SpawnPoint.Id +
                    "' se superpone con el punto '" +
                    pointB.SpawnPoint.Id + "'.");
            }
        }
    }

    private static void AddPhysicalDataIssue(
        List<LevelDataValidationIssue> issues,
        LevelDataValidationErrorCode code,
        int pointIndex,
        string pointId,
        string message)
    {
        AddIssue(
            issues,
            code,
            pointIndex,
            pointId,
            null,
            null,
            message);
    }

    private static void AddIssue(
        List<LevelDataValidationIssue> issues,
        LevelDataValidationErrorCode code,
        int? pointIndex,
        string pointId,
        int? relatedPointIndex,
        string relatedPointId,
        string message)
    {
        issues.Add(new LevelDataValidationIssue(
            code,
            pointIndex,
            pointId,
            relatedPointIndex,
            relatedPointId,
            message));
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private sealed class SpawnPointValidationState
    {
        public readonly BallSpawnPointData SpawnPoint;
        public Vector2 WorldPosition;
        public bool HasPositiveProbability;
        public bool ConfigurationsResolved = true;
        public readonly List<ActiveColliderConfiguration> ActiveConfigurations =
            new List<ActiveColliderConfiguration>();

        public SpawnPointValidationState(BallSpawnPointData spawnPoint)
        {
            SpawnPoint = spawnPoint;
        }
    }

    private struct ActiveColliderConfiguration
    {
        public readonly Vector2 CenterOffset;
        public readonly float Radius;

        public ActiveColliderConfiguration(Vector2 centerOffset, float radius)
        {
            CenterOffset = centerOffset;
            Radius = radius;
        }
    }
}
