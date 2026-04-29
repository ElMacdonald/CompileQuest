using UnityEngine;
using TMPro;

public class TextFileReader : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI textDisplay;
    public GameObject panel;

    // Reads from AIFeedbackStore instead of loading a file
    public void LoadTextFile()
    {
        DisplayFeedback(AIFeedbackStore.feedback);
    }

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
