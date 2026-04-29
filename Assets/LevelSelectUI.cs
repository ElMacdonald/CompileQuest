using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LevelSelectUI : MonoBehaviour
{
    [Header("Button Colors")]
    public Color defaultColor   = Color.white;
    public Color completedColor = new Color(0.3f, 0.85f, 0.4f);
    public Color lockedColor    = new Color(0.5f, 0.5f, 0.5f);

    [Header("Level Locking")]
    public bool useLevelLocking = false;

    [Header("Index Offset")]
    [Tooltip("0-based index of the first level in this panel. Planet 1 = 0, Planet 2 = 15, Planet 3 = 30.")]
    public int firstLevelIndex = 0;

    // OnEnable intentionally removed — refresh is driven by LevelManager.OnSceneLoaded
    // and PlanetCarouselUI.EnterPlanet after Firebase sync completes, preventing stale reads.

    public void RefreshAllButtons()
    {
        if (LevelManager.Instance == null) return;

        List<GameObject> levelObjects = FindLevelObjects();
        if (levelObjects.Count == 0) return;

        for (int i = 0; i < levelObjects.Count; i++)
        {
            // Prefer reading the absolute index directly from the button name (e.g. "2-3")
            // so the offset is never wrong regardless of panel order or firstLevelIndex.
            int levelIndex = AbsoluteIndexFromName(levelObjects[i].name);
            if (levelIndex < 0)
                levelIndex = firstLevelIndex + i; // fallback for unnamed buttons

            bool  isCompleted = LevelManager.Instance.IsLevelCompleted(levelIndex);
            bool  isLocked    = useLevelLocking && levelIndex > 0
                                && !LevelManager.Instance.IsLevelCompleted(levelIndex - 1);
            Color target      = isLocked ? lockedColor : (isCompleted ? completedColor : defaultColor);

            Button btn = levelObjects[i].GetComponent<Button>();
            if (btn != null)
            {
                ColorBlock cb       = btn.colors;
                cb.normalColor      = target;
                cb.highlightedColor = isCompleted ? completedColor * 1.1f : target;
                cb.colorMultiplier  = 1f;
                btn.colors          = cb;
                btn.interactable    = !isLocked;
                continue;
            }

            Image img = levelObjects[i].GetComponent<Image>();
            if (img != null) { img.color = target; continue; }

            foreach (var childImg in levelObjects[i].GetComponentsInChildren<Image>())
                childImg.color = target;
        }
    }

    // Returns the absolute 0-based index from a name like "2-3" -> 17, or -1 if not parseable.
    private int AbsoluteIndexFromName(string name)
    {
        var parts = name.Split('-');
        if (parts.Length == 2
            && int.TryParse(parts[0], out int planet)
            && int.TryParse(parts[1], out int level))
            return (planet - 1) * 15 + (level - 1);
        return -1;
    }

    private List<GameObject> FindLevelObjects()
    {
        var result = new List<GameObject>();
        CollectLevelChildren(transform, result);
        result.Sort((a, b) => ParseLevelNumber(a.name).CompareTo(ParseLevelNumber(b.name)));
        return result;
    }

    // Names to skip when scanning for level buttons
    private static readonly HashSet<string> IgnoredNames =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        { "Button", "Back", "Return", "Close", "Exit" };

    private void CollectLevelChildren(Transform parent, List<GameObject> result)
    {
        foreach (Transform child in parent)
        {
            if (IgnoredNames.Contains(child.name)) continue;

            if (IsLevelName(child.name))
                result.Add(child.gameObject);
            else
                CollectLevelChildren(child, result); // recurse into non-level containers
        }
    }

    private bool IsLevelName(string name)
    {
        var parts = name.Split('-');
        if (parts.Length != 2) return false;
        return int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _);
    }

    private int ParseLevelNumber(string name)
    {
        var parts = name.Split('-');
        if (parts.Length == 2
            && int.TryParse(parts[0], out int planet)
            && int.TryParse(parts[1], out int level))
            return (planet - 1) * 15 + (level - 1);
        return 0;
    }
}
