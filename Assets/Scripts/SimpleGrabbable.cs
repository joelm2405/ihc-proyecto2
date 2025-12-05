using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleGrabbable : MonoBehaviour
{
    [Tooltip("Punto de agarre opcional; si se deja vacío usa el centro del objeto.")]
    public Transform grabPivot;

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Transform originalParent;
    [HideInInspector] public bool originalKinematic;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;
        originalKinematic = rb.isKinematic;
    }
}
