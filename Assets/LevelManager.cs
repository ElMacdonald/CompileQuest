using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private const string LEVEL_KEY_PREFIX = "Level_Completed_";
    private bool _completedThisScene = false;

    // Bootstraps a LevelManager if one doesn't exist — works regardless of start scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("LevelManager [Auto]");
        go.AddComponent<LevelManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _completedThisScene = false;

        if (scene.name != "Level Select") return;

        // Sync Firebase completions into PlayerPrefs so the UI reads correctly.
        // Wipe all level keys first so stale keys from old sessions can't bleed through.
        if (Session.currentPlayer != null)
        {
            for (int i = 0; i < 45; i++)
                PlayerPrefs.DeleteKey(LEVEL_KEY_PREFIX + i);
            foreach (string levelId in Session.currentPlayer.completedLevelIds)
                if (int.TryParse(levelId, out int idx))
                    PlayerPrefs.SetInt(LEVEL_KEY_PREFIX + idx, 1);
            PlayerPrefs.Save();
        }

        foreach (var ui in FindObjectsByType<LevelSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ui.RefreshAllButtons();

        foreach (var carousel in FindObjectsByType<PlanetCarouselUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            carousel.RefreshAllPanels();
    }

    public void CompleteLevel(int levelIndex)
    {
        if (_completedThisScene) return;
        _completedThisScene = true;

        // Use scene name to derive a stable index, immune to build order changes
        int adjustedIndex = GetStableLevelIndex();
        if (adjustedIndex < 0)
        {
            Debug.LogWarning($"[LevelManager] Scene name '{SceneManager.GetActiveScene().name}' could not be parsed. Level NOT marked complete. Rename the scene to 'planet-level' format (e.g. '2-3').");
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"[LevelManager] Complete — scene: {SceneManager.GetActiveScene().name}, index: {adjustedIndex}");
#endif

        PlayerPrefs.SetInt(LEVEL_KEY_PREFIX + adjustedIndex, 1);
        PlayerPrefs.Save();

        if (Session.currentPlayer == null) return;

        Session.currentPlayer.MarkLevelComplete(adjustedIndex.ToString(), stars: 1);

        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.Save(
                Session.userId,
                Session.currentPlayer,
#if UNITY_EDITOR
                onSuccess: () => Debug.Log("[Firebase] Level " + adjustedIndex + " synced."),
                onError:   (err) => Debug.LogWarning("[Firebase] Sync failed: " + err)
#else
                onSuccess: null,
                onError:   null
#endif
            );
        }
    }

    public bool IsLevelCompleted(int levelIndex)
    {
        return PlayerPrefs.GetInt(LEVEL_KEY_PREFIX + levelIndex, 0) == 1;
    }

    // Converts scene name (e.g. "1-5") to a 0-based index
    // Planet 1: 0–14, Planet 2: 15–29, Planet 3: 30–44
    public static int GetStableLevelIndex()
    {
        var parts = SceneManager.GetActiveScene().name.Split('-');
        if (parts.Length != 2) return -1;
        if (!int.TryParse(parts[0], out int planet)) return -1;
        if (!int.TryParse(parts[1], out int level))  return -1;
        return (planet - 1) * 15 + (level - 1);
    }

    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    public void ResetLevel(int levelIndex)
    {
        PlayerPrefs.DeleteKey(LEVEL_KEY_PREFIX + levelIndex);
        PlayerPrefs.Save();
    }
}
