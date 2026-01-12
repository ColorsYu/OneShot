using UnityEngine;

public class DestroyOnHit : MonoBehaviour
{
    public string targetTag = "Pickup";

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag(targetTag)) return;

        var rb = collision.collider.attachedRigidbody;
        if (rb != null) Destroy(rb.gameObject);
        else Destroy(collision.collider.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;

        var rb = other.attachedRigidbody;
        if (rb != null) Destroy(rb.gameObject);
        else Destroy(other.gameObject);
    }
}
