using UnityEngine;

public class BallTestLauncher : MonoBehaviour
{
    [SerializeField] private Rigidbody2D target;
    [SerializeField] private float launchSpeed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (target == null)
        {
            Debug.LogError("BallTestLauncher: falta asignar el Rigidbody2D objetivo.");
            return;
        }

        BallController controller = GetComponent<BallController>();
        if (controller == null)
        {
            Debug.LogError("BallTestLauncher: falta BallController.");
            return;
        }

        Vector2 direction =
            (target.position - GetComponent<Rigidbody2D>().position).normalized;

        controller.LaunchWithVelocity(direction * launchSpeed);
    }
}
