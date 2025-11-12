using UnityEngine;

public class OVRRayVisual : MonoBehaviour
{
    LineRenderer line;
    public float length = 5f;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        if (line != null) line.positionCount = 2;
    }

    void Update()
    {
        if (line == null) return;

        line.SetPosition(0, transform.position);
        line.SetPosition(1, transform.position + transform.forward * length);
    }
}
