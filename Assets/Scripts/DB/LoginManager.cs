using UnityEngine;
using TMPro;

//  FIRST TIME: Student enters ID + Join Code + new 6-digit PIN
//              → account created, PIN hashed and saved to Firebase
//  RETURNING:  Student enters ID + Join Code + their PIN
//              → PIN verified against stored hash
//              → 3 wrong attempts locks for 5 minutes
//  This means no two students can share the same ID + Join Code
//  because the first student to register locks it with their PIN.


public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject      loginPanel;
    public TMP_InputField  studentIdInput;
    public TMP_InputField  joinCodeInput;
    public TMP_InputField  pinInput;           // 6-digit PIN field
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI pinLabel;           // label that changes: "Create PIN" vs "Enter PIN"

    [Header("Panels")]
    public GameObject levelSelectPanel;
    public GameObject teacherDashboard;

    private const float SESSION_DURATION  = 28800f; // 8 hours
    private const int   PIN_LENGTH        = 6;
    private const int   MAX_ATTEMPTS      = 3;
    private const float LOCKOUT_MINUTES   = 5f;

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
                string savedUserId  = PlayerPrefs.GetString("sessionUserId");
                string savedCode    = PlayerPrefs.GetString("sessionCode", "");
                bool   savedTeacher = PlayerPrefs.GetInt("sessionIsTeacher", 0) == 1;
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

    // Login Button 

    public void OnLoginButtonPressed()
    {
        string userId   = studentIdInput.text.Trim();
        string joinCode = joinCodeInput.text.Trim().ToUpper();
        string pin      = pinInput.text.Trim();

        if (string.IsNullOrEmpty(userId))   { SetStatus("Please enter your student ID.");  return; }
        if (string.IsNullOrEmpty(joinCode)) { SetStatus("Please enter your join code.");   return; }
        if (string.IsNullOrEmpty(pin))      { SetStatus("Please enter your PIN.");         return; }

        if (pin.Length != PIN_LENGTH)
        {
            SetStatus($"PIN must be exactly {PIN_LENGTH} digits.");
            return;
        }

        bool isTeacher = userId.ToUpper().Contains("TCHR");

        if (isTeacher)
        {
            HandleTeacherLogin(userId, joinCode);
            return;
        }

        string fullId = joinCode + "_" + userId;

        // Check lockout
        if (IsLockedOut(fullId))
            return;

        SetStatus("Checking account...");

        // Check if account exists in Firebase
        FirebaseManager.Instance.LoadRaw("students/" + fullId, (json) =>
        {
            if (json == null)
            {
                // New account — validate PIN length and create
                if (pin.Length != PIN_LENGTH)
                {
                    SetStatus($"New account. Create a {PIN_LENGTH}-digit PIN.");
                    return;
                }
                if (pinLabel != null) pinLabel.text = "Create a 6-digit PIN";
                CreateNewAccount(fullId, userId, joinCode, pin);
            }
            else
            {
                // Existing account — verify PIN
                if (pinLabel != null) pinLabel.text = "Enter your PIN";
                VerifyAndLogin(fullId, joinCode, userId, pin, json);
            }
        },
        (error) =>
        {
            // Firebase unavailable — allow offline with warning
            Debug.LogWarning("[Login] Firebase unavailable: " + error);
            Session.currentPlayer = new PlayerData { userId = fullId, displayName = userId, classroomCode = joinCode };
            Session.StartSession();
            loginPanel.SetActive(false);
            SetStatus("(Offline mode — progress may not save)");
        });
    }

    // ── New Account ──────────────────────────────────────────

    void CreateNewAccount(string fullId, string userId, string joinCode, string pin)
    {
        SetStatus("Creating account...");

        string pinHash = PinHasher.Hash(pin, fullId);

        PlayerData newPlayer = new PlayerData
        {
            userId        = fullId,
            displayName   = userId,
            classroomCode = joinCode
        };

        // Save player data and PIN hash to Firebase
        FirebaseManager.Instance.SaveWithPin(fullId, newPlayer, pinHash,
            onSuccess: () =>
            {
                Session.userId        = fullId;
                Session.classroomCode = joinCode;
                Session.currentPlayer = newPlayer;
                Session.StartSession();
                SaveSessionCookie(fullId, joinCode, false);
                loginPanel.SetActive(false);
                SetStatus("");
                Debug.Log("[Login] New account created: " + fullId);
            },
            onError: (e) =>
            {
                SetStatus("Failed to create account. Try again.");
                Debug.LogWarning("[Login] Account creation failed: " + e);
            });
    }

    // ── Existing Account ─────────────────────────────────────

    void VerifyAndLogin(string fullId, string joinCode, string userId, string pin, string firestoreJson)
    {
        // Extract PIN hash from Firestore document
        string storedPinHash = FirebaseManager.Instance.ExtractPinHash(firestoreJson);

        if (string.IsNullOrEmpty(storedPinHash))
        {
            // Account exists but has no PIN (legacy account) — let them set one
            SetStatus("No PIN found. Please re-create your account or contact your teacher.");
            return;
        }

        if (!PinHasher.Verify(pin, fullId, storedPinHash))
        {
            RegisterFailedAttempt(fullId);
            return;
        }

        // PIN correct — load player data
        ResetFailedAttempts(fullId);

        PlayerData loadedData = FirebaseManager.Instance.ParsePlayerDataFromRaw(firestoreJson);
        Session.userId        = fullId;
        Session.classroomCode = joinCode;
        Session.currentPlayer = loadedData ?? new PlayerData { userId = fullId, displayName = userId, classroomCode = joinCode };
        Session.StartSession();
        SaveSessionCookie(fullId, joinCode, false);
        loginPanel.SetActive(false);
        SetStatus("");
        Debug.Log("[Login] Logged in: " + fullId);
    }

    // ── Teacher Login ────────────────────────────────────────

    void HandleTeacherLogin(string userId, string joinCode)
    {
        Session.userId        = userId;
        Session.classroomCode = joinCode;
        Session.currentPlayer = new PlayerData { userId = userId, displayName = userId, classroomCode = joinCode };
        Session.StartSession();
        SaveSessionCookie(userId, joinCode, true);
        loginPanel.SetActive(false);
        SetStatus("");
        ShowTeacherDashboard(joinCode);
    }

    // ── Auto Login ───────────────────────────────────────────

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

    // ── Lockout System ───────────────────────────────────────

    bool IsLockedOut(string userId)
    {
        string attemptsKey  = "attempts_" + userId;
        string lockTimeKey  = "locktime_"  + userId;

        int   attempts  = PlayerPrefs.GetInt(attemptsKey, 0);
        float lockTime  = PlayerPrefs.GetFloat(lockTimeKey, 0f);

        if (attempts >= MAX_ATTEMPTS)
        {
            float elapsed = Time.realtimeSinceStartup - lockTime;
            float remaining = (LOCKOUT_MINUTES * 60f) - elapsed;

            if (remaining > 0f)
            {
                int mins = Mathf.CeilToInt(remaining / 60f);
                SetStatus($"Account locked. Try again in {mins} minute{(mins == 1 ? "" : "s")}.");
                return true;
            }
            else
            {
                // Lockout expired — reset
                ResetFailedAttempts(userId);
                return false;
            }
        }

        return false;
    }

    void RegisterFailedAttempt(string userId)
    {
        string attemptsKey = "attempts_" + userId;
        string lockTimeKey = "locktime_"  + userId;

        int attempts = PlayerPrefs.GetInt(attemptsKey, 0) + 1;
        PlayerPrefs.SetInt(attemptsKey, attempts);

        if (attempts >= MAX_ATTEMPTS)
        {
            PlayerPrefs.SetFloat(lockTimeKey, Time.realtimeSinceStartup);
            SetStatus($"Wrong PIN. Account locked for {(int)LOCKOUT_MINUTES} minutes.");
        }
        else
        {
            int remaining = MAX_ATTEMPTS - attempts;
            SetStatus($"Wrong PIN. {remaining} attempt{(remaining == 1 ? "" : "s")} remaining.");
        }

        PlayerPrefs.Save();
    }

    void ResetFailedAttempts(string userId)
    {
        PlayerPrefs.DeleteKey("attempts_" + userId);
        PlayerPrefs.DeleteKey("locktime_"  + userId);
        PlayerPrefs.Save();
    }

    // ── Teacher Dashboard ────────────────────────────────────

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
            Debug.LogWarning("[Login] teacherDashboard not assigned in Inspector!");
        }
    }

    // ── Session Cookie ───────────────────────────────────────

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
        if (teacherDashboard != null) teacherDashboard.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
    }
}
