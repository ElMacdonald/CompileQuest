using UnityEngine;

// Editor-only. Attach to any title screen GameObject.
// Press P in Play Mode to wipe the DEV_devuser's Firebase and PlayerPrefs progress.

public class DevTools : MonoBehaviour
{
#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            ResetDevProgress();
    }

    void ResetDevProgress()
    {
        Debug.LogWarning("[DevTools] Resetting DEV progress...");

        for (int i = 0; i < 45; i++)
            PlayerPrefs.DeleteKey("Level_Completed_" + i);
        PlayerPrefs.Save();

        if (Session.currentPlayer != null)
        {
            Session.currentPlayer.completedLevelIds.Clear();
            Session.currentPlayer.levelStars.Clear();
            Session.currentPlayer.levelAttempts.Clear();
            Session.currentPlayer.coins = 0;
        }
        else
        {
            Session.userId        = "DEV_devuser";
            Session.classroomCode = "DEV";
            Session.currentPlayer = new PlayerData
            {
                userId        = "DEV_devuser",
                displayName   = "Dev User",
                classroomCode = "DEV"
            };
        }

        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.Save(
                Session.userId,
                Session.currentPlayer,
                onSuccess: () => Debug.LogWarning("[DevTools] Firebase reset done."),
                onError:   (err) => Debug.LogError("[DevTools] Firebase reset failed: " + err)
            );
        }
        else
        {
            Debug.LogError("[DevTools] FirebaseManager not found.");
        }
    }
#endif
}
