using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConditionSequence : MonoBehaviour
{
    struct Condition
    {
        public float stopDuration;
        public float moveDuration;
    }

    public CapsuleMoveXZ_Rigidbody capsule;
    public string nextSceneName = "";
    public string menuSceneName = "";
    public bool shuffle = true;
    public bool useRandomSeed = true;
    public int fixedSeed = 12345;
    public bool logEvents = false;

    static List<Condition> sequence;
    static List<ResultRecord> results;
    static int currentIndex = -1;
    static bool initialized;
    static bool completed;

    struct ResultRecord
    {
        public string sceneName;
        public string conditionLabel;
        public int hitCount;
    }

    void Awake()
    {
        EnsureSequence();
        ResolveCapsule();
    }

    void Start()
    {
        ApplyCurrentCondition();
    }

    public void Advance()
    {
        if (completed) return;
        currentIndex++;
        if (currentIndex >= sequence.Count)
        {
            completed = true;
            if (logEvents) Debug.Log("[ConditionSequence] Completed all conditions.", this);
            if (capsule != null) capsule.movementEnabled = false;
            if (!string.IsNullOrEmpty(menuSceneName))
            {
                SceneManager.LoadScene(menuSceneName);
            }
            return;
        }

        ReloadScene();
    }

    public void RecordAndAdvance(int hitCount)
    {
        if (completed) return;
        if (sequence == null || sequence.Count == 0) return;
        if (currentIndex < 0 || currentIndex >= sequence.Count) return;

        Condition condition = sequence[currentIndex];
        string label = string.Format("Stop={0:F1} Move={1:F1}", condition.stopDuration, condition.moveDuration);
        results.Add(new ResultRecord
        {
            sceneName = SceneManager.GetActiveScene().name,
            conditionLabel = label,
            hitCount = hitCount
        });

        if (logEvents)
        {
            Debug.Log(string.Format("[ConditionSequence] Record {0} hits={1}", label, hitCount), this);
        }

        Advance();
    }

    void EnsureSequence()
    {
        if (initialized) return;

        sequence = new List<Condition>();
        if (results == null)
        {
            results = new List<ResultRecord>();
        }
        float[] values = new float[] { 0.1f, 0.3f, 0.5f };
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values.Length; j++)
            {
                sequence.Add(new Condition { stopDuration = values[i], moveDuration = values[j] });
            }
        }

        if (shuffle)
        {
            int seed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : fixedSeed;
            Shuffle(sequence, seed);
        }

        currentIndex = 0;
        completed = false;
        initialized = true;
    }

    void ApplyCurrentCondition()
    {
        if (completed) return;
        if (sequence == null || sequence.Count == 0) return;
        if (currentIndex < 0 || currentIndex >= sequence.Count) return;

        ResolveCapsule();
        if (capsule == null) return;

        Condition condition = sequence[currentIndex];
        capsule.stopDuration = condition.stopDuration;
        capsule.moveDuration = condition.moveDuration;
        capsule.ResetForCondition();

        if (logEvents)
        {
            Debug.Log(string.Format("[ConditionSequence] Apply index={0} stop={1:F1} move={2:F1}",
                currentIndex + 1, condition.stopDuration, condition.moveDuration), this);
        }

        if (capsule.startWithCountdown)
        {
            capsule.BeginCountdown();
        }
        else
        {
            capsule.movementEnabled = true;
        }
    }

    void ReloadScene()
    {
        string sceneName = string.IsNullOrEmpty(nextSceneName)
            ? SceneManager.GetActiveScene().name
            : nextSceneName;
        SceneManager.LoadScene(sceneName);
    }

    void ResolveCapsule()
    {
        if (capsule == null)
        {
            capsule = FindObjectOfType<CapsuleMoveXZ_Rigidbody>();
        }
    }

    public static void ResetSequence(bool clearResults = true)
    {
        initialized = false;
        completed = false;
        currentIndex = -1;
        if (sequence != null) sequence.Clear();
        sequence = null;
        if (clearResults && results != null)
        {
            results.Clear();
        }
        if (clearResults) results = null;
    }

    public static void WriteCsv(string fileName = "sphere_results.csv")
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (results == null)
        {
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                writer.WriteLine("scene,condition,hits");
            }
            Debug.Log(string.Format("[ConditionSequence] CSV saved (no results): {0}", path));
            return;
        }

        using (StreamWriter writer = new StreamWriter(path, false))
        {
            writer.WriteLine("scene,condition,hits");
            for (int i = 0; i < results.Count; i++)
            {
                ResultRecord r = results[i];
                writer.WriteLine(string.Format("{0},{1},{2}", r.sceneName, r.conditionLabel, r.hitCount));
            }
        }
        Debug.Log(string.Format("[ConditionSequence] CSV saved: {0} rows={1}", path, results.Count));
    }

    static void Shuffle(List<Condition> list, int seed)
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
