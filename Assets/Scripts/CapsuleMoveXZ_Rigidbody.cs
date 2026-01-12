using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CapsuleMoveXZ_Rigidbody : MonoBehaviour
{
    public float speed = 5f;
    public float deadZone = 0.2f;

    public bool movementEnabled = true; // š’Ç‰ÁFŠO•”‚©‚çˆÚ“®ON/OFF‚Å‚«‚é

    Rigidbody rb;
    float fixedY;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fixedY = rb.position.y;
    }

    void FixedUpdate()
    {
        if (!movementEnabled) return; // š’Ç‰ÁF“®‚¯‚È‚¢‚Æ‚«‚Í“ü—Í–³‹

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(x, 0f, z);
        if (input.magnitude < deadZone) return;

        Vector3 dir = input.normalized;

        Vector3 next = rb.position + dir * speed * Time.fixedDeltaTime;
        next.y = fixedY;
        rb.MovePosition(next);
    }
}
