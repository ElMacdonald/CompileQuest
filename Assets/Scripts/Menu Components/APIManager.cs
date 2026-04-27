using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance;

    private string inputsURL = "https://api.compilequest.org/inputs";
    private string parsonsURL = "https://api.compilequest.org/parsons";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // optional but recommended
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [System.Serializable]
    public class RequestData
    {
        public string player_input;
        public string solution;
    }

    [System.Serializable]
    public class ResponseData
    {
        public string feedback;
    }

    public IEnumerator SendInputs(string playerCode, string solutionCode, System.Action<string> callback)
    {
        yield return SendRequest(inputsURL, playerCode, solutionCode, callback);
    }

    public IEnumerator SendParsons(string playerCode, string solutionCode, System.Action<string> callback)
    {
        yield return SendRequest(parsonsURL, playerCode, solutionCode, callback);
    }

    private IEnumerator SendRequest(string url, string playerCode, string solutionCode, System.Action<string> callback)
    {
        RequestData data = new RequestData
        {
            player_input = playerCode,
            solution = solutionCode
        };

        string json = JsonUtility.ToJson(data);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ResponseData response = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);
            if (response == null || string.IsNullOrEmpty(response.feedback))
            {
                Debug.LogError("[APIManager] Received empty or unparseable response: " + request.downloadHandler.text);
                callback?.Invoke("Error: No feedback received from server.");
            }
            else
            {
                callback?.Invoke(response.feedback);
            }
        }
        else
        {
            Debug.LogError("[APIManager] Request failed: " + request.error + "\n" + request.downloadHandler.text);
            callback?.Invoke("Error: Could not reach server.");
        }
    }
}