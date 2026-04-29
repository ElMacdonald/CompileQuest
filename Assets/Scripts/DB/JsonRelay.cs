using UnityEngine;

// Stub for handling server responses.

public class JsonRelay : MonoBehaviour
{
    public static JsonRelay Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void HandleServerResponse(string jsonFromServer)
    {
        DeliverToGame(jsonFromServer);
        // SaveToFirebase(jsonFromServer);
    }

    void DeliverToGame(string json)
    {
        Debug.Log("[JsonRelay] Delivered to game: " + json);
    }

    /*
    void SaveToFirebase(string json)
    {
        if (Session.currentPlayer == null) return;
        // Parse json and update Session.currentPlayer as needed
        FirebaseManager.Instance.Save(Session.userId, Session.currentPlayer);
    }
    */
}
