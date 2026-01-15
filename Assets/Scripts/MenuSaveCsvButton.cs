using System;
using System.IO;
using UnityEngine;

public class MenuSaveCsvButton : MonoBehaviour
{
    public string csvFileName = "sphere_results.csv";

    public void SaveCsv()
    {
        string uniqueFileName = BuildUniqueFileName(csvFileName);
        ConditionSequence.WriteCsv(uniqueFileName);
    }

    static string BuildUniqueFileName(string fileName)
    {
        string directory = Path.GetDirectoryName(fileName);
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".csv";
        }

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string candidateName = string.Format("{0}_{1}{2}", baseName, stamp, extension);
        string combinedName = string.IsNullOrEmpty(directory)
            ? candidateName
            : Path.Combine(directory, candidateName);
        string path = Path.Combine(Application.persistentDataPath, combinedName);

        int index = 1;
        while (File.Exists(path))
        {
            candidateName = string.Format("{0}_{1}_{2}{3}", baseName, stamp, index, extension);
            combinedName = string.IsNullOrEmpty(directory)
                ? candidateName
                : Path.Combine(directory, candidateName);
            path = Path.Combine(Application.persistentDataPath, combinedName);
            index++;
        }

        return combinedName;
    }
}
