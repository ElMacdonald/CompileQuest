using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeacherDashboard : MonoBehaviour
{
    [Header("Header")]
    public TextMeshProUGUI titleText;

    [Header("Student List")]
    public Transform  studentListContainer;
    public GameObject studentRowPrefab;

    [Header("Info Panel")]
    public GameObject      infoPanel;        // a panel that shows/hides
    public TextMeshProUGUI infoText;
    public Button          closeInfoButton;

    [Header("Buttons")]
    public Button refreshButton;
    public Button logoutButton;

    private string _classroom;
    private GameObject _selectedRow;

    void Start()
    {
        if (refreshButton    != null) refreshButton.onClick.AddListener(() => LoadClassroom(_classroom));
        if (logoutButton     != null) logoutButton.onClick.AddListener(() => LoginManager.Instance?.Logout());
        if (closeInfoButton  != null) closeInfoButton.onClick.AddListener(CloseInfo);
        if (infoPanel        != null) infoPanel.SetActive(false);
    }

    public void LoadClassroom(string classroomCode)
    {
        _classroom = classroomCode;
        if (titleText != null) titleText.text = "Classroom: " + classroomCode;
        ClearList();
        CloseInfo();

        FirebaseManager.Instance.LoadClassroom(classroomCode, (students) =>
        {
            ClearList();
            if (students == null || students.Count == 0) return;

            foreach (PlayerData student in students)
            {
                if (student.userId.ToUpper().Contains("TCHR")) continue;
                CreateRow(student);
            }
        },
        (error) => Debug.LogWarning("[TeacherDashboard] " + error));
    }

    void CreateRow(PlayerData student)
    {
        if (studentRowPrefab == null || studentListContainer == null) return;

        GameObject row = Instantiate(studentRowPrefab, studentListContainer);

        // Set button text to student name
        TextMeshProUGUI txt = row.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.text      = string.IsNullOrEmpty(student.displayName) ? student.userId : student.displayName;
            txt.color     = Color.white;
            txt.fontSize  = 16;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            txt.enableWordWrapping = false;
            txt.overflowMode = TextOverflowModes.Ellipsis;
        }

        // Set row size
        RectTransform rt = row.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(200, 40);

        Button btn = row.GetComponent<Button>();
        if (btn == null) btn = row.AddComponent<Button>();

        PlayerData captured = student;
        btn.onClick.AddListener(() =>
        {
            _selectedRow = row;
            ShowInfo(captured);
        });
    }

    void ShowInfo(PlayerData s)
    {
        if (infoPanel != null) infoPanel.SetActive(true);

        string levels = s.completedLevelIds.Count == 0
            ? "None"
            : string.Join(", ", s.completedLevelIds);

        string lastLogin = "Never";
        if (!string.IsNullOrEmpty(s.lastLogin) &&
            System.DateTime.TryParse(s.lastLogin, out System.DateTime dt))
            lastLogin = dt.ToLocalTime().ToString("MMM dd yyyy  h:mm tt");

        int    total   = (int)s.totalPlayTime;
        int    hours   = total / 3600;
        int    minutes = (total % 3600) / 60;
        int    seconds = total % 60;
        string time    = hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m {seconds}s";

        if (infoText != null)
            infoText.text =
                "Student:      " + (string.IsNullOrEmpty(s.displayName) ? s.userId : s.displayName) + "\n" +
                "User ID:       " + s.userId        + "\n\n" +
                "Levels Done: " + s.completedLevelIds.Count + "\n" +
                "Level IDs:    " + levels            + "\n\n" +
                "Coins:          " + s.coins         + "\n\n" +
                "Last Login:   " + lastLogin          + "\n\n" +
                "Play Time:    " + time;
    }

    void CloseInfo()
    {
        if (infoPanel != null) infoPanel.SetActive(false);
        if (infoText  != null) infoText.text = "";
    }

    void ClearList()
    {
        if (studentListContainer == null) return;
        foreach (Transform child in studentListContainer)
            Destroy(child.gameObject);
    }
}