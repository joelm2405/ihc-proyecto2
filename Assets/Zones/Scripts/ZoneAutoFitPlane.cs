using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(MeshFilter))]
public class ZoneAutoFitPlane : MonoBehaviour
{
    [Header("Trigger")]
    public bool isTrigger = true;

    [Header("Grosor vertical del área (m)")]
    [Range(0.05f, 2f)] public float thicknessY = 0.6f;

    [Header("Ajustes finos")]
    [Tooltip("Levanta el centro del collider si lo necesitas")]
    public float centerYOffset = 0f;

    [Tooltip("Eleva levemente el render (no el collider) para evitar z-fighting")]
    public float visualLift = 0.01f;

    // Cache
    BoxCollider boxCol;
    MeshFilter mf;

    void Reset()      { Apply(); }
    void Awake()      { Apply(); }
    void OnEnable()   { Apply(); }
    void OnValidate() { Apply(); }

    void Apply()
    {
        if (!boxCol) boxCol = GetComponent<BoxCollider>();
        if (!mf)     mf     = GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh || !boxCol) return;

        // 1) Datos del mesh en ESPACIO LOCAL (para un Plane: ~10 x 0 x 10)
        var mesh     = mf.sharedMesh;
        var sizeL    = mesh.bounds.size;   // local
        var centerL  = mesh.bounds.center; // local

        // 2) Fijamos el BoxCollider en ESPACIO LOCAL
        //    *El transform lo escalará a mundo automáticamente*
        var newSize = new Vector3(sizeL.x, thicknessY, sizeL.z);
        var newCent = new Vector3(centerL.x, (thicknessY * 0.5f) + centerYOffset, centerL.z);

        // Evita re-asignar si no cambió (reduce dirty flags en editor)
        if (boxCol.size != newSize)   boxCol.size = newSize;
        if (boxCol.center != newCent) boxCol.center = newCent;

        if (boxCol.isTrigger != isTrigger) boxCol.isTrigger = isTrigger;

        // 3) Opcional: levanta solo el visual (no toca el collider)
        //    Si el Plane es hijo único, suele bastar con ajustar su posición Y levemente.
        //    (Este script NO mueve el collider: solo el renderer si existe)
        var mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            // Truco simple: si el objeto no tiene hijos, levanta un poquito el transform.
            // Si prefieres no mover el transform, comenta estas líneas.
            var t = transform.localPosition;
            if (!Application.isPlaying && Mathf.Abs(t.y - visualLift) > 1e-4f)
                transform.localPosition = new Vector3(t.x, visualLift, t.z);
        }
    }
}
