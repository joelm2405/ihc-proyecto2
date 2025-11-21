using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class ZoneArea : MonoBehaviour
{
    public enum ZoneType { Safe, Danger }
    public ZoneType type = ZoneType.Safe;
    public string zoneName = "SafeZone_A";

    void Reset()
    {
        var c = GetComponent<BoxCollider>();
        c.isTrigger = true;
    }
}
