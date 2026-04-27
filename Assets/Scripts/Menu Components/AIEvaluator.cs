using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

// WebGL-safe AI evaluator.
// Replaces the Python scripts (inputs.py / parsons.py) by calling the Groq API
// directly over HTTP — no file I/O, no Process.Start, works in browser builds.
//
// HOW TO USE IN THE INSPECTOR:
//   1. Add this component to the PYTHON RUNNER GameObject (alongside PythonRunner).
//   2. Set your Groq API key in the groqApiKey field.
//   3. In the sampleSolution field, paste this level's full sample solution text
//      (include "Problem: ..." on the first line if it's a coding level).
//   4. Check isParsons if this is a drag-and-drop Parsons level.
//   5. Assign the scene's TextFileReader component to the textFileReader field.
//   6. On the Submit button:
//        - Keep: ParsonsFileReading.WriteDropzones  (or TMPToFileWriter.SaveTextToFile)
//        - Keep: PythonRunner.RunPython             (no-op in WebGL, still runs on desktop)
//        - Keep: GameObject.SetActive(true)         (shows the feedback panel)
//      In WebGL the panel will appear immediately showing "Thinking..."
//      and the text will update when the API responds.
//
// On DESKTOP (Editor / standalone), RunPython still runs as before and
// this script does nothing in non-WebGL builds.

public class AIEvaluator : MonoBehaviour
{
#if UNITY_WEBGL
    [Header("Groq API Key")]
    [Tooltip("Your Groq API key — get one free at https://console.groq.com")]
    public string groqApiKey = "";

    [Header("Problem Setup")]
    [Tooltip("Full sample solution text for this level. For coding levels include 'Problem: ...' on line 1.")]
    [TextArea(4, 14)]
    public string sampleSolution = "";

    [Tooltip("Check this for Parsons (drag-and-drop) levels.")]
    public bool isParsons = false;

    [Header("Scene References")]
    [Tooltip("Drag the TEXT SETTER GameObject's TextFileReader component here.")]
    public TextFileReader textFileReader;

    [Tooltip("Optional status label (e.g. inside the feedback panel) shown while waiting.")]
    public TextMeshProUGUI statusText;

    private const string GroqEndpoint = "https://api.groq.com/openai/v1/chat/completions";
    private const string Model = "llama-3.3-70b-versatile";

    // Called by the Submit button.
    public void RunEvaluation()
    {
        StartCoroutine(EvaluateCoroutine());
    }

    IEnumerator EvaluateCoroutine()
    {
        if (string.IsNullOrWhiteSpace(groqApiKey))
        {
            Debug.LogError("[AIEvaluator] groqApiKey is not set in the Inspector.");
            yield break;
        }

        // --- Build player input string ---
        string playerInput;
        if (isParsons)
        {
            string[] storedLines = PythonInputStore.lines;
            playerInput = (storedLines != null && storedLines.Length > 0)
                ? string.Join("\n", storedLines)
                : "(no lines submitted)";
        }
        else
        {
            playerInput = string.IsNullOrEmpty(PythonInputStore.text)
                ? "(no code submitted)"
                : PythonInputStore.text;
        }

        string prompt = isParsons
            ? BuildParsonsPrompt(playerInput, sampleSolution)
            : BuildCodingPrompt(playerInput, sampleSolution);

        string requestBody = BuildGroqRequestBody(prompt);

        if (statusText != null) statusText.text = "Thinking...";

        // --- Send HTTP request to Groq ---
        using UnityWebRequest req = new UnityWebRequest(GroqEndpoint, "POST");
        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(requestBody);
        req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + groqApiKey);

        yield return req.SendWebRequest();

        if (statusText != null) statusText.text = "";

        if (req.result == UnityWebRequest.Result.Success)
        {
            string feedback = ParseGroqResponse(req.downloadHandler.text);
            AIFeedbackStore.feedback = feedback;
            Debug.Log("[AIEvaluator] Feedback stored successfully.");
        }
        else
        {
            string err = "(AI Error: " + req.error + ")";
            AIFeedbackStore.feedback = err;
            Debug.LogError("[AIEvaluator] " + err + "\n" + req.downloadHandler.text);
        }

        // --- Display feedback via TextFileReader.DisplayFeedback ---
        if (textFileReader != null)
            textFileReader.DisplayFeedback(AIFeedbackStore.feedback);
        else
            Debug.LogWarning("[AIEvaluator] textFileReader is not assigned. Assign it in the Inspector.");
    }

    // ---------------------------------------------------------------
    // JSON request body for Groq's OpenAI-compatible endpoint
    // ---------------------------------------------------------------
    string BuildGroqRequestBody(string userPrompt)
    {
        string systemEscaped = EscapeJson("You are a middle school Python teacher.");
        string userEscaped   = EscapeJson(userPrompt);

        return "{\"model\":\"" + Model + "\",\"max_tokens\":200,\"temperature\":0.2," +
               "\"messages\":[" +
               "{\"role\":\"system\",\"content\":\"" + systemEscaped + "\"}," +
               "{\"role\":\"user\",\"content\":\"" + userEscaped + "\"}" +
               "]}";
    }

    // ---------------------------------------------------------------
    // Parse the "content" field from Groq's JSON response
    // {"choices":[{"message":{"content":"..."}}]}
    // ---------------------------------------------------------------
    string ParseGroqResponse(string json)
    {
        const string key = "\"content\"";
        int start = json.IndexOf(key);
        if (start < 0) return "(Could not parse AI response)";

        start += key.Length;
        while (start < json.Length && json[start] != '"') start++;
        if (start >= json.Length) return "(Could not parse AI response)";
        start++; // step past opening quote

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        while (start < json.Length)
        {
            char c = json[start];
            if (c == '\\' && start + 1 < json.Length)
            {
                char next = json[start + 1];
                switch (next)
                {
                    case '"':  sb.Append('"');  start += 2; break;
                    case '\\': sb.Append('\\'); start += 2; break;
                    case 'n':  sb.Append('\n'); start += 2; break;
                    case 'r':  sb.Append('\r'); start += 2; break;
                    case 't':  sb.Append('\t'); start += 2; break;
                    default:   sb.Append(c);   start++;    break;
                }
            }
            else if (c == '"') break;
            else { sb.Append(c); start++; }
        }
        return sb.ToString().Trim();
    }

    // ---------------------------------------------------------------
    // Prompts
    // ---------------------------------------------------------------
    string BuildCodingPrompt(string playerInput, string sample)
    {
        return
"You are an AI tutor for a young beginner (age 8-12).\n\n" +
"The game provides two pieces of text:\n\n" +
"FILE #1 - Player Input:\n(Problem + student's code)\n" +
"=====================\n" + playerInput + "\n=====================\n\n" +
"FILE #2 - Sample Solution:\n(Problem + correct code)\n" +
"=====================\n" + sample + "\n=====================\n\n" +
"TASK:\nYou MUST follow these rules exactly:\n\n" +
"1. Output ONLY these three sections, in this exact order:\n" +
"   Problem:\n   Your Code:\n   Feedback:\n\n" +
"2. Include NOTHING before, after, or between those sections.\n" +
"   - No greetings\n   - No introductions\n   - No \"Okay!\" or \"Let's see\"\n" +
"   - No emojis\n   - No extra commentary\n\n" +
"3. Do not output the sample solution code.\n" +
"4. Compare the student's code to the problem and the sample solution.\n" +
"5. Decide if the student solved the problem correctly.\n" +
"6. If correct: Praise them briefly. Explain why in simple words.\n" +
"7. If incorrect: Give short, simple hints. Explain every incorrect piece. " +
"Suggest what to try next. Do NOT reveal the correct answer.\n" +
"8. Keep the feedback VERY short, friendly, and kid-safe.\n" +
"9. Keep the preamble to a minimum.\n" +
"10. Do NOT use the sample solution code in your response.\n" +
"11. With numbers, don't give exact values; use approximate terms like \"more\" or \"less\".\n\n" +
"You must ONLY produce the required three sections and nothing else.";
    }

    string BuildParsonsPrompt(string playerInput, string sample)
    {
        return
"You are an AI tutor for a young beginner (age 8-12).\n\n" +
"This is a PARSONS PROBLEM. The student reorders shuffled lines of code.\n\n" +
"FILE #1 - Player Input (student's chosen order of lines):\n" +
"=====================\n" + playerInput + "\n=====================\n\n" +
"FILE #2 - Sample Solution (correct line order):\n" +
"=====================\n" + sample + "\n=====================\n\n" +
"TASK:\n" +
"1. Output ONLY these three sections in this exact order:\n" +
"   Problem:\n   Your Code:\n   Feedback:\n\n" +
"2. Include NOTHING before, after, or between those sections.\n" +
"3. Do NOT output the correct solution code.\n" +
"4. Use the problem description found inside the sample solution.\n" +
"5. Compare the student's code order to the correct order.\n" +
"6. If correct: Praise them briefly. Explain why in simple words.\n" +
"7. If incorrect: Give short, simple hints. Explain what is out of order. " +
"Suggest what to try next. Do NOT reveal the correct order.\n" +
"8. Keep everything very short, friendly, and kid-safe.\n\n" +
"You must ONLY produce the required three sections and nothing else.";
    }

    static string EscapeJson(string s)
    {
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

#else
    // ---------------------------------------------------------------
    // Desktop / Editor stub — this component does nothing outside WebGL.
    // RunPython.cs already handles the desktop flow.
    // ---------------------------------------------------------------
    public string groqApiKey = "";
    [TextArea(4, 14)] public string sampleSolution = "";
    public bool isParsons = false;
    public TextFileReader textFileReader;
    public TextMeshProUGUI statusText;

    public void RunEvaluation() =>
        Debug.Log("[AIEvaluator] RunEvaluation() is a no-op outside WebGL — desktop uses RunPython.");
#endif
}
