using System.Collections;
using UnityEngine;

public class SpikeMoveStutter : MonoBehaviour
{
    [Header("止まる/動ける時間（Inspectorで変更OK）")]
    public float immobilizeTime = 0.5f;
    public float moveTime = 0.5f;

    [Header("移動スクリプト（ここにCapsuleの移動コンポーネントを入れるのが確実）")]
    public MonoBehaviour movementComponent;

    [Header("デバッグログ")]
    public bool debugLog = true;

    Rigidbody rb;
    Coroutine routine;
    int touchingCount = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (movementComponent == null && debugLog)
        {
            Debug.LogWarning("[SpikeMoveStutter] movementComponent が未設定です。InspectorでCapsuleの移動スクリプトを入れてください。", this);
        }
    }

    bool IsSpike(Collider col)
    {
        // 当たったコライダー自身 / 親方向 / 子方向 すべて探す
        if (col.GetComponent<SpikeBall>() != null) return true;
        if (col.GetComponentInParent<SpikeBall>() != null) return true;
        if (col.GetComponentInChildren<SpikeBall>() != null) return true;

        return false;
    }

    // ---- Collision版 ----
    void OnCollisionEnter(Collision c)
    {
        if (!IsSpike(c.collider)) return;
        touchingCount++;
        if (debugLog) Debug.Log($"[SpikeMoveStutter] Hit Spike (CollisionEnter). count={touchingCount}", this);
        StartLoop();
    }

    void OnCollisionExit(Collision c)
    {
        if (!IsSpike(c.collider)) return;
        touchingCount = Mathf.Max(0, touchingCount - 1);
        if (debugLog) Debug.Log($"[SpikeMoveStutter] Leave Spike (CollisionExit). count={touchingCount}", this);
        StopLoopIfNeeded();
    }

    // ---- Trigger版 ----
    void OnTriggerEnter(Collider other)
    {
        if (!IsSpike(other)) return;
        touchingCount++;
        if (debugLog) Debug.Log($"[SpikeMoveStutter] Hit Spike (TriggerEnter). count={touchingCount}", this);
        StartLoop();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsSpike(other)) return;
        touchingCount = Mathf.Max(0, touchingCount - 1);
        if (debugLog) Debug.Log($"[SpikeMoveStutter] Leave Spike (TriggerExit). count={touchingCount}", this);
        StopLoopIfNeeded();
    }

    void StartLoop()
    {
        if (movementComponent == null) return;
        if (routine != null) return;
        routine = StartCoroutine(Loop());
    }

    void StopLoopIfNeeded()
    {
        if (touchingCount > 0) return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (movementComponent != null) movementComponent.enabled = true;
    }

    IEnumerator Loop()
    {
        while (touchingCount > 0)
        {
            // 動けない
            movementComponent.enabled = false;
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            yield return new WaitForSeconds(immobilizeTime);

            // 動ける
            movementComponent.enabled = true;
            yield return new WaitForSeconds(moveTime);
        }

        if (movementComponent != null) movementComponent.enabled = true;
        routine = null;
    }
}
