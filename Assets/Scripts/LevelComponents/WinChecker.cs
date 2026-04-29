using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class WinChecker : MonoBehaviour
{
    [Header("Win Panel")]
    public GameObject winPanel;

    [Header("Player")]
    public Transform player;
    public Vector2 respawnPoint;

    [Header("Timing")]
    public float winDelay      = 1f;
    public float checkInterval = 0.2f;

    private Coroutine winRoutine;
    public GameObject[] collectables;
    public ObjectiveTracker objTracker;

    private void Start()
    {
        InvokeRepeating(nameof(CheckChildren), 0f, checkInterval);
        respawnPoint = player.position;

        if (GameObject.Find("Objective Manager") != null)
            objTracker = GameObject.Find("Objective Manager").GetComponent<ObjectiveTracker>();

        // Snapshot all children at start before any get deactivated or destroyed
        collectables = new GameObject[transform.childCount];
        int i = 0;
        foreach (Transform child in transform)
            collectables[i++] = child.gameObject;
    }

    private void CheckChildren()
    {
        if (winPanel != null && winPanel.activeSelf) return;

        // If any collectable is still active, not done yet
        foreach (GameObject c in collectables)
        {
            if (c != null && c.activeSelf)
            {
                if (winRoutine != null) { StopCoroutine(winRoutine); winRoutine = null; }
                return;
            }
        }

        // All collected — check objective tracker if present
        if (winRoutine == null)
        {
            bool objsPassed = objTracker == null || objTracker.levelWon;
            if (objsPassed)
                winRoutine = StartCoroutine(WinAfterDelay());
        }
    }

    private IEnumerator WinAfterDelay()
    {
        float timer = 0f;
        while (timer < winDelay)
        {
            timer += Time.deltaTime;

            // If a collectable comes back active, abort
            foreach (GameObject c in collectables)
            {
                if (c != null && c.activeSelf) { winRoutine = null; yield break; }
            }

            yield return null;
        }

        // Award coins on win
        if (Session.currentPlayer != null)
            Session.currentPlayer.coins += 10;

        if (winPanel != null) winPanel.SetActive(true);

        if (LevelManager.Instance != null)
            LevelManager.Instance.CompleteLevel(SceneManager.GetActiveScene().buildIndex);

        winRoutine = null;
    }

    public void ResetLevel()
    {
        if (winRoutine != null) { StopCoroutine(winRoutine); winRoutine = null; }

        // Re-enable all collectables from the snapshot taken in Start
        foreach (GameObject c in collectables)
            if (c != null) c.SetActive(true);

        if (winPanel != null) winPanel.SetActive(false);
        if (player != null)   player.position = respawnPoint;
    }
}
