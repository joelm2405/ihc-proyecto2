using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class PlayerZoneTracker : MonoBehaviour
{
    public bool autoAddKinematicRB = true;

    readonly HashSet<ZoneArea> _inside = new HashSet<ZoneArea>();

    void Awake()
    {
        if (autoAddKinematicRB)
        {
            var rb = GetComponent<Rigidbody>();
            if (!rb) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }

    void Update()
    {
        if (ScoreManager.I == null) return;

        bool inDanger = false;
        bool inSafe   = false;

        foreach (var z in _inside)
        {
            if (!z) continue;
            if (z.type == ZoneArea.ZoneType.Danger) { inDanger = true; break; }
            if (z.type == ZoneArea.ZoneType.Safe)   { inSafe   = true; }
        }

        if (inDanger)
            ScoreManager.I.AddOverTime(ScoreManager.I.dangerRate);
        else if (inSafe)
            ScoreManager.I.AddOverTime(ScoreManager.I.safeRate);
        else
            ScoreManager.I.AddOverTime(ScoreManager.I.neutralRate);
    }

    void OnTriggerEnter(Collider other)
    {
        var zone = other.GetComponent<ZoneArea>();
        if (zone != null) _inside.Add(zone);
    }

    void OnTriggerExit(Collider other)
    {
        var zone = other.GetComponent<ZoneArea>();
        if (zone != null) _inside.Remove(zone);
    }
}
