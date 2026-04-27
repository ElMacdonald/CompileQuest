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

    public void Submit()
    {
        if (APIManager.Instance == null)
        {
            Debug.LogError("[SubmitCode] APIManager not found in scene!");
            return;
        }

        string playerCode = inputField.text;

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