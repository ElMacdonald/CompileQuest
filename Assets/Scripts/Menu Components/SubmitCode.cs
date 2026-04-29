using UnityEngine;
using TMPro;
using System.Collections;

public class SubmitCode : MonoBehaviour
{
    public TMP_InputField inputField;
    public TextFileReader feedbackUI;

    [TextArea]
    public string solutionText;

    public GameObject loadingSpinner;

    [Header("Parsons Mode")]
    public bool isParsons = false;

    public void Submit()
    {
        if (APIManager.Instance == null)
        {
            Debug.LogError("[SubmitCode] APIManager not found in scene!");
            return;
        }

        string playerCode;

        if (isParsons)
        {
            ParsonsFileReading pfr = FindObjectOfType<ParsonsFileReading>();
            if (pfr == null)
            {
                Debug.LogError("[SubmitCode] isParsons is true but no ParsonsFileReading found in scene!");
                return;
            }

            pfr.WriteDropzones();
            playerCode = string.Join("\n", PythonInputStore.lines);
        }
        else
        {
            playerCode = inputField.text;
        }

        if (loadingSpinner != null)
            loadingSpinner.SetActive(true);

        StartCoroutine(APIManager.Instance.SendInputs(playerCode, solutionText, OnFeedbackReceived));
    }

    private void OnFeedbackReceived(string feedback)
    {
        if (loadingSpinner != null)
            loadingSpinner.SetActive(false);

        AIFeedbackStore.feedback = feedback;
        feedbackUI.DisplayFeedback(feedback);
    }
}