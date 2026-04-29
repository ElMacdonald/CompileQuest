using UnityEngine;
using TMPro;

>>>>>>> f73c97a7d563d226ac61a0e68f5fba5072e7c855
public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject      loginPanel;
    public TMP_InputField  studentIdInput;
    public TMP_InputField  joinCodeInput;
    public TextMeshProUGUI statusText;

    [Header("Panels")]
    public GameObject levelSelectPanel;  // your main level select UI root
    public GameObject teacherDashboard;  // teacher dashboard panel

    private const float SESSION_DURATION = 28800f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (Session.currentPlayer != null) return;

        if (PlayerPrefs.HasKey("sessionUserId"))
        {
            float savedTime = PlayerPrefs.GetFloat("sessionTime", 0f);
            float elapsed   = Time.realtimeSinceStartup - savedTime;

            if (elapsed < SESSION_DURATION)
            {
                string savedUserId   = PlayerPrefs.GetString("sessionUserId");
                string savedCode     = PlayerPrefs.GetString("sessionCode", "");
                bool   savedTeacher  = PlayerPrefs.GetInt("sessionIsTeacher", 0) == 1;
                AutoLogin(savedUserId, savedCode, savedTeacher);
                return;
            }
            else
            {
                ClearSession();
            }
        }

        loginPanel.SetActive(true);
    }

    public void OnLoginButtonPressed()
    {
        string userId   = studentIdInput.text.Trim();
        string joinCode = joinCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(userId))   { SetStatus("Please enter your ID.");        return; }
        if (string.IsNullOrEmpty(joinCode)) { SetStatus("Please enter your join code."); return; }

        bool isTeacher = userId.ToUpper().Contains("TCHR");

        SetStatus("Loading...");

        if (isTeacher)
        {
            // Teacher — no Firebase lookup needed, just validate and show dashboard
            Session.userId        = userId;
            Session.classroomCode = joinCode;
            Session.currentPlayer = new PlayerData
            {
                userId        = userId,
                displayName   = userId,
                classroomCode = joinCode
            };
            Session.StartSession();
            SaveSessionCookie(userId, joinCode, true);
            loginPanel.SetActive(false);
            SetStatus("");
            ShowTeacherDashboard(joinCode);
        }
        else
        {
            // Student — load from Firebase
            string fullId = joinCode + "_" + userId;
            Session.userId        = fullId;
            Session.classroomCode = joinCode;

            FirebaseManager.Instance.Load(fullId, (loadedData) =>
            {
                if (loadedData != null)
                {
                    Session.currentPlayer = loadedData;
                    Debug.Log("[Login] Loaded save for: " + fullId);
                }
                else
                {
                    Session.currentPlayer = new PlayerData
                    {
                        userId        = fullId,
                        displayName   = userId,
                        classroomCode = joinCode
                    };
                    FirebaseManager.Instance.Save(fullId, Session.currentPlayer);
                    Debug.Log("[Login] New save created for: " + fullId);
                }

                Session.StartSession();
                SaveSessionCookie(fullId, joinCode, false);
                loginPanel.SetActive(false);
                SetStatus("");
            },
            (error) =>
            {
                Debug.LogWarning("[Login] Firebase unavailable: " + error);
                Session.currentPlayer = new PlayerData
                {
                    userId        = fullId,
                    displayName   = userId,
                    classroomCode = joinCode
                };
                Session.StartSession();
                loginPanel.SetActive(false);
                SetStatus("(Offline mode)");
            });
        }
    }

    void AutoLogin(string userId, string code, bool isTeacher)
    {
        Session.userId        = userId;
        Session.classroomCode = code;
        SetStatus("Loading...");

        if (isTeacher)
        {
            Session.currentPlayer = new PlayerData { userId = userId, classroomCode = code };
            Session.StartSession();
            loginPanel.SetActive(false);
            SetStatus("");
            ShowTeacherDashboard(code);
            return;
        }

        FirebaseManager.Instance.Load(userId, (loadedData) =>
        {
            Session.currentPlayer = loadedData ?? new PlayerData { userId = userId, classroomCode = code };
            Session.StartSession();
            loginPanel.SetActive(false);
            SetStatus("");
            Debug.Log("[Login] Auto-login OK: " + userId);
        },
        (error) =>
        {
            Session.currentPlayer = new PlayerData { userId = userId, classroomCode = code };
            Session.StartSession();
            loginPanel.SetActive(false);
            SetStatus("(Offline mode)");
        });
    }

    void ShowTeacherDashboard(string joinCode)
    {
        if (teacherDashboard != null)
        {
            teacherDashboard.SetActive(true);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
            TeacherDashboard td = teacherDashboard.GetComponent<TeacherDashboard>();
            if (td != null) td.LoadClassroom(joinCode);
        }
        else
        {
            Debug.LogWarning("[Login] teacherDashboard panel not assigned in Inspector!");
        }
    }

    void SaveSessionCookie(string userId, string code, bool isTeacher)
    {
        PlayerPrefs.SetString("sessionUserId",    userId);
        PlayerPrefs.SetString("sessionCode",      code);
        PlayerPrefs.SetFloat ("sessionTime",      Time.realtimeSinceStartup);
        PlayerPrefs.SetInt   ("sessionIsTeacher", isTeacher ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ClearSession()
    {
        PlayerPrefs.DeleteKey("sessionUserId");
        PlayerPrefs.DeleteKey("sessionCode");
        PlayerPrefs.DeleteKey("sessionTime");
        PlayerPrefs.DeleteKey("sessionIsTeacher");
        PlayerPrefs.Save();
    }

>>>>>>> f73c97a7d563d226ac61a0e68f5fba5072e7c855
    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    public void Logout()
    {
        ClearSession();
        Session.userId        = "";
        Session.classroomCode = "";
        Session.currentPlayer = null;
        loginPanel.SetActive(true);
        if (teacherDashboard  != null) teacherDashboard.SetActive(false);
        if (levelSelectPanel  != null) levelSelectPanel.SetActive(true);
    }
}