using UnityEngine;

public class SimpleHandGrabber : MonoBehaviour
{
    public enum Hand { Left, Right }

    [Header("Input")]
    public Hand hand = Hand.Right;
    [Range(0f,1f)] public float triggerThreshold = 0.6f;

    [Header("Grab")]
    public Transform grabAnchor;                 // un hijo en la mano donde “se pega” el objeto
    public float grabRadius = 0.15f;             // distancia de búsqueda
    public LayerMask grabbableLayers;            // capa de objetos agarrables (opcional)
    public bool alignToPivot = true;             // alinea el pivot si existe

    [Header("Throw")]
    public float velocityMultiplier = 1.0f;
    public float angularVelocityMultiplier = 1.0f;

    SimpleGrabbable held;                        // actual
    bool wasPressed;

#if OVRPLUGIN_PRESENT || OCULUS_INTEGRATION
    OVRInput.Controller ControllerOVR =>
        hand == Hand.Right ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;
#endif

    void Reset()
    {
        if (!grabAnchor)
        {
            // si no hay anchor, usa este mismo transform
            grabAnchor = transform;
        }
    }

    void Update()
    {
        float trigger = 0f;
#if OVRPLUGIN_PRESENT || OCULUS_INTEGRATION
        trigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, ControllerOVR);
#else
        // fallback para editor: click izquierdo simula gatillo
        trigger = Input.GetMouseButton(0) ? 1f : 0f;
#endif
        bool pressed = trigger > triggerThreshold;

        if (pressed && !wasPressed) TryGrab();
        if (!pressed && wasPressed) TryRelease();

        wasPressed = pressed;

        // seguir el anchor mientras está tomado (suavizado 100% pegado)
        if (held)
        {
            held.transform.position = grabAnchor.position;
            held.transform.rotation = grabAnchor.rotation;
        }
    }

    void TryGrab()
    {
        if (held) return;

        // Busca el SimpleGrabbable más cercano dentro del radio
        Collider[] hits = Physics.OverlapSphere(grabAnchor.position, grabRadius, grabbableLayers.value == 0 ? ~0 : grabbableLayers);
        SimpleGrabbable best = null;
        float bestDist = Mathf.Infinity;

        foreach (var h in hits)
        {
            var g = h.GetComponentInParent<SimpleGrabbable>();
            if (!g) continue;
            float d = Vector3.SqrMagnitude((g.grabPivot ? g.grabPivot.position : g.transform.position) - grabAnchor.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = g;
            }
        }

        if (!best) return;

        held = best;
        held.originalParent = held.transform.parent;
        held.originalKinematic = held.rb.isKinematic;

        // “pega” al anchor
        if (alignToPivot && held.grabPivot)
        {
            // alinear el pivot del objeto con el anchor
            var offset = grabAnchor.worldToLocalMatrix * held.grabPivot.localToWorldMatrix;
            held.transform.SetPositionAndRotation(grabAnchor.position, grabAnchor.rotation * offset.rotation);
        }

        held.transform.SetParent(grabAnchor, true);
        held.rb.isKinematic = true;    // desactiva física mientras está agarrado
    }

    void TryRelease()
    {
        if (!held) return;

        // desacoplar
        held.transform.SetParent(held.originalParent, true);
        held.rb.isKinematic = held.originalKinematic;

        // aplica velocidad del control (throw)
        Vector3 v = Vector3.zero;
        Vector3 w = Vector3.zero;

#if OVRPLUGIN_PRESENT || OCULUS_INTEGRATION
        v = OVRInput.GetLocalControllerVelocity(ControllerOVR);
        w = OVRInput.GetLocalControllerAngularVelocity(ControllerOVR);
        // convierte de espacio local del controlador a mundo
        v = grabAnchor.TransformVector(v) * velocityMultiplier;
        w = grabAnchor.TransformVector(w) * angularVelocityMultiplier;
#endif
        held.rb.linearVelocity = v;
        held.rb.angularVelocity = w;

        held = null;
    }

    void OnDrawGizmosSelected()
    {
        if (!grabAnchor) grabAnchor = transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(grabAnchor.position, grabRadius);
    }
}
