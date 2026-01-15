using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuStartButton : MonoBehaviour
{
    public string gameplaySceneName = "";
    public bool clearResultsOnStart = false;

    public void StartSequence()
    {
        ConditionSequence.ResetSequence(clearResultsOnStart);
        if (!string.IsNullOrEmpty(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}
