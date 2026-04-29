// Shared in-memory store for AI feedback.
// AIEvaluator writes here; TextFileReader reads from here.
public static class AIFeedbackStore
{
    public static string feedback = "";
}
