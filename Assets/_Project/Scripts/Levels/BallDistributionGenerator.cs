using System;
using System.Collections.Generic;
using UnityEngine;

public enum BallDistributionErrorCode
{
    MissingLevelData,
    NoSpawnPoints,
    NullSpawnPoint,
    NoSpawnOptions,
    NullSpawnOption,
    NullBallDataAsset,
    InvalidProbability,
    NoSelectableOptions
}

public sealed class BallDistributionError
{
    public BallDistributionErrorCode Code { get; }
    public int? SpawnPointIndex { get; }
    public string SpawnPointId { get; }
    public string Message { get; }

    internal BallDistributionError(
        BallDistributionErrorCode code,
        int? spawnPointIndex,
        string spawnPointId,
        string message)
    {
        Code = code;
        SpawnPointIndex = spawnPointIndex;
        SpawnPointId = spawnPointId;
        Message = message;
    }
}

public sealed class BallDistributionEntry
{
    public int SpawnPointIndex { get; }
    public string SpawnPointId { get; }
    public Vector2 LocalPosition { get; }
    public BallSpawnOption SelectedOption { get; }
    public BallDataAsset BallData => SelectedOption.BallData;

    internal BallDistributionEntry(
        int spawnPointIndex,
        BallSpawnPointData spawnPoint,
        BallSpawnOption selectedOption)
    {
        SpawnPointIndex = spawnPointIndex;
        SpawnPointId = spawnPoint.Id;
        LocalPosition = spawnPoint.LocalPosition;
        SelectedOption = selectedOption;
    }
}

public sealed class BallDistributionResult
{
    private static readonly IReadOnlyList<BallDistributionEntry> EmptyEntries =
        new List<BallDistributionEntry>().AsReadOnly();

    public bool IsSuccess { get; }
    public IReadOnlyList<BallDistributionEntry> Entries { get; }
    public BallDistributionError Error { get; }

    private BallDistributionResult(
        IReadOnlyList<BallDistributionEntry> entries,
        BallDistributionError error)
    {
        IsSuccess = error == null;
        Entries = entries;
        Error = error;
    }

    internal static BallDistributionResult Success(
        List<BallDistributionEntry> entries)
    {
        return new BallDistributionResult(entries.AsReadOnly(), null);
    }

    internal static BallDistributionResult Failure(
        BallDistributionError error)
    {
        return new BallDistributionResult(EmptyEntries, error);
    }
}

public static class BallDistributionGenerator
{
    private const double BalanceFactor = 0.6d;
    private static readonly double LogBalanceFactor = Math.Log(BalanceFactor);

    public static BallDistributionResult Generate(LevelDataAsset levelData)
    {
        if (levelData == null)
        {
            return Failure(
                BallDistributionErrorCode.MissingLevelData,
                null,
                null,
                "No se asignó un LevelDataAsset.");
        }

        IReadOnlyList<BallSpawnPointData> spawnPoints = levelData.SpawnPoints;
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return Failure(
                BallDistributionErrorCode.NoSpawnPoints,
                null,
                null,
                "El nivel no tiene puntos de aparición.");
        }

        var random = new System.Random(levelData.Seed);
        bool enableDynamicBalance = levelData.EnableDynamicBalance;
        BallDataAsset previousAsset = null;
        int consecutiveAppearances = 0;
        var entries = new List<BallDistributionEntry>(spawnPoints.Count);

        for (int pointIndex = 0; pointIndex < spawnPoints.Count; pointIndex++)
        {
            BallSpawnPointData spawnPoint = spawnPoints[pointIndex];
            if (spawnPoint == null)
            {
                return Failure(
                    BallDistributionErrorCode.NullSpawnPoint,
                    pointIndex,
                    null,
                    "El punto de aparición no está asignado.");
            }

            IReadOnlyList<BallSpawnOption> options = spawnPoint.SpawnOptions;
            if (options == null || options.Count == 0)
            {
                return Failure(
                    BallDistributionErrorCode.NoSpawnOptions,
                    pointIndex,
                    spawnPoint.Id,
                    "El punto no tiene opciones de aparición.");
            }

            bool previousAssetAvailable = false;
            bool hasSelectableOption = false;
            for (int optionIndex = 0;
                 optionIndex < options.Count;
                 optionIndex++)
            {
                BallSpawnOption option = options[optionIndex];
                if (option == null)
                {
                    return Failure(
                        BallDistributionErrorCode.NullSpawnOption,
                        pointIndex,
                        spawnPoint.Id,
                        "El punto contiene una opción de aparición no asignada.");
                }

                float probability = option.SpawnProbability;
                if (!IsFinite(probability) ||
                    probability < 0f ||
                    probability > 100f)
                {
                    return Failure(
                        BallDistributionErrorCode.InvalidProbability,
                        pointIndex,
                        spawnPoint.Id,
                        "La probabilidad debe ser un valor finito entre 0 y 100.");
                }

                if (option.BallData == null)
                {
                    return Failure(
                        BallDistributionErrorCode.NullBallDataAsset,
                        pointIndex,
                        spawnPoint.Id,
                        "La opción de aparición no tiene un BallDataAsset asignado.");
                }

                if (probability <= 0f)
                    continue;

                hasSelectableOption = true;
                if (ReferenceEquals(option.BallData, previousAsset))
                    previousAssetAvailable = true;
            }

            if (!hasSelectableOption)
            {
                return Failure(
                    BallDistributionErrorCode.NoSelectableOptions,
                    pointIndex,
                    spawnPoint.Id,
                    "El punto no tiene opciones con probabilidad mayor que cero.");
            }

            if (enableDynamicBalance &&
                !ReferenceEquals(previousAsset, null) &&
                !previousAssetAvailable)
            {
                previousAsset = null;
                consecutiveAppearances = 0;
            }

            double[] logWeights = new double[options.Count];
            double maximumLogWeight = double.NegativeInfinity;
            for (int optionIndex = 0;
                 optionIndex < options.Count;
                 optionIndex++)
            {
                BallSpawnOption option = options[optionIndex];
                float probability = option.SpawnProbability;
                if (probability <= 0f)
                {
                    logWeights[optionIndex] = double.NegativeInfinity;
                    continue;
                }

                double logWeight = Math.Log(probability);
                if (enableDynamicBalance &&
                    consecutiveAppearances > 0 &&
                    ReferenceEquals(option.BallData, previousAsset))
                {
                    logWeight += consecutiveAppearances * LogBalanceFactor;
                }

                logWeights[optionIndex] = logWeight;
                maximumLogWeight = Math.Max(maximumLogWeight, logWeight);
            }

            double totalWeight = 0d;
            int lastPositiveWeightIndex = -1;
            for (int optionIndex = 0;
                 optionIndex < logWeights.Length;
                 optionIndex++)
            {
                if (double.IsNegativeInfinity(logWeights[optionIndex]))
                    continue;

                double weight = Math.Exp(
                    logWeights[optionIndex] - maximumLogWeight);
                logWeights[optionIndex] = weight;
                if (weight <= 0d)
                    continue;

                totalWeight += weight;
                lastPositiveWeightIndex = optionIndex;
            }

            if (lastPositiveWeightIndex < 0 || totalWeight <= 0d)
            {
                return Failure(
                    BallDistributionErrorCode.NoSelectableOptions,
                    pointIndex,
                    spawnPoint.Id,
                    "El punto no tiene pesos seleccionables válidos.");
            }

            double targetWeight = random.NextDouble() * totalWeight;
            double cumulativeWeight = 0d;
            int selectedOptionIndex = lastPositiveWeightIndex;
            for (int optionIndex = 0;
                 optionIndex < logWeights.Length;
                 optionIndex++)
            {
                double weight = logWeights[optionIndex];
                if (weight <= 0d)
                    continue;

                cumulativeWeight += weight;
                if (targetWeight < cumulativeWeight)
                {
                    selectedOptionIndex = optionIndex;
                    break;
                }
            }

            BallSpawnOption selectedOption = options[selectedOptionIndex];
            entries.Add(
                new BallDistributionEntry(
                    pointIndex,
                    spawnPoint,
                    selectedOption));

            if (!enableDynamicBalance)
                continue;

            if (ReferenceEquals(selectedOption.BallData, previousAsset))
            {
                consecutiveAppearances++;
            }
            else
            {
                previousAsset = selectedOption.BallData;
                consecutiveAppearances = 1;
            }
        }

        return BallDistributionResult.Success(entries);
    }

    private static BallDistributionResult Failure(
        BallDistributionErrorCode code,
        int? pointIndex,
        string pointId,
        string message)
    {
        return BallDistributionResult.Failure(
            new BallDistributionError(
                code,
                pointIndex,
                pointId,
                message));
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
