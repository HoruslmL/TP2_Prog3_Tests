using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BallPool ballPool;
    [SerializeField] private BallDataAsset normalBallData;
    [SerializeField] private BallDataAsset explosiveBallData;

    private Inventory inventory;

    public void Initialize(Inventory inventory)
    {
        if (inventory == null)
        {
            Debug.LogError("BallSpawner: Inventory es null.");
            return;
        }

        this.inventory = inventory;
    }

    public BallController SpawnBall(
        BallAbilityType type,
        Vector2 position)
    {
        if (inventory == null || ballPool == null)
        {
            Debug.LogError("BallSpawner: faltan referencias.");
            return null;
        }

        // Comprobamos si hay unidades disponibles.
        if (!inventory.HasBall(type))
            return null;

        // Obtenemos los datos correspondientes al tipo.
        BallDataAsset dataAsset = GetDataAsset(type);

        if (dataAsset == null)
        {
            Debug.LogError("BallSpawner: no hay BallDataAsset para este tipo.");
            return null;
        }

        // Solicitamos una instancia al pool.
        BallController ball = ballPool.GetBall(
            dataAsset.ToBallData(),
            position
        );

        if (ball == null)
        {
            Debug.LogError("BallSpawner: BallPool no pudo entregar una bolita.");
            return null;
        }


        // Descontamos la unidad solamente si se generó correctamente.
        if (!inventory.TryUseBall(type))
        {
            ball.Despawn();
            return null;
        }

        return ball;
    }

    private BallDataAsset GetDataAsset(BallAbilityType type)
    {
        switch (type)
        {
            case BallAbilityType.None:
                return normalBallData;

            case BallAbilityType.Explosive:
                return explosiveBallData;

            default:
                return null;
        }
    }
}
