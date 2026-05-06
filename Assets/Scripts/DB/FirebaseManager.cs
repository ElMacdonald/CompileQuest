using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// ================================================================
//  FirebaseManager.cs  —  REPLACE your existing one
//  Added: SaveWithPin, ExtractPinHash, ParsePlayerDataFromRaw
// ================================================================

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [Header("Firebase Project ID")]
    public string projectId = "";

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
            if (!string.IsNullOrEmpty(projectId))
                Instance.projectId = projectId;
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Save PlayerData ──────────────────────────────────────

    public void Save(string userId, PlayerData data,
        System.Action onSuccess = null,
        System.Action<string> onError = null)
    {
        Session.FlushPlayTime();
        string json = BuildFirestoreDocument(data, null);
        StartCoroutine(PatchDocument("students/" + userId, json, onSuccess, onError));
    }

    // ── Save PlayerData WITH PIN hash (first time login) ─────

    public void SaveWithPin(string userId, PlayerData data, string pinHash,
        System.Action onSuccess = null,
        System.Action<string> onError = null)
    {
        string json = BuildFirestoreDocument(data, pinHash);
        StartCoroutine(PatchDocument("students/" + userId, json, onSuccess, onError));
    }

    // ── Load PlayerData ──────────────────────────────────────

    public void Load(string userId,
        System.Action<PlayerData> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(GetDocument("students/" + userId, onSuccess, onError));
    }

    // ── Load Raw JSON (for PIN verification) ─────────────────

    public void LoadRaw(string path,
        System.Action<string> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(GetRawDocument(path, onSuccess, onError));
    }

    // ── Load all students in a classroom ─────────────────────

    public void LoadClassroom(string classroomCode,
        System.Action<System.Collections.Generic.List<PlayerData>> onSuccess,
        System.Action<string> onError = null)
    {
        StartCoroutine(QueryClassroom(classroomCode, onSuccess, onError));
    }

    // ── Extract PIN hash from raw Firestore JSON ─────────────

    public string ExtractPinHash(string firestoreJson)
    {
        try
        {
            const string key = "\"pinHash\"";
            int idx = firestoreJson.IndexOf(key);
            if (idx < 0) return null;
            idx += key.Length;
            // Skip to stringValue
            int sv = firestoreJson.IndexOf("\"stringValue\"", idx);
            if (sv < 0) return null;
            sv += "\"stringValue\"".Length;
            while (sv < firestoreJson.Length && firestoreJson[sv] != '"') sv++;
            if (sv >= firestoreJson.Length) return null;
            sv++;
            int end = sv;
            while (end < firestoreJson.Length && firestoreJson[end] != '"') end++;
            return firestoreJson.Substring(sv, end - sv);
        }
        catch { return null; }
    }

    // ── Parse PlayerData from raw Firestore JSON ─────────────

    public PlayerData ParsePlayerDataFromRaw(string firestoreJson)
    {
        return ParseFirestoreDocument(firestoreJson);
    }

    // ── REST Coroutines ──────────────────────────────────────

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
            Debug.LogWarning("[Firebase] Save failed: " + req.error + " — " + req.downloadHandler.text);
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
                onSuccess?.Invoke(null);
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("[Firebase] Load failed: " + req.error);
#endif
                onError?.Invoke(req.error);
            }
        }
    }

    IEnumerator GetRawDocument(string path,
        System.Action<string> onSuccess,
        System.Action<string> onError)
    {
        string url = BaseUrl + "/" + path;
        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            onSuccess?.Invoke(req.downloadHandler.text);
        else if (req.responseCode == 404)
            onSuccess?.Invoke(null);
        else
        {
            Debug.LogWarning("[Firebase] LoadRaw failed: " + req.error);
            onError?.Invoke(req.error);
        }
    }

    IEnumerator QueryClassroom(string classroomCode,
        System.Action<System.Collections.Generic.List<PlayerData>> onSuccess,
        System.Action<string> onError)
    {
        string url  = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents:runQuery";
        string body = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"students\"}]," +
                      "\"where\":{\"fieldFilter\":{\"field\":{\"fieldPath\":\"classroomCode\"}," +
                      "\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + classroomCode + "\"}}}}}";

        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
        using UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            onSuccess?.Invoke(ParseQueryResults(req.downloadHandler.text));
        else
        {
            Debug.LogWarning("[Firebase] Query failed: " + req.error);
            onError?.Invoke(req.error);
        }
    }

    // ── JSON Helpers ─────────────────────────────────────────

    string BuildFirestoreDocument(PlayerData data, string pinHash)
    {
        string inner   = JsonUtility.ToJson(data);
        string escaped = inner.Replace("\\", "\\\\").Replace("\"", "\\\"");

        string fields = "\"json\":{\"stringValue\":\"" + escaped + "\"}," +
                        "\"classroomCode\":{\"stringValue\":\"" + data.classroomCode + "\"}";

        // Only include pinHash field if provided
        if (!string.IsNullOrEmpty(pinHash))
            fields += ",\"pinHash\":{\"stringValue\":\"" + pinHash + "\"}";

        return "{\"fields\":{" + fields + "}}";
    }

PlayerData ParseFirestoreDocument(string firestoreJson)
{
    try
    {
        // Find the "json" field specifically, not just any stringValue
        const string jsonField = "\"json\"";
        int fieldStart = firestoreJson.IndexOf(jsonField);
        if (fieldStart < 0) return null;

        const string key = "\"stringValue\"";
        int start = firestoreJson.IndexOf(key, fieldStart);
        if (start < 0) return null;
        start += key.Length;
        while (start < firestoreJson.Length && firestoreJson[start] != '"') start++;
        if (start >= firestoreJson.Length) return null;
        start++;

        int end = start;
        while (end < firestoreJson.Length)
        {
            if (firestoreJson[end] == '\\') { end += 2; continue; }
            if (firestoreJson[end] == '"')  { break; }
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

    System.Collections.Generic.List<PlayerData> ParseQueryResults(string json)
    {
        var list = new System.Collections.Generic.List<PlayerData>();
        int pos  = 0;
        while (true)
        {
            int docStart = json.IndexOf("\"document\"", pos);
            if (docStart < 0) break;

            int fieldsStart = json.IndexOf("\"fields\"", docStart);
            if (fieldsStart < 0) break;

            const string key = "\"stringValue\"";
            int svStart = json.IndexOf(key, fieldsStart);
            if (svStart < 0) break;
            svStart += key.Length;
            while (svStart < json.Length && json[svStart] != '"') svStart++;
            if (svStart >= json.Length) break;
            svStart++;

            int svEnd = svStart;
            while (svEnd < json.Length)
            {
                if (json[svEnd] == '\\') { svEnd += 2; continue; }
                if (json[svEnd] == '"')  { break; }
                svEnd++;
            }

            string escaped = json.Substring(svStart, svEnd - svStart);
            string inner   = escaped.Replace("\\\"", "\"").Replace("\\\\", "\\");

            try
            {
                PlayerData pd = JsonUtility.FromJson<PlayerData>(inner);
                if (pd != null) list.Add(pd);
            }
            catch { }

            pos = svEnd + 1;
        }
        return list;
    }
}
