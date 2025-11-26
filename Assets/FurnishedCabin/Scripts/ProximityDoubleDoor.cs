using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ProximityDoubleDoor : MonoBehaviour
{
    [Header("Referencias")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Jugador")]
    public Transform playerRoot;        // arrastra aquí el OVRCameraRigInteraction (raíz)
    public Vector3 playerOffset = Vector3.zero; // si quieres medir desde otro punto

    [Header("Ángulos (local Y)")]
    public float leftOpenDelta  = 120f;   // +Y para izquierda
    public float rightOpenDelta = -120f;  // -Y para derecha

    [Header("Velocidades")]
    public float openSpeedDegPerSec  = 180f;
    public float closeSpeedDegPerSec = 120f;

    [Header("Comportamiento")]
    public bool autoClose = true;
    public float closeDelay = 0f;  // segundos de espera antes de cerrar

    // --- internos ---
    float _leftInitY, _rightInitY;
    float _leftTargetY, _rightTargetY;
    float _closeAtTime = -1f;
    bool _isInsidePrev = false;
    BoxCollider _box;

    void Reset()
    {
        var bc = GetComponent<BoxCollider>();
        bc.isTrigger = true; // no es obligatorio ya, pero ayuda visualmente
    }

    void Awake()
    {
        _box = GetComponent<BoxCollider>();

        if (!leftDoor || !rightDoor)
        {
            Debug.LogError("[ProximityDoubleDoor] Asigna leftDoor y rightDoor en el inspector.");
            enabled = false; return;
        }

        _leftInitY  = leftDoor.localEulerAngles.y;
        _rightInitY = rightDoor.localEulerAngles.y;

        _leftTargetY  = _leftInitY;
        _rightTargetY = _rightInitY;

        if (!playerRoot)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) playerRoot = p.transform;
        }
    }

    void Update()
    {
        if (!playerRoot) return;

        // 1) ¿El jugador está dentro del BoxCollider (en mundo)?
        bool isInside = IsPointInsideBox(_box, playerRoot.position + playerOffset);

        // 2) Estado → targets
        if (isInside)
        {
            _leftTargetY  = _leftInitY  + leftOpenDelta;
            _rightTargetY = _rightInitY + rightOpenDelta;
            _closeAtTime = -1f; // cancela cierre programado
        }
        else if (autoClose)
        {
            if (closeDelay <= 0f)   // cerrar ya
            {
                _leftTargetY  = _leftInitY;
                _rightTargetY = _rightInitY;
            }
            else                    // programa cierre
            {
                if (_isInsidePrev && _closeAtTime < 0f)
                    _closeAtTime = Time.time + closeDelay;

                if (_closeAtTime > 0f && Time.time >= _closeAtTime)
                {
                    _leftTargetY  = _leftInitY;
                    _rightTargetY = _rightInitY;
                    _closeAtTime = -1f;
                }
            }
        }

        _isInsidePrev = isInside;

        // 3) Interpolar hacia target (suave)
        float dt = Time.deltaTime;
        float spOpen  = openSpeedDegPerSec  * dt;
        float spClose = closeSpeedDegPerSec * dt;

        float curLeft  = leftDoor.localEulerAngles.y;
        float curRight = rightDoor.localEulerAngles.y;

        bool opening = Mathf.DeltaAngle(curLeft,  _leftTargetY)  < 0f ? leftOpenDelta  < 0f : leftOpenDelta  > 0f;
        // Para la velocidad escogemos openSpeed si el target está alejado del inicial, si no, closeSpeed
        float spL = (Mathf.Approximately(_leftTargetY,  _leftInitY)  ? spClose : spOpen);
        float spR = (Mathf.Approximately(_rightTargetY, _rightInitY) ? spClose : spOpen);

        float newLeft  = Mathf.MoveTowardsAngle(curLeft,  _leftTargetY,  spL);
        float newRight = Mathf.MoveTowardsAngle(curRight, _rightTargetY, spR);

        var le = leftDoor.localEulerAngles;  le.y = newLeft;  leftDoor.localEulerAngles  = le;
        var re = rightDoor.localEulerAngles; re.y = newRight; rightDoor.localEulerAngles = re;
    }

    // Comprueba si un punto mundo está dentro del BoxCollider (considerando su rotación/escala)
    static bool IsPointInsideBox(BoxCollider box, Vector3 worldPoint)
    {
        // Transformar punto al espacio local del collider
        Vector3 local = box.transform.InverseTransformPoint(worldPoint);
        Vector3 half  = box.size * 0.5f;
        Vector3 c     = box.center;

        return (local.x >= c.x - half.x && local.x <= c.x + half.x) &&
               (local.y >= c.y - half.y && local.y <= c.y + half.y) &&
               (local.z >= c.z - half.z && local.z <= c.z + half.z);
    }
}
