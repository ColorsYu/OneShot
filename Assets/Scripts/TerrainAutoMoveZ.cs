using System.Collections.Generic;
using UnityEngine;

public class TerrainAutoMoveZ_MultiPassengers : MonoBehaviour
{
    public float speed = 5f;
    public bool movementEnabled = true;
    public bool scanAtStart = true;
    public string[] ignorePassengerTags = new string[] { "Sphere" };
    public Transform goalTransform;
    public string goalObjectName = "Goal";
    public Transform[] extraMoveTargets;

    private readonly HashSet<Rigidbody> passengers = new HashSet<Rigidbody>();
    private readonly List<Rigidbody> removeBuffer = new List<Rigidbody>();
    Vector3 startPos;
    bool startPosCached;

    void Start()
    {
        if (!startPosCached)
        {
            startPos = transform.position;
            startPosCached = true;
        }
        if (goalTransform == null && !string.IsNullOrEmpty(goalObjectName))
        {
            var goalObj = GameObject.Find(goalObjectName);
            if (goalObj != null) goalTransform = goalObj.transform;
        }

        if (!scanAtStart) return;

        foreach (var p in FindObjectsOfType<TerrainPassenger>(true))
        {
            if (IsIgnoredPassenger(p.gameObject)) continue;
            var rb = p.GetComponent<Rigidbody>();
            if (rb != null) passengers.Add(rb);
        }
    }

    void FixedUpdate()
    {
        if (!movementEnabled) return;
        Vector3 delta = new Vector3(0f, 0f, -speed * Time.fixedDeltaTime);

        // Terrainを動かす
        transform.position += delta;

        MoveTarget(goalTransform, delta);
        if (extraMoveTargets != null)
        {
            for (int i = 0; i < extraMoveTargets.Length; i++)
            {
                MoveTarget(extraMoveTargets[i], delta);
            }
        }

        // Passengerを同じだけ動かす
        removeBuffer.Clear();
        foreach (var rb in passengers)
        {
            if (rb == null) { removeBuffer.Add(rb); continue; }
            if (rb.transform != null && rb.transform.IsChildOf(transform)) continue;
            rb.MovePosition(rb.position + delta);
        }
        foreach (var rb in removeBuffer) passengers.Remove(rb);
    }

    public void ResetToStart()
    {
        if (!startPosCached)
        {
            startPos = transform.position;
            startPosCached = true;
        }
        Vector3 delta = startPos - transform.position;
        transform.position = startPos;
        MoveTarget(goalTransform, delta);
        if (extraMoveTargets != null)
        {
            for (int i = 0; i < extraMoveTargets.Length; i++)
            {
                MoveTarget(extraMoveTargets[i], delta);
            }
        }
        removeBuffer.Clear();
        foreach (var rb in passengers)
        {
            if (rb == null) { removeBuffer.Add(rb); continue; }
            if (rb.transform != null && rb.transform.IsChildOf(transform)) continue;
            rb.MovePosition(rb.position + delta);
        }
        foreach (var rb in removeBuffer) passengers.Remove(rb);
    }

    void MoveTarget(Transform target, Vector3 delta)
    {
        if (target == null) return;
        if (target.IsChildOf(transform)) return;

        var rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.MovePosition(rb.position + delta);
            return;
        }

        target.position += delta;
    }

    void OnTriggerEnter(Collider other)
    {
        var passenger = other.GetComponentInParent<TerrainPassenger>();
        if (passenger == null) return;
        if (IsIgnoredPassenger(passenger.gameObject)) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) rb = passenger.GetComponent<Rigidbody>();

        if (rb != null) passengers.Add(rb);
    }

    void OnTriggerExit(Collider other)
    {
        var passenger = other.GetComponentInParent<TerrainPassenger>();
        if (passenger == null) return;
        if (IsIgnoredPassenger(passenger.gameObject)) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) rb = passenger.GetComponent<Rigidbody>();

        if (rb != null) passengers.Remove(rb);
    }

    bool IsIgnoredPassenger(GameObject obj)
    {
        if (obj == null) return false;
        if (ignorePassengerTags == null || ignorePassengerTags.Length == 0) return false;
        for (int i = 0; i < ignorePassengerTags.Length; i++)
        {
            string tag = ignorePassengerTags[i];
            if (string.IsNullOrEmpty(tag)) continue;
            if (obj.CompareTag(tag)) return true;
        }
        return false;
    }
}


