using UnityEngine;

public class Movimiento : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform escenario;          // arrastra el Escenario que vibra
    [SerializeField] private EarthquakeHybrid terremoto;   // arrastra tu EarthquakeHybrid (opcional)

    [Header("Parámetros físicos")]
    [SerializeField] private float fuerza = 1.5f;
    [SerializeField] private float verticalBoost = 1.0f;

    private Rigidbody rb;
    private Vector3 lastPos;
    private Vector3 lastVel;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>(); // asegura que haya RB
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        if (escenario != null)
            lastPos = escenario.position;
    }

    void FixedUpdate()
    {
        if (escenario == null || rb == null) return;

        float dt = Time.fixedDeltaTime;
        Vector3 pos = escenario.position;
        Vector3 vel = (pos - lastPos) / dt;
        Vector3 accel = (vel - lastVel) / dt;

        lastPos = pos;
        lastVel = vel;

        Vector3 pseudoInercia = -accel;
        pseudoInercia.y *= verticalBoost;

        float escala = 1f;
        if (terremoto != null && terremoto.intensidadMaxima > 0f)
            escala = Mathf.Clamp01(terremoto.GetIntensidadActual() / terremoto.intensidadMaxima);

        rb.AddForce(pseudoInercia * fuerza * escala, ForceMode.Acceleration);
    }
}
