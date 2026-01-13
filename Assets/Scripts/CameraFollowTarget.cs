using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 5f, -6f);
    public float followSpeed = 10f;
    public bool useLateUpdate = true;
    public bool lockHorizontal = true;
    public bool lockRotation = true;
    public bool followEnabled = false;

    float baseX;
    Quaternion baseRotation;

    void Start()
    {
        baseX = transform.position.x;
        baseRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (useLateUpdate)
        {
            Follow(Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (!useLateUpdate)
        {
            Follow(Time.fixedDeltaTime);
        }
    }

    void Follow(float dt)
    {
        if (!followEnabled || target == null) return;

        Vector3 desired = target.position + offset;
        if (lockHorizontal)
        {
            desired.x = baseX;
        }
        transform.position = Vector3.Lerp(transform.position, desired, Mathf.Clamp01(followSpeed * dt));
        if (lockRotation)
        {
            transform.rotation = baseRotation;
        }
        else
        {
            transform.LookAt(target.position);
        }
    }
}
