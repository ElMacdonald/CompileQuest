using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;

    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SoundManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SoundManager (auto)");
                    _instance = go.AddComponent<SoundManager>();
                    Debug.LogWarning("[SoundManager] None found in scene — created empty instance. Add a SoundManager to your first scene and assign clips in the Inspector.");
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildLookup();
    }

    [System.Serializable]
    public class SoundEntry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
    }

    public List<SoundEntry> sounds = new List<SoundEntry>();
    private Dictionary<string, SoundEntry> _lookup = new Dictionary<string, SoundEntry>();

    void BuildLookup()
    {
        _lookup.Clear();
        foreach (var entry in sounds)
        {
            if (string.IsNullOrEmpty(entry.id)) continue;
            if (_lookup.ContainsKey(entry.id))
            {
                Debug.LogWarning($"[SoundManager] Duplicate sound ID: '{entry.id}' — keeping first.");
                continue;
            }
            _lookup[entry.id] = entry;
        }
    }

    public static void Play(string id, float volume = -1f, float pitch = -1f)
    {
        Instance?.PlayInternal(id, Vector3.zero, false, volume, pitch);
    }

    public static void Play(string id, Vector3 position, float volume = -1f, float pitch = -1f)
    {
        Instance?.PlayInternal(id, position, true, volume, pitch);
    }

    void PlayInternal(string id, Vector3 position, bool spatialize, float volumeOverride, float pitchOverride)
    {
        if (!_lookup.TryGetValue(id, out SoundEntry entry))
        {
            Debug.LogWarning($"[SoundManager] Sound ID not found: '{id}'");
            return;
        }

        if (entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] Sound '{id}' has no AudioClip assigned.");
            return;
        }

        entry.clip.LoadAudioData();

        GameObject go = new GameObject($"Sound_{id}");
        go.transform.SetParent(transform);
        go.transform.position = position;

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = entry.clip;
        src.volume = volumeOverride >= 0f ? volumeOverride : entry.volume;
        float resolvedPitch = pitchOverride >= 0f ? pitchOverride : entry.pitch;
        src.pitch = Mathf.Approximately(resolvedPitch, 0f) ? 1f : resolvedPitch;
        src.loop = entry.loop;
        src.spatialBlend = spatialize ? 1f : 0f;
        src.playOnAwake = false;
        src.Play();

        if (!entry.loop)
        {
            float clipLength = entry.clip.length > 0f ? entry.clip.length : 5f;
            Destroy(go, clipLength / Mathf.Max(Mathf.Abs(src.pitch), 0.01f));
        }
    }

    public static void Stop(string id)
    {
        foreach (var src in FindObjectsOfType<AudioSource>())
        {
            if (src.gameObject.name == $"Sound_{id}")
            {
                src.Stop();
                Destroy(src.gameObject);
            }
        }
    }
}
