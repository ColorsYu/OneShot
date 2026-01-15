using UnityEngine;

public class MenuSaveCsvButton : MonoBehaviour
{
    public string csvFileName = "sphere_results.csv";

    public void SaveCsv()
    {
        ConditionSequence.WriteCsv(csvFileName);
    }
}
