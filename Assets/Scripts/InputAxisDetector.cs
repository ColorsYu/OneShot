using UnityEngine;

public class InputAxisDetector : MonoBehaviour
{
    [Header("Axis range (1-28). Unity Input Manager joystick axes are 1-based.")]
    public int maxAxis = 12;

    [Header("Log every N frames")]
    public int logEveryFrames = 15;

    [Header("Only log when abs(value) >= threshold")]
    public float threshold = 0.2f;

    int frameCounter;
    bool missingAxisSetup;

    void Update()
    {
        frameCounter++;
        if (frameCounter % logEveryFrames != 0) return;

        for (int i = 1; i <= maxAxis; i++)
        {
            string axisName = "joystick axis " + i;
            float v;
            try
            {
                v = Input.GetAxisRaw(axisName);
            }
            catch (System.ArgumentException)
            {
                if (!missingAxisSetup)
                {
                    missingAxisSetup = true;
                    Debug.LogWarning(
                        "[AxisDetector] 'joystick axis N' is not set up in Input Manager. " +
                        "Create axes with those names or disable this component.",
                        this);
                }
                enabled = false;
                return;
            }
            if (Mathf.Abs(v) >= threshold)
            {
                Debug.Log(string.Format("[AxisDetector] {0} = {1:F3}", axisName, v), this);
            }
        }
    }
}
