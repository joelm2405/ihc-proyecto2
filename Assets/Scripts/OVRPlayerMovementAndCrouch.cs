using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class OVRPlayerMovementAndCrouch : MonoBehaviour
{
    [Header("=== MOVIMIENTO ===")]
    public float velocidadMovimiento = 3f;
    public float velocidadRotacion = 90f;
    public bool rotacionSnap = true;
    public float anguloSnap = 30f;

    [Header("=== AGACHARSE ===")]
    public float cantidadAgacharse = 0.7f;
    public float velocidadTransicion = 5f;

    [Header("=== ALTURA BASE ===")]
    public float alturaBase = 0f;

    [Header("=== GRAVEDAD ===")]
    public bool aplicarGravedad = true;
    public float fuerzaGravedad = 9.81f;

    [Header("=== REFERENCIAS ===")]
    public Transform trackingSpace;
    public Transform centerEyeAnchor;

    [Header("Auto-switch por mano")]
    public PerHandAutoSwitcher handSwitch; // arrástralo desde el Rig

    CharacterController cc;
    bool estaAgachado = false;
    float offsetObjetivo, offsetActual;
    float alturaCCOriginal;
    Vector3 centroCCOriginal;
    bool rotacionEnProgreso = false;
    Vector3 velVertical = Vector3.zero;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        var rig = GetComponentInChildren<OVRCameraRig>();
        if (!rig) { Debug.LogError("OVRCameraRig no encontrado."); enabled = false; return; }

        if (!trackingSpace)   trackingSpace   = rig.trackingSpace;
        if (!centerEyeAnchor) centerEyeAnchor = rig.centerEyeAnchor;
        if (!handSwitch)      handSwitch      = GetComponentInChildren<PerHandAutoSwitcher>(true);

        alturaCCOriginal = cc.height;
        centroCCOriginal = cc.center;

        offsetActual = offsetObjetivo = alturaBase;
        AplicarAltura(offsetActual);
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two)) ToggleCrouch();

        if (Mathf.Abs(offsetActual - offsetObjetivo) > 0.001f)
        {
            offsetActual = Mathf.Lerp(offsetActual, offsetObjetivo, Time.deltaTime * velocidadTransicion);
            AplicarAltura(offsetActual);
        }

        ProcesarMovimientoYRotacion();
        if (aplicarGravedad) AplicarGravedad();
    }

    void ProcesarMovimientoYRotacion()
    {
        // Decide qué lado tiene mando activo
        bool leftHasCtrl  = handSwitch ? handSwitch.LeftIsController  : OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch);
        bool rightHasCtrl = handSwitch ? handSwitch.RightIsController : OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch);

        // Movimiento: prioriza mando izquierdo; si no hay, usa el derecho; si ninguno, no mueve.
        OVRInput.Controller moveCtrl =
            leftHasCtrl  ? OVRInput.Controller.LTouch :
            rightHasCtrl ? OVRInput.Controller.RTouch : OVRInput.Controller.None;

        // Rotación: prioriza mando derecho; si no hay, usa el izquierdo.
        OVRInput.Controller rotCtrl =
            rightHasCtrl ? OVRInput.Controller.RTouch :
            leftHasCtrl  ? OVRInput.Controller.LTouch : OVRInput.Controller.None;

        if (moveCtrl != OVRInput.Controller.None)
        {
            Vector2 in2 = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, moveCtrl);
            if (in2.sqrMagnitude > 0.01f)
            {
                Vector3 fwd = centerEyeAnchor.forward;  fwd.y = 0; fwd.Normalize();
                Vector3 rgt = centerEyeAnchor.right;    rgt.y = 0; rgt.Normalize();
                Vector3 move = (fwd * in2.y + rgt * in2.x) * (velocidadMovimiento * Time.deltaTime);
                cc.Move(move);
            }
        }

        if (rotCtrl != OVRInput.Controller.None)
        {
            float x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, rotCtrl).x;
            if (rotacionSnap)
            {
                if (!rotacionEnProgreso && Mathf.Abs(x) > 0.75f)
                {
                    rotacionEnProgreso = true;
                    float step = x > 0 ? anguloSnap : -anguloSnap;
                    transform.RotateAround(centerEyeAnchor.position, Vector3.up, step);
                }
                else if (Mathf.Abs(x) < 0.3f) rotacionEnProgreso = false;
            }
            else
            {
                if (Mathf.Abs(x) > 0.1f)
                {
                    float step = x * velocidadRotacion * Time.deltaTime;
                    transform.RotateAround(centerEyeAnchor.position, Vector3.up, step);
                }
            }
        }
    }

    void AplicarGravedad()
    {
        if (cc.isGrounded) velVertical.y = -0.5f;
        else velVertical.y -= fuerzaGravedad * Time.deltaTime;
        cc.Move(velVertical * Time.deltaTime);
    }

    void ToggleCrouch()
    {
        estaAgachado = !estaAgachado;
        offsetObjetivo = estaAgachado ? (alturaBase - cantidadAgacharse) : alturaBase;
    }

    void AplicarAltura(float offset)
    {
        if (!trackingSpace) return;
        var lp = trackingSpace.localPosition; lp.y = offset; trackingSpace.localPosition = lp;

        float h = alturaCCOriginal + offset;
        cc.height = h;
        var c = centroCCOriginal; c.y = h * 0.5f; cc.center = c;
    }
}
