using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class LevelBallSpawner : MonoBehaviour
{
    [SerializeField] private LevelDataAsset levelData;
    [SerializeField] private Transform levelRoot;
    [SerializeField] private BallPool ballPool;

    private bool hasSpawnedLevel;

    public bool TrySpawnLevel(out string error)
    {
        error = null;

        if (hasSpawnedLevel)
        {
            error = "Este LevelBallSpawner ya generó el nivel; no se permiten duplicados.";
            return false;
        }

        LevelDataValidationResult validation =
            LevelDataValidator.Validate(levelData, levelRoot);
        if (!validation.IsValid)
        {
            error = FormatValidationIssues(validation);
            return false;
        }

        BallDistributionResult distribution =
            BallDistributionGenerator.Generate(levelData);
        if (!distribution.IsSuccess)
        {
            error = "No se pudo generar la distribución: " +
                distribution.Error.Code + ": " +
                distribution.Error.Message;
            return false;
        }

        if (ballPool == null)
        {
            error = "No se asignó un BallPool ya inicializado.";
            return false;
        }

        var spawnedBalls = new List<BallController>(distribution.Entries.Count);
        for (int i = 0; i < distribution.Entries.Count; i++)
        {
            BallDistributionEntry entry = distribution.Entries[i];
            BallData ballData = entry.BallData.ToBallData();
            Vector3 worldPosition = levelRoot.TransformPoint(
                new Vector3(
                    entry.LocalPosition.x,
                    entry.LocalPosition.y,
                    0f));

            BallController ball = ballPool.GetBall(
                ballData,
                new Vector2(worldPosition.x, worldPosition.y));

            if (ball == null)
            {
                error = RollBack(
                    spawnedBalls,
                    "BallPool no pudo proporcionar una bolita para el punto '" +
                    entry.SpawnPointId + "' (índice " +
                    entry.SpawnPointIndex + "). Verificá que el pool esté " +
                    "inicializado y tenga capacidad disponible.");
                return false;
            }

            spawnedBalls.Add(ball);
        }

        hasSpawnedLevel = true;
        return true;
    }

    private static string FormatValidationIssues(
        LevelDataValidationResult validation)
    {
        var message = new StringBuilder(
            "La configuración del nivel no es válida:");

        for (int i = 0; i < validation.Issues.Count; i++)
        {
            LevelDataValidationIssue issue = validation.Issues[i];
            message.AppendLine();
            message.Append(issue.Code);
            if (issue.SpawnPointIndex.HasValue)
                message.Append(" en el punto ").Append(issue.SpawnPointIndex.Value);
            if (!string.IsNullOrEmpty(issue.SpawnPointId))
                message.Append(" (").Append(issue.SpawnPointId).Append(')');
            message.Append(": ").Append(issue.Message);
        }

        return message.ToString();
    }

    private static string RollBack(
        List<BallController> spawnedBalls,
        string failureMessage)
    {
        var rollbackFailures = new List<string>();
        for (int i = spawnedBalls.Count - 1; i >= 0; i--)
        {
            BallController ball = spawnedBalls[i];
            if (ball == null)
                continue;

            ball.Despawn();
            if (ball != null && ball.gameObject.activeSelf)
                rollbackFailures.Add(ball.name);
        }

        if (rollbackFailures.Count == 0)
            return failureMessage;

        return failureMessage + " No se pudieron devolver al pool estas " +
            "instancias: " + string.Join(", ", rollbackFailures) + ".";
    }
}
