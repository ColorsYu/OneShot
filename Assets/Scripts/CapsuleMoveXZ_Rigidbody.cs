using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class CapsuleMoveXZ_Rigidbody : MonoBehaviour
{
    public float speed = 5f;
    public float deadZone = 0.2f;
    public string horizontalAxisName = "Horizontal";
    public string verticalAxisName = "Vertical";
    public bool invertVertical = true;
    public bool freezeRotationDuringMove = true;
    public bool restrictToHorizontal = true;

    public bool movementEnabled = true; // ??????????ON/OFF???

    public bool startWithCountdown = true;
    public int countdownFrom = 3;
    public float countdownInterval = 1.0f;
    public TMP_Text countdownText;
    public bool logCountdown = false;

    public float stopDuration = 0.5f;
    public float moveDuration = 1.0f;

    public float knockbackForce = 6f;
    public float knockbackUp = 1.5f;
    public float knockbackTorque = 25f;
    public float knockbackDuration = 0.25f;
    public float knockbackHoldDuration = 1.0f;
    public float standUpDuration = 0.2f;
    public float maxTiltAngle = 60f;
    public bool setTriggerDuringKnockback = true;

    public bool blinkOnKnockback = true;
    public int blinkCount = 3;
    public float blinkInterval = 0.1f;

    public bool useAxisCalibration = true;
    public float calibrationDuration = 0.2f;
    public bool debugInput = false;
    public float debugLogInterval = 0.2f;

    Rigidbody rb;
    Collider capsuleCollider;
    Renderer capsuleRenderer;
    Coroutine stopMoveCoroutine;
    Coroutine knockbackCoroutine;
    Coroutine blinkCoroutine;
    readonly System.Collections.Generic.HashSet<Collider> ignoredSpikeColliders = new System.Collections.Generic.HashSet<Collider>();

    float fixedY;
    Vector2 axisOffset;
    bool axisCalibrated;
    Vector2 moveInput;
    float debugLogTimer;

    bool isKnockbacking;
    bool isCountingDown;
    RigidbodyConstraints originalConstraints;
    RigidbodyConstraints movementConstraints;
    bool originalCapsuleTrigger;

    float GetAxisSafe(string axisName)
    {
        try
        {
            return Input.GetAxisRaw(axisName);
        }
        catch (System.ArgumentException)
        {
            return 0f;
        }
    }

    float GetSignedAngle(float angle)
    {
        if (angle > 180f) return angle - 360f;
        return angle;
    }

    void ClampTilt()
    {
        Vector3 euler = rb.rotation.eulerAngles;
        float x = GetSignedAngle(euler.x);
        if (Mathf.Abs(x) > maxTiltAngle)
        {
            float clampedX = Mathf.Clamp(x, -maxTiltAngle, maxTiltAngle);
            rb.rotation = Quaternion.Euler(clampedX, euler.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<Collider>();
        capsuleRenderer = GetComponent<Renderer>();
        fixedY = rb.position.y;

        originalConstraints = rb.constraints;
        movementConstraints = originalConstraints;
        if (freezeRotationDuringMove)
        {
            movementConstraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        rb.constraints = movementConstraints;

        originalCapsuleTrigger = capsuleCollider != null ? capsuleCollider.isTrigger : false;
    }

    void Start()
    {
        if (startWithCountdown)
        {
            movementEnabled = false;
            StartCoroutine(StartupRoutine());
            return;
        }

        if (!useAxisCalibration)
        {
            axisCalibrated = true;
            return;
        }

        string hName = string.IsNullOrEmpty(horizontalAxisName) ? "Horizontal" : horizontalAxisName;
        string vName = string.IsNullOrEmpty(verticalAxisName) ? "Vertical" : verticalAxisName;
        float startX = GetAxisSafe(hName);
        float startZ = GetAxisSafe(vName);
        if (Mathf.Abs(startX) > deadZone || Mathf.Abs(startZ) > deadZone)
        {
            axisOffset = Vector2.zero;
            axisCalibrated = true;
            return;
        }

        StartCoroutine(CalibrateAxisRoutine());
    }

    void Update()
    {
        if (isCountingDown)
        {
            moveInput = Vector2.zero;
            return;
        }
        if (!axisCalibrated)
        {
            moveInput = Vector2.zero;
            return;
        }

        string hName = string.IsNullOrEmpty(horizontalAxisName) ? "Horizontal" : horizontalAxisName;
        string vName = string.IsNullOrEmpty(verticalAxisName) ? "Vertical" : verticalAxisName;
        float x = GetAxisSafe(hName) - axisOffset.x;
        float z = GetAxisSafe(vName) - axisOffset.y;

        if (invertVertical) z = -z;

        if (restrictToHorizontal) z = 0f;

        x = Mathf.Clamp(x, -1f, 1f);
        z = Mathf.Clamp(z, -1f, 1f);

        if (Mathf.Abs(x) < deadZone) x = 0f;
        if (Mathf.Abs(z) < deadZone) z = 0f;

        moveInput = new Vector2(x, z);

        if (debugInput)
        {
            debugLogTimer += Time.unscaledDeltaTime;
            if (debugLogTimer >= debugLogInterval)
            {
                debugLogTimer = 0f;
                Debug.Log(string.Format("[CapsuleMoveXZ] input=({0:F3},{1:F3}) vel={2} enabled={3}", moveInput.x, moveInput.y, rb.velocity, movementEnabled), this);
            }
        }
    }

    void FixedUpdate()
    {
        if (debugInput)
        {
            Debug.Log("[CapsuleMoveXZ] FixedUpdate", this);
        }
        if (isKnockbacking)
        {
            return;
        }
        if (rb.constraints != movementConstraints)
        {
            rb.constraints = movementConstraints;
        }
        if (!movementEnabled)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        if (input.sqrMagnitude <= 0f)
        {
            Vector3 v = rb.velocity;
            v.x = 0f;
            v.z = 0f;
            rb.velocity = v;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        Vector3 dir = input.normalized;

        Vector3 next = rb.position + dir * speed * Time.fixedDeltaTime;
        next.y = fixedY;
        if (debugInput)
        {
            Debug.Log(string.Format("[CapsuleMoveXZ] MovePosition from={0} to={1}", rb.position, next), this);
        }
        rb.MovePosition(next);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleSpikeHit(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleSpikeHit(other);
    }

    void HandleSpikeHit(Collider other)
    {
        if (!TryGetSpikeRoot(other, out Transform root)) return;

        if (capsuleCollider != null)
        {
            Collider[] spikeColliders = root.GetComponentsInChildren<Collider>();
            foreach (var col in spikeColliders)
            {
                if (col == null) continue;
                Physics.IgnoreCollision(capsuleCollider, col, true);
                ignoredSpikeColliders.Add(col);
            }
        }

        StartStopMoveRoutine();
        StartKnockback();
    }

    bool TryGetSpikeRoot(Collider other, out Transform root)
    {
        root = null;
        if (other == null) return false;

        if (other.CompareTag("SpikeBall"))
        {
            root = other.transform;
            return true;
        }

        Transform current = other.transform.parent;
        while (current != null)
        {
            if (current.CompareTag("SpikeBall"))
            {
                root = current;
                return true;
            }
            current = current.parent;
        }

        return false;
    }

    void StartStopMoveRoutine()
    {
        if (stopMoveCoroutine != null) return;
        stopMoveCoroutine = StartCoroutine(StopMoveRoutine());
    }

    IEnumerator StopMoveRoutine()
    {
        while (true)
        {
            movementEnabled = false;
            float stopElapsed = 0f;
            while (stopElapsed < stopDuration)
            {
                stopElapsed += Time.deltaTime;
                yield return null;
            }

            movementEnabled = true;
            float moveElapsed = 0f;
            while (moveElapsed < moveDuration)
            {
                moveElapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    IEnumerator CalibrateAxisRoutine()
    {
        float elapsed = 0f;
        int count = 0;
        Vector2 sum = Vector2.zero;
        while (elapsed < calibrationDuration)
        {
            string hName = string.IsNullOrEmpty(horizontalAxisName) ? "Horizontal" : horizontalAxisName;
            string vName = string.IsNullOrEmpty(verticalAxisName) ? "Vertical" : verticalAxisName;
            sum.x += GetAxisSafe(hName);
            sum.y += GetAxisSafe(vName);
            count++;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        axisOffset = (count > 0) ? (sum / count) : Vector2.zero;
        axisCalibrated = true;
    }

    IEnumerator StartupRoutine()
    {
        if (!useAxisCalibration)
        {
            axisCalibrated = true;
        }
        else
        {
            string hName = string.IsNullOrEmpty(horizontalAxisName) ? "Horizontal" : horizontalAxisName;
            string vName = string.IsNullOrEmpty(verticalAxisName) ? "Vertical" : verticalAxisName;
            float startX = GetAxisSafe(hName);
            float startZ = GetAxisSafe(vName);
            if (Mathf.Abs(startX) > deadZone || Mathf.Abs(startZ) > deadZone)
            {
                axisOffset = Vector2.zero;
                axisCalibrated = true;
            }
            else
            {
                yield return StartCoroutine(CalibrateAxisRoutine());
            }
        }

        isCountingDown = true;
        yield return StartCoroutine(CountdownRoutine());
        isCountingDown = false;
        movementEnabled = true;
    }

    public void BeginCountdown()
    {
        StopAllCoroutines();
        stopMoveCoroutine = null;
        knockbackCoroutine = null;
        blinkCoroutine = null;
        isKnockbacking = false;
        movementEnabled = false;
        StartCoroutine(StartupRoutine());
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (rb != null)
        {
            bool wasKinematic = rb.isKinematic;
            if (!wasKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.position = position;
            rb.rotation = rotation;
            transform.position = position;
            transform.rotation = rotation;
            rb.isKinematic = wasKinematic;
            if (!wasKinematic)
            {
                rb.Sleep();
            }
        }
        else
        {
            transform.position = position;
            transform.rotation = rotation;
        }

        fixedY = position.y;
        moveInput = Vector2.zero;
        Physics.SyncTransforms();
    }

    IEnumerator CountdownRoutine()
    {
        int start = Mathf.Max(1, countdownFrom);
        if (countdownText != null) countdownText.gameObject.SetActive(true);
        for (int i = start; i >= 1; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();
            if (logCountdown) Debug.Log(string.Format("[Countdown] {0}", i), this);
            yield return new WaitForSecondsRealtime(countdownInterval);
        }
        if (countdownText != null) countdownText.text = string.Empty;
    }

    void StartKnockback()
    {
        if (knockbackCoroutine != null) return;
        knockbackCoroutine = StartCoroutine(KnockbackRoutine());
    }

    public void ResetForCondition()
    {
        if (stopMoveCoroutine != null)
        {
            StopCoroutine(stopMoveCoroutine);
            stopMoveCoroutine = null;
        }
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }
        StopBlink();
        isKnockbacking = false;
        moveInput = Vector2.zero;
        movementEnabled = false;

        if (capsuleCollider != null)
        {
            capsuleCollider.isTrigger = originalCapsuleTrigger;
        }
        RestoreSpikeCollisions();
        if (rb != null)
        {
            rb.constraints = movementConstraints;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    IEnumerator KnockbackRoutine()
    {
        isKnockbacking = true;
        movementEnabled = false;
        StartBlink();

        if (setTriggerDuringKnockback && capsuleCollider != null)
        {
            capsuleCollider.isTrigger = true;
        }

        // Allow tipping backward around X only.
        rb.constraints = originalConstraints | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        Vector3 force = -transform.forward * knockbackForce + Vector3.up * knockbackUp;
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(-transform.right * knockbackTorque, ForceMode.Impulse);

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            ClampTilt();
            yield return null;
        }

        yield return new WaitForSeconds(knockbackHoldDuration);

        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;

        // Stand up smoothly: keep current yaw, reset pitch/roll.
        Vector3 euler = rb.rotation.eulerAngles;
        Quaternion target = Quaternion.Euler(0f, euler.y, 0f);
        Quaternion startRot = rb.rotation;
        float standElapsed = 0f;
        while (standElapsed < standUpDuration)
        {
            standElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(standElapsed / Mathf.Max(standUpDuration, 0.0001f));
            rb.rotation = Quaternion.Slerp(startRot, target, t);
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            yield return null;
        }
        rb.rotation = target;

        if (setTriggerDuringKnockback && capsuleCollider != null)
        {
            capsuleCollider.isTrigger = originalCapsuleTrigger;
        }
        RestoreSpikeCollisions();

        rb.constraints = movementConstraints;
        movementEnabled = true;
        isKnockbacking = false;
        StopBlink();
        knockbackCoroutine = null;
    }

    void RestoreSpikeCollisions()
    {
        if (capsuleCollider == null) return;
        if (ignoredSpikeColliders.Count == 0) return;
        foreach (var col in ignoredSpikeColliders)
        {
            if (col == null) continue;
            Physics.IgnoreCollision(capsuleCollider, col, false);
        }
        ignoredSpikeColliders.Clear();
    }

    void StartBlink()
    {
        if (!blinkOnKnockback || capsuleRenderer == null) return;
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        if (capsuleRenderer != null) capsuleRenderer.enabled = true;
    }

    IEnumerator BlinkRoutine()
    {
        int count = Mathf.Max(1, blinkCount);
        for (int i = 0; i < count; i++)
        {
            capsuleRenderer.enabled = false;
            yield return new WaitForSeconds(blinkInterval);
            capsuleRenderer.enabled = true;
            yield return new WaitForSeconds(blinkInterval);
        }
        blinkCoroutine = null;
    }
}
