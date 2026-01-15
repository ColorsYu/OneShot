using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SphereCollisionCounter : MonoBehaviour
{
    public Transform countSpheresRoot;
    public string csvFileName = "sphere_hits.csv";
    public bool logEvents = false;

    readonly HashSet<GameObject> hitSpheres = new HashSet<GameObject>();

    void OnCollisionEnter(Collision collision)
    {
        TryRegister(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        TryRegister(other);
    }

    void TryRegister(Collider other)
    {
        if (other == null || countSpheresRoot == null) return;
        Transform t = other.transform;
        if (!t.IsChildOf(countSpheresRoot)) return;

        GameObject sphere = t.gameObject;
        if (hitSpheres.Add(sphere))
        {
            Debug.Log(string.Format("[SphereCounter] Hit {0}. total={1}", sphere.name, hitSpheres.Count), this);
        }
    }

    public int GetHitCount()
    {
        return hitSpheres.Count;
    }

    public void WriteCsv()
    {
        string path = Path.Combine(Application.persistentDataPath, csvFileName);
        using (StreamWriter writer = new StreamWriter(path, false))
        {
            writer.WriteLine("count");
            writer.WriteLine(hitSpheres.Count.ToString());
        }

        if (logEvents)
        {
            Debug.Log(string.Format("[SphereCounter] CSV saved: {0}", path), this);
        }
    }
}
