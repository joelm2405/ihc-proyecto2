using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ProximityDoubleDoor : MonoBehaviour
{
    [Header("Referencias")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Colliders de las Puertas")]
    [Tooltip("Se encuentran automaticamente si estan vacios")]
    public Collider leftDoorCollider;
    public Collider rightDoorCollider;

    [Header("Jugador")]
    public Transform playerRoot;        // arrastra aquí el OVRCameraRigInteraction (raíz)
    public Vector3 playerOffset = Vector3.zero; // si quieres medir desde otro punto

    [Header("Ángulos (local Y)")]
    public float leftOpenDelta = 120f;   // +Y para izquierda
    public float rightOpenDelta = -120f;  // -Y para derecha

    [Header("Velocidades")]
    public float openSpeedDegPerSec = 180f;
    public float closeSpeedDegPerSec = 120f;

    [Header("Comportamiento")]
    public bool autoClose = true;
    public float closeDelay = 0f;  // segundos de espera antes de cerrar

    [Header("Proteccion de Terremoto")]
    [Tooltip("Referencia al EarthquakeManager - las puertas se bloquean durante el terremoto")]
    public EarthquakeHybrid earthquakeManager;

    [Tooltip("Bloquear puertas durante el terremoto")]
    public bool bloquearDuranteTerremoto = true;

    [Header("Debug")]
    public bool mostrarDebug = false;

    // --- internos ---
    float _leftInitY, _rightInitY;
    float _leftTargetY, _rightTargetY;
    float _closeAtTime = -1f;
    bool _isInsidePrev = false;
    bool _puertasBloqueadas = false;
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

        _leftInitY = leftDoor.localEulerAngles.y;
        _rightInitY = rightDoor.localEulerAngles.y;

        _leftTargetY = _leftInitY;
        _rightTargetY = _rightInitY;

        if (!playerRoot)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) playerRoot = p.transform;
        }

        // Buscar colliders de las puertas si no estan asignados
        if (leftDoorCollider == null && leftDoor != null)
        {
            leftDoorCollider = leftDoor.GetComponent<Collider>();
            if (leftDoorCollider != null && mostrarDebug)
                Debug.Log("[ProximityDoubleDoor] Left door collider encontrado automaticamente");
        }

        if (rightDoorCollider == null && rightDoor != null)
        {
            rightDoorCollider = rightDoor.GetComponent<Collider>();
            if (rightDoorCollider != null && mostrarDebug)
                Debug.Log("[ProximityDoubleDoor] Right door collider encontrado automaticamente");
        }

        // ACTIVAR colliders al inicio (puertas cerradas y solidas)
        ActivarCollidersPuertas(true);

        // Buscar EarthquakeManager si no esta asignado
        if (earthquakeManager == null && bloquearDuranteTerremoto)
        {
            earthquakeManager = FindObjectOfType<EarthquakeHybrid>();

            if (earthquakeManager != null && mostrarDebug)
            {
                Debug.Log("[ProximityDoubleDoor] EarthquakeManager encontrado automaticamente");
            }
        }
    }

    void Update()
    {
        if (!playerRoot) return;

        // === VERIFICAR SI LAS PUERTAS ESTÁN BLOQUEADAS ===
        bool puertasDeberianEstarBloqueadas = DeberianEstarBloqueadas();

        // Log cuando cambia el estado de bloqueo
        if (puertasDeberianEstarBloqueadas != _puertasBloqueadas && mostrarDebug)
        {
            if (puertasDeberianEstarBloqueadas)
                Debug.Log("<color=red>[ProximityDoubleDoor] Puertas BLOQUEADAS - Terremoto activo</color>");
            else
                Debug.Log("<color=green>[ProximityDoubleDoor] Puertas DESBLOQUEADAS - Terremoto terminado</color>");
        }

        _puertasBloqueadas = puertasDeberianEstarBloqueadas;

        // Si las puertas están bloqueadas, forzar cierre
        if (_puertasBloqueadas)
        {
            _leftTargetY = _leftInitY;
            _rightTargetY = _rightInitY;
            _closeAtTime = -1f;

            // ACTIVAR colliders para que sean solidas durante el terremoto
            ActivarCollidersPuertas(true);

            // Interpolar hacia cerrado (más abajo)
            InterpolrarPuertas();
            return;
        }

        // === LÓGICA NORMAL DE APERTURA (solo si no hay terremoto) ===

        // 1) ¿El jugador está dentro del BoxCollider (en mundo)?
        bool isInside = IsPointInsideBox(_box, playerRoot.position + playerOffset);

        // 2) Estado → targets
        if (isInside)
        {
            _leftTargetY = _leftInitY + leftOpenDelta;
            _rightTargetY = _rightInitY + rightOpenDelta;
            _closeAtTime = -1f; // cancela cierre programado

            // DESACTIVAR colliders cuando las puertas estan abiertas
            ActivarCollidersPuertas(false);
        }
        else if (autoClose)
        {
            if (closeDelay <= 0f)   // cerrar ya
            {
                _leftTargetY = _leftInitY;
                _rightTargetY = _rightInitY;

                // ACTIVAR colliders cuando las puertas se cierran
                ActivarCollidersPuertas(true);
            }
            else                    // programa cierre
            {
                if (_isInsidePrev && _closeAtTime < 0f)
                    _closeAtTime = Time.time + closeDelay;

                if (_closeAtTime > 0f && Time.time >= _closeAtTime)
                {
                    _leftTargetY = _leftInitY;
                    _rightTargetY = _rightInitY;
                    _closeAtTime = -1f;

                    // ACTIVAR colliders cuando las puertas se cierran
                    ActivarCollidersPuertas(true);
                }
            }
        }

        _isInsidePrev = isInside;

        // 3) Interpolar hacia target (suave)
        InterpolrarPuertas();
    }

    bool DeberianEstarBloqueadas()
    {
        // Si no está activado el bloqueo, siempre false
        if (!bloquearDuranteTerremoto)
            return false;

        // Si no hay earthquakeManager, no bloquear
        if (earthquakeManager == null)
            return false;

        // Bloquear SOLO durante el terremoto activo
        // NO bloquear antes ni después
        return earthquakeManager.EstaTerremotoActivo();
    }

    void ActivarCollidersPuertas(bool activar)
    {
        if (leftDoorCollider != null)
        {
            leftDoorCollider.enabled = activar;
        }

        if (rightDoorCollider != null)
        {
            rightDoorCollider.enabled = activar;
        }

        if (mostrarDebug && Time.frameCount % 120 == 0)
        {
            Debug.Log($"[ProximityDoubleDoor] Colliders de puertas: {(activar ? "ACTIVADOS" : "DESACTIVADOS")}");
        }
    }

    void InterpolrarPuertas()
    {
        float dt = Time.deltaTime;
        float spOpen = openSpeedDegPerSec * dt;
        float spClose = closeSpeedDegPerSec * dt;

        float curLeft = leftDoor.localEulerAngles.y;
        float curRight = rightDoor.localEulerAngles.y;

        // Para la velocidad escogemos openSpeed si el target está alejado del inicial, si no, closeSpeed
        float spL = (Mathf.Approximately(_leftTargetY, _leftInitY) ? spClose : spOpen);
        float spR = (Mathf.Approximately(_rightTargetY, _rightInitY) ? spClose : spOpen);

        float newLeft = Mathf.MoveTowardsAngle(curLeft, _leftTargetY, spL);
        float newRight = Mathf.MoveTowardsAngle(curRight, _rightTargetY, spR);

        var le = leftDoor.localEulerAngles; le.y = newLeft; leftDoor.localEulerAngles = le;
        var re = rightDoor.localEulerAngles; re.y = newRight; rightDoor.localEulerAngles = re;
    }

    // Comprueba si un punto mundo está dentro del BoxCollider (considerando su rotación/escala)
    static bool IsPointInsideBox(BoxCollider box, Vector3 worldPoint)
    {
        // Transformar punto al espacio local del collider
        Vector3 local = box.transform.InverseTransformPoint(worldPoint);
        Vector3 half = box.size * 0.5f;
        Vector3 c = box.center;

        return (local.x >= c.x - half.x && local.x <= c.x + half.x) &&
               (local.y >= c.y - half.y && local.y <= c.y + half.y) &&
               (local.z >= c.z - half.z && local.z <= c.z + half.z);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || _box == null) return;
        
        // Cambiar color del gizmo según si están bloqueadas
        if (_puertasBloqueadas)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Rojo = bloqueadas
        }
        else
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Verde = desbloqueadas
        }
        
        // Dibujar el box collider
        Gizmos.matrix = _box.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(_box.center, _box.size);
        
        if (_puertasBloqueadas)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(_box.center, _box.size * 0.5f);
        }
    }
#endif
}