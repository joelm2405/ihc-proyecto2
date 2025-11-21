using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WASDProxy : MonoBehaviour
{
    public float moveSpeed = 3f;       // velocidad en m/s
    public float accel = 20f;          // qué tan rápido alcanza la velocidad
    public float sprintMultiplier = 1.6f;
    public float maxSpeed = 5f;        // límite superior por seguridad

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;                 // ON para que caiga
        rb.isKinematic = false;               // dinámico
        rb.constraints = RigidbodyConstraints.FreezeRotation; // que no se vuelque
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D o ←/→
        float v = Input.GetAxisRaw("Vertical");   // W/S o ↑/↓
        Vector3 input = new Vector3(h, 0f, v).normalized;

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        // velocidad objetivo en plano XZ
        Vector3 targetVel = input * speed;
        Vector3 current = rb.linearVelocity;
        Vector3 desired = new Vector3(targetVel.x, current.y, targetVel.z);

        // acercar la velocidad actual a la deseada suavemente
        rb.linearVelocity = Vector3.MoveTowards(current, desired, accel * Time.fixedDeltaTime);

        // clamp por seguridad
        Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flat.magnitude > maxSpeed)
        {
            flat = flat.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(flat.x, rb.linearVelocity.y, flat.z);
        }
    }
}
