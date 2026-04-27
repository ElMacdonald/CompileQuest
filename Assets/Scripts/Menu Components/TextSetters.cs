using UnityEngine;
using TMPro;

public class TextFileReader : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI textDisplay;
    public GameObject panel;

    // Called by button events that previously used the old file-based LoadTextFile.
    // Reads from the in-memory AIFeedbackStore instead of fetching ai_feedback.txt
    // (which fails in WebGL because Application.dataPath resolves to a server URL).
    public void LoadTextFile()
    {
        DisplayFeedback(AIFeedbackStore.feedback);
    }

    // ✅ NEW: This replaces file reading
    public void DisplayFeedback(string feedback)
    {
        if (string.IsNullOrEmpty(feedback))
        {
            textDisplay.text = "No feedback received.";
        }
        else
        {
            string searchTerm = "Feedback:";
            int index = feedback.IndexOf(searchTerm);

            if (index != -1)
            {
                string afterFeedback = feedback.Substring(index + searchTerm.Length).Trim();
                textDisplay.text = afterFeedback;
            }
            else
            {
                textDisplay.text = feedback;
            }
        }

        panel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (panel != null) panel.SetActive(false);
    }
}
