using UnityEngine;

public class PerHandAutoSwitcher : MonoBehaviour
{
    [Header("Roots (por mano)")]
    public GameObject leftHandRoot;
    public GameObject rightHandRoot;
    public GameObject leftControllerRoot;
    public GameObject rightControllerRoot;

    [Header("Tiempos y umbrales")]
    [Tooltip("Segundos de inactividad para pasar a Hand Tracking en ese lado")]
    public float idleToHandSeconds = 0.6f;
    [Tooltip("Magnitud mínima para considerar actividad (joystick/velocidad)")]
    public float controllerActivityDeadzone = 0.15f;

    float _leftIdle, _rightIdle;

    // Estados con backing fields (las properties son de solo lectura)
    bool _leftIsController  = true;
    bool _rightIsController = true;
    public bool LeftIsController  => _leftIsController;
    public bool RightIsController => _rightIsController;

    void Reset()  { AutoHook(); }
    void Awake()
    {
        AutoHook();
        SetLeft(true);   // arrancar con mando visible
        SetRight(true);
    }

    void AutoHook()
    {
        if (!leftHandRoot)       leftHandRoot       = FindDeep(transform, "LeftHand");
        if (!rightHandRoot)      rightHandRoot      = FindDeep(transform, "RightHand");
        if (!leftControllerRoot) leftControllerRoot = FindDeep(transform, "LeftController");
        if (!rightControllerRoot)rightControllerRoot= FindDeep(transform, "RightController");
    }

    GameObject FindDeep(Transform root, string name)
    {
        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all) if (t.name == name) return t.gameObject;
        return null;
    }

    void Update()
    {
        UpdateSide(
            OVRInput.Controller.LTouch,
            ref _leftIdle,
            _leftIsController,
            sideToController => SetLeft(sideToController)
        );

        UpdateSide(
            OVRInput.Controller.RTouch,
            ref _rightIdle,
            _rightIsController,
            sideToController => SetRight(sideToController)
        );
    }

    // Lógica por mano (sin ref en properties)
    void UpdateSide(
        OVRInput.Controller ctrl,
        ref float idleTimer,
        bool isControllerNow,
        System.Action<bool> applySideToController)
    {
        bool tracked = OVRInput.GetControllerPositionTracked(ctrl);

        // actividad del mando
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, ctrl);
        Vector3 vLin  = OVRInput.GetLocalControllerVelocity(ctrl);
        Vector3 vAng  = OVRInput.GetLocalControllerAngularVelocity(ctrl);
        bool anyActivity =
            OVRInput.Get(OVRInput.Button.Any, ctrl) ||
            Mathf.Abs(stick.x) > controllerActivityDeadzone ||
            Mathf.Abs(stick.y) > controllerActivityDeadzone ||
            vLin.sqrMagnitude > controllerActivityDeadzone * controllerActivityDeadzone ||
            vAng.sqrMagnitude > controllerActivityDeadzone * controllerActivityDeadzone;

        if (tracked && anyActivity)
        {
            idleTimer = 0f;
            if (!isControllerNow) applySideToController(true); // volver a mando
        }
        else
        {
            idleTimer += Time.unscaledDeltaTime;
            if (!tracked || idleTimer >= idleToHandSeconds)
            {
                if (isControllerNow) applySideToController(false); // pasar a mano
            }
        }
    }

    // Cambios efectivos por lado
    public void SetLeft(bool toController)
    {
        _leftIsController = toController;
        if (leftControllerRoot) leftControllerRoot.SetActive(toController);
        if (leftHandRoot)       leftHandRoot.SetActive(!toController);
    }

    public void SetRight(bool toController)
    {
        _rightIsController = toController;
        if (rightControllerRoot) rightControllerRoot.SetActive(toController);
        if (rightHandRoot)       rightHandRoot.SetActive(!toController);
    }
}
