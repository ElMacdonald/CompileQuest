using System.Collections.Generic;
using UnityEngine;
using TMPro;

// WebGL-safe: uses PythonInputStore instead of file I/O.
// Application.dataPath is a server URL in WebGL.
public class ParsonsFileReading : MonoBehaviour
{
    public GameObject[] dropZones;

    // Reads each drop zone's text and writes it to PythonInputStore
    public void WriteDropzones()
    {
        List<string> lines = new List<string>();

        foreach (GameObject zone in dropZones)
        {
            TextMeshProUGUI tmp = zone.GetComponentInChildren<TextMeshProUGUI>();
            lines.Add(tmp != null ? tmp.text : "");
        }

        PythonInputStore.lines = lines.ToArray();
        Debug.Log("[ParsonsFileReading] Wrote " + lines.Count + " lines to PythonInputStore.");
    }

    // Reads PythonInputStore back into drop zones
    public void ReadDropzones()
    {
        string[] lines = PythonInputStore.lines;

        for (int i = 0; i < dropZones.Length && i < lines.Length; i++)
        {
            TextMeshProUGUI tmp = dropZones[i].GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = lines[i];
        }

        Debug.Log("[ParsonsFileReading] Read " + lines.Length + " lines from PythonInputStore.");
    }
}
