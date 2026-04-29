using UnityEngine;
#if !UNITY_WEBGL
using System.Diagnostics;
using System.IO;
#endif

// Process.Start is not supported in WebGL, so this whole script is compiled out for those builds.
public class RunPythonFile : MonoBehaviour
{
#if !UNITY_WEBGL
    [Header("Path to your Python file")]
    public string pythonFilePath = "C:/path/to/your/script.py";

    void Start()
    {
        RunPythonScript();
    }

    public void RunPythonScript()
    {
        if (!File.Exists(pythonFilePath))
        {
            UnityEngine.Debug.LogError("Python file not found: " + pythonFilePath);
            return;
        }

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "python";
        psi.Arguments = $"\"{pythonFilePath}\"";
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.CreateNoWindow = true;

        try
        {
            Process process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(output))
                UnityEngine.Debug.Log("Python output:\n" + output);
            if (!string.IsNullOrEmpty(errors))
                UnityEngine.Debug.LogError("Python errors:\n" + errors);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Error running Python script: " + e.Message);
        }
    }
#else
    // WebGL stub
    [Header("Path to your Python file (unused in WebGL)")]
    public string pythonFilePath = "";
    void Start() =>
        UnityEngine.Debug.Log("[RunPythonFile] Disabled in WebGL build.");
    public void RunPythonScript() =>
        UnityEngine.Debug.Log("[RunPythonFile] RunPythonScript() is a no-op in WebGL.");
#endif
}
