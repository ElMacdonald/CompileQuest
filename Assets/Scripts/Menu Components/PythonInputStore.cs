// Shared in-memory store for player code input.
// Used instead of writing to Application.dataPath, which breaks in WebGL.
// ParsonsFileReading and WriteToFile both read/write here.
public static class PythonInputStore
{
    public static string[] lines = new string[0];
    public static string text = "";
}
