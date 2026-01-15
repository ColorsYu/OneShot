using UnityEngine;
using TMPro;

public class GoalTrigger : MonoBehaviour
{
    public ConditionSequence conditionSequence;
    public CapsuleMoveXZ_Rigidbody capsule;
    public SphereCollisionCounter collisionCounter;
    public TMP_Text goalText;
    public string goalMessage = "Goal";
    public string advanceButton = "Submit";

    bool waitingForAdvance;

    void Awake()
    {
        if (conditionSequence == null)
        {
            conditionSequence = FindObjectOfType<ConditionSequence>();
        }
        if (capsule == null)
        {
            capsule = FindObjectOfType<CapsuleMoveXZ_Rigidbody>();
        }
        if (collisionCounter == null)
        {
            collisionCounter = FindObjectOfType<SphereCollisionCounter>();
        }
        if (goalText != null)
        {
            goalText.text = string.Empty;
        }
    }

    void Update()
    {
        if (!waitingForAdvance) return;
        if (Input.GetButtonDown(advanceButton))
        {
            waitingForAdvance = false;
            if (goalText != null) goalText.text = string.Empty;
            int hitCount = collisionCounter != null ? collisionCounter.GetHitCount() : 0;
            conditionSequence.RecordAndAdvance(hitCount);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        TryTrigger(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        TryTrigger(other);
    }

    void TryTrigger(Collider other)
    {
        if (waitingForAdvance) return;
        if (other == null || conditionSequence == null) return;

        CapsuleMoveXZ_Rigidbody hitCapsule = other.GetComponentInParent<CapsuleMoveXZ_Rigidbody>();
        if (hitCapsule == null) return;
        if (capsule != null && hitCapsule != capsule) return;

        waitingForAdvance = true;
        if (goalText != null) goalText.text = goalMessage;

        if (hitCapsule != null)
        {
            hitCapsule.movementEnabled = false;
            Rigidbody rb = hitCapsule.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
