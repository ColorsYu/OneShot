using System.Collections.Generic;
using UnityEngine;

public class TerrainAutoMoveZ_MultiPassengers : MonoBehaviour
{
    public float speed = 5f;
    public bool scanAtStart = true;

    private readonly HashSet<Rigidbody> passengers = new HashSet<Rigidbody>();
    private readonly List<Rigidbody> removeBuffer = new List<Rigidbody>();

    void Start()
    {
        if (!scanAtStart) return;

        foreach (var p in FindObjectsOfType<TerrainPassenger>(true))
        {
            var rb = p.GetComponent<Rigidbody>();
            if (rb != null) passengers.Add(rb);
        }
    }

    void FixedUpdate()
    {
        Vector3 delta = new Vector3(0f, 0f, -speed * Time.fixedDeltaTime);

        // Terrain‚ð“®‚©‚·
        transform.position += delta;

        // Passenger‚ð“¯‚¶‚¾‚¯“®‚©‚·
        removeBuffer.Clear();
        foreach (var rb in passengers)
        {
            if (rb == null) { removeBuffer.Add(rb); continue; }
            rb.MovePosition(rb.position + delta);
        }
        foreach (var rb in removeBuffer) passengers.Remove(rb);
    }

    void OnTriggerEnter(Collider other)
    {
        var passenger = other.GetComponentInParent<TerrainPassenger>();
        if (passenger == null) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) rb = passenger.GetComponent<Rigidbody>();

        if (rb != null) passengers.Add(rb);
    }

    void OnTriggerExit(Collider other)
    {
        var passenger = other.GetComponentInParent<TerrainPassenger>();
        if (passenger == null) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) rb = passenger.GetComponent<Rigidbody>();

        if (rb != null) passengers.Remove(rb);
    }
}
