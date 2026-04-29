using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [Header("Firebase Project ID")]
    [Tooltip("Set in the Inspector on the title screen. Fallback used when auto-created at runtime.")]
    public string projectId = "seniorprojectgame-ceb56";

    private string BaseUrl =>
        $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("FirebaseManager [Auto]");
        go.AddComponent<FirebaseManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Copy the inspector projectId to the live instance before destroying this duplicate
            if (!string.IsNullOrEmpty(projectId))
                Instance.projectId = projectId;
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Save(string userId, PlayerData data,
        System.Action onSuccess = null,
        System.Action<string> onError = null)
    {
        Session.FlushPlayTime();
        string json = BuildFirestoreDocument(data);
        StartCoroutine(PatchDocument("students/" + userId, json, onSuccess, onError));
    }

    public void Load(string userId,
        System.Action<PlayerData> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(GetDocument("students/" + userId, onSuccess, onError));
    }

    IEnumerator PatchDocument(string path, string json,
        System.Action onSuccess,
        System.Action<string> onError)
    {
        string url  = BaseUrl + "/" + path;
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using UnityWebRequest req = new UnityWebRequest(url, "PATCH");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
#if UNITY_EDITOR
            Debug.Log("[Firebase] Saved: " + path);
#endif
            onSuccess?.Invoke();
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("[Firebase] Save failed: " + req.error);
#endif
            onError?.Invoke(req.error);
        }
    }

    IEnumerator GetDocument(string path,
        System.Action<PlayerData> onSuccess,
        System.Action<string> onError)
    {
        string url = BaseUrl + "/" + path;

        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            PlayerData data = ParseFirestoreDocument(req.downloadHandler.text);
#if UNITY_EDITOR
            Debug.Log("[Firebase] Loaded: " + path);
#endif
            onSuccess?.Invoke(data);
        }
        else
        {
            if (req.responseCode == 404)
            {
                onSuccess?.Invoke(null);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Firebase] Load failed: " + req.error);
#endif
                onError?.Invoke(req.error);
            }
        }
    }

    // Wraps PlayerData as a stringValue field for Firestore
    string BuildFirestoreDocument(PlayerData data)
    {
        string inner   = JsonUtility.ToJson(data);
        string escaped = inner.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return "{\"fields\":{\"json\":{\"stringValue\":\"" + escaped + "\"}}}";
    }

    PlayerData ParseFirestoreDocument(string firestoreJson)
    {
        try
        {
            // Firestore REST can format "stringValue" with or without a space before the colon,
            // so just search for the field name and skip ahead to the opening quote.
            const string key = "\"stringValue\"";
            int start = firestoreJson.IndexOf(key);
            if (start < 0) return null;
            start += key.Length;
            // Skip whitespace and colon, then land on the opening quote
            while (start < firestoreJson.Length && firestoreJson[start] != '"') start++;
            if (start >= firestoreJson.Length) return null;
            start++; // skip past the opening quote

            // Walk forward to the closing quote, skipping escaped chars
            int end = start;
            while (end < firestoreJson.Length)
            {
                if (firestoreJson[end] == '\\') { end += 2; continue; } // escaped char
                if (firestoreJson[end] == '"')  { break; }              // closing quote
                end++;
            }
            if (end >= firestoreJson.Length) return null;

            string escaped = firestoreJson.Substring(start, end - start);
            string inner   = escaped.Replace("\\\"", "\"").Replace("\\\\", "\\");
            return JsonUtility.FromJson<PlayerData>(inner);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Firebase] Parse error: " + e.Message);
            return null;
        }
    }
}
