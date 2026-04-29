using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ObjectiveTracker : MonoBehaviour
{
    public GameObject collectablesParent;

    [Header("Collectables")]
    public bool collectableLevel;
    public int totalCollectables;
    public int currentCollectables = 0;

    [Header("Variables")]
    public bool varsNeeded;
    public int usedVars;
    public int neededVars;

    [Header("Conditionals")]
    public bool conditionalsNeeded;
    public int usedConditionals;
    public int neededConditionals;

    [Header("Loops")]
    public bool loopsNeeded;
    public int usedLoops;
    public int neededLoops;

    [Header("Lines of Code (Max Budget)")]
    [Tooltip("Player must complete the level using this many lines or fewer.")]
    public bool linesNeeded;
    public int usedLines;
    public int maxLines;

    public bool levelWon = false;

    public TextMeshProUGUI objectiveDisplay;

    void Start()
    {
        if (collectableLevel)
            totalCollectables = collectablesParent.transform.childCount;

        UpdateObjectiveDisplay();
    }

    void UpdateObjectiveDisplay()
    {
        List<string> objectives = new List<string>();

        if (collectableLevel)
        {
            currentCollectables = totalCollectables;
            foreach (Transform child in collectablesParent.transform)
            {
                if (child.gameObject.activeSelf)
                    currentCollectables -= 1;
            }
            objectives.Add("Collectables: " + currentCollectables + " / " + totalCollectables);
        }
        if (varsNeeded)
            objectives.Add("Variables Used: " + usedVars + " / " + neededVars);

        if (conditionalsNeeded)
            objectives.Add("Conditionals Used: " + usedConditionals + " / " + neededConditionals);

        if (loopsNeeded)
            objectives.Add("Loops Used: " + usedLoops + " / " + neededLoops);

        if (linesNeeded)
        {
            string lineStatus = usedLines <= maxLines ? "✓" : "✗";
            objectives.Add("Lines of Code: " + usedLines + " / " + maxLines + " max " + lineStatus);
        }

        objectiveDisplay.text = string.Join("\n", objectives);
    }

    void checkWin()
    {
        if (collectableLevel)
        {
            if (currentCollectables >= totalCollectables)
                levelWon = true;
            else { levelWon = false; return; }
        }
        if (varsNeeded)
        {
            if (usedVars >= neededVars)
                levelWon = true;
            else { levelWon = false; return; }
        }
        if (conditionalsNeeded)
        {
            if (usedConditionals >= neededConditionals)
                levelWon = true;
            else { levelWon = false; return; }
        }
        if (loopsNeeded)
        {
            if (usedLoops >= neededLoops)
                levelWon = true;
            else { levelWon = false; return; }
        }
        if (linesNeeded)
        {
            if (usedLines <= maxLines)
                levelWon = true;
            else { levelWon = false; return; }
        }
    }

    // Called by ReadBox at the start of each run to reset runtime counters
    public void ResetCounters()
    {
        usedVars = 0;
        usedConditionals = 0;
        usedLoops = 0;
        usedLines = 0;
    }

    void Update()
    {
        UpdateObjectiveDisplay();
        checkWin();
    }
}
