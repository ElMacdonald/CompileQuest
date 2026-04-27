using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Dialogue : MonoBehaviour
{
    public GameObject dialogueBox;
    public TextMeshProUGUI dialogueText;
    public Image navImg;
    public string[] dialogueLines;
    public string[] dialogueMoods;
    public Sprite[] sprites;
    private int currentLineIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
        dialogueBox = GameObject.Find("Dialogue Panel");
        dialogueText = dialogueBox.GetComponentInChildren<TextMeshProUGUI>();
        NextLine();
    }

    public void NextLine()
    {
        if (currentLineIndex < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLineIndex];
            string mood = dialogueMoods[currentLineIndex];
            if(mood == "happy")
            {
                navImg.sprite = sprites[0];
            }
            else if(mood == "shocked")
            {
                navImg.sprite = sprites[1];
            }
            else if(mood == "thinking")
            {
                navImg.sprite = sprites[2];
            }
            else
            {
                navImg.sprite = sprites[3];
            }
            currentLineIndex++;
        }
        else
        {
            dialogueBox.SetActive(false);
        }
    }
}
