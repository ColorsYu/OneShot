using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ConditionSequenceManager : MonoBehaviour
{
    struct Condition
    {
        public float stopDuration;
        public float moveDuration;
    }

    public CapsuleMoveXZ_Rigidbody player;
    public TerrainAutoMoveZ_MultiPassengers terrainMover;
    public Transform spawnPoint;
    public string advanceButton = "Submit";
    public KeyCode advanceKey = KeyCode.JoystickButton0;
    public bool autoAdvanceOnGoal = false;
    public float autoAdvanceDelay = 0f;

    [Header("UI")]
    public GameObject goalUiRoot;
    public TMP_Text conditionText;

    [Header("Randomize")]
    public bool shuffleConditions = true;
    public bool useRandomSeed = true;
    public int fixedSeed = 12345;

    public bool logEvents = false;

    readonly List<Condition> conditions = new List<Condition>();
    int currentConditionIndex = -1;
    bool waitingForAdvance;
    Rigidbody playerRb;
    Vector3 fallbackSpawnPos;
    Quaternion fallbackSpawnRot;
    bool spawnCached;
    Vector3 fixedSpawnPos;
    Quaternion fixedSpawnRot;
    bool fixedSpawnCached;
    Coroutine resumeTerrainCoroutine;

    void Awake()
    {
        InitializeConditions();
        ResolvePlayer();
        ResolveTerrainMover();
        CacheFixedSpawn();
        CacheFallbackSpawn();
        SetGoalUi(false);
    }

    void Start()
    {
        AdvanceCondition();
    }

    void Update()
    {
        if (!waitingForAdvance) return;
        if (Input.GetButtonDown(advanceButton) || Input.GetKeyDown(advanceKey))
        {
            waitingForAdvance = false;
            SetGoalUi(false);
            AdvanceCondition();
        }
    }

    public void OnGoalReached()
    {
        if (waitingForAdvance) return;
        if (logEvents) Debug.Log("[ConditionSequence] OnGoalReached", this);
        SetGoalUi(true);
        SetTerrainMove(false);

        var capsule = ResolvePlayer();
        if (capsule != null)
        {
            capsule.movementEnabled = false;
            if (playerRb != null)
            {
                playerRb.velocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }
        }

        if (autoAdvanceOnGoal)
        {
            StartCoroutine(AutoAdvanceRoutine());
            return;
        }

        waitingForAdvance = true;
    }

    System.Collections.IEnumerator AutoAdvanceRoutine()
    {
        if (autoAdvanceDelay > 0f)
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
        }
        SetGoalUi(false);
        AdvanceCondition();
    }

    void InitializeConditions()
    {
        conditions.Clear();
        float[] values = new float[] { 0.1f, 0.3f, 0.5f };
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values.Length; j++)
            {
                conditions.Add(new Condition { stopDuration = values[i], moveDuration = values[j] });
            }
        }

        if (shuffleConditions)
        {
            int seed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : fixedSeed;
            Shuffle(conditions, seed);
        }
    }

    void AdvanceCondition()
    {
        currentConditionIndex++;
        if (currentConditionIndex >= conditions.Count)
        {
            if (logEvents) Debug.Log("[ConditionSequence] All conditions complete.", this);
            waitingForAdvance = false;
            SetGoalUi(false);
            var capsule = ResolvePlayer();
            if (capsule != null)
            {
                capsule.movementEnabled = false;
            }
            return;
        }

        ApplyCondition(conditions[currentConditionIndex]);
    }

    void ApplyCondition(Condition condition)
    {
        if (logEvents)
        {
            Debug.Log(string.Format("[ConditionSequence] ApplyCondition stop={0:F1} move={1:F1}", condition.stopDuration, condition.moveDuration), this);
        }
        UpdateConditionText(condition);

        var capsule = ResolvePlayer();
        if (capsule == null) return;
        capsule.ResetForCondition();
        SetTerrainMove(false);
        ResetTerrainPosition();
        capsule.stopDuration = condition.stopDuration;
        capsule.moveDuration = condition.moveDuration;

        ResetPlayerPosition(capsule);

        if (capsule.startWithCountdown)
        {
            capsule.BeginCountdown();
        }
        else
        {
            capsule.movementEnabled = true;
        }
        StartResumeTerrainRoutine(capsule);
    }

    void ResetPlayerPosition(CapsuleMoveXZ_Rigidbody capsule)
    {
        Vector3 pos;
        Quaternion rot;
        if (fixedSpawnCached)
        {
            pos = fixedSpawnPos;
            rot = fixedSpawnRot;
        }
        else if (spawnPoint != null)
        {
            pos = spawnPoint.position;
            rot = spawnPoint.rotation;
        }
        else
        {
            CacheFallbackSpawn();
            pos = fallbackSpawnPos;
            rot = fallbackSpawnRot;
        }

        if (logEvents)
        {
            Debug.Log(string.Format("[ConditionSequence] ResetPlayerPosition from={0} to={1}", capsule.transform.position, pos), this);
        }
        capsule.TeleportTo(pos, rot);
    }

    CapsuleMoveXZ_Rigidbody ResolvePlayer()
    {
        if (player == null)
        {
            player = FindObjectOfType<CapsuleMoveXZ_Rigidbody>();
        }
        if (player != null && playerRb == null)
        {
            playerRb = player.GetComponent<Rigidbody>();
        }
        return player;
    }

    void ResolveTerrainMover()
    {
        if (terrainMover == null)
        {
            terrainMover = FindObjectOfType<TerrainAutoMoveZ_MultiPassengers>();
        }
    }

    void SetTerrainMove(bool enabled)
    {
        if (terrainMover != null) terrainMover.movementEnabled = enabled;
    }

    void ResetTerrainPosition()
    {
        if (terrainMover != null) terrainMover.ResetToStart();
    }

    void StartResumeTerrainRoutine(CapsuleMoveXZ_Rigidbody capsule)
    {
        if (resumeTerrainCoroutine != null)
        {
            StopCoroutine(resumeTerrainCoroutine);
        }
        resumeTerrainCoroutine = StartCoroutine(ResumeTerrainWhenReady(capsule));
    }

    System.Collections.IEnumerator ResumeTerrainWhenReady(CapsuleMoveXZ_Rigidbody capsule)
    {
        while (capsule != null && !capsule.movementEnabled)
        {
            yield return null;
        }
        SetTerrainMove(true);
        resumeTerrainCoroutine = null;
    }

    void CacheFallbackSpawn()
    {
        if (spawnCached) return;
        var capsule = ResolvePlayer();
        if (capsule == null) return;
        fallbackSpawnPos = capsule.transform.position;
        fallbackSpawnRot = capsule.transform.rotation;
        spawnCached = true;
    }

    void CacheFixedSpawn()
    {
        if (fixedSpawnCached) return;
        if (spawnPoint == null) return;
        fixedSpawnPos = spawnPoint.position;
        fixedSpawnRot = spawnPoint.rotation;
        fixedSpawnCached = true;
    }

    void SetGoalUi(bool visible)
    {
        if (goalUiRoot != null)
        {
            goalUiRoot.SetActive(visible);
        }
    }

    void UpdateConditionText(Condition condition)
    {
        if (conditionText == null) return;
        conditionText.text = string.Format("Stop={0:F1}  Move={1:F1}", condition.stopDuration, condition.moveDuration);
    }

    void Shuffle(List<Condition> list, int seed)
    {
        System.Random rng = new System.Random(seed);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swap = rng.Next(i + 1);
            Condition tmp = list[swap];
            list[swap] = list[i];
            list[i] = tmp;
        }
    }
}
