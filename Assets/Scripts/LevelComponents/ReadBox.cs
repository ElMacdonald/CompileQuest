using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class ReadBox : MonoBehaviour
{
    public TMP_InputField input;
    public Movement playerMovement;
    public string inputText;
    public List<string> textLines = new List<string>();
    public bool failed = false;
    public TextMeshProUGUI[] codeLines;

    public TextFileReader ts;

    public List<string> variableNames = new List<string>();
    public List<float> variableValues = new List<float>();
    public ObjectiveTracker objTracker;
    private Coroutine codeRunning;

    private const int MAX_LOOP_ITERATIONS = 20;

    [Header("Execution Settings")]
    [Tooltip("Seconds to wait between each line of code executing. Default is 1.")]
    public float lineDelay = 1f;

    void Start()
    {
        playerMovement = FindObjectOfType<Movement>();
        if (GameObject.Find("Objective Manager") != null)
            objTracker = GameObject.Find("Objective Manager").GetComponent<ObjectiveTracker>();
    }

    public void ReadBoxInput()
    {
        inputText = input.text;
        string[] lines = inputText.Split('\n');
        textLines.Clear();

        foreach (string line in lines)
            textLines.Add(line.TrimEnd());

        codeRunning = StartCoroutine(PerformActions());
    }

    // -----------------------------------------------------------------------
    // Value / Expression Resolution
    // -----------------------------------------------------------------------

    int GetIndent(string line)
    {
        int indent = 0;
        foreach (char c in line)
        {
            if (c == ' ') indent++;
            else if (c == '\t') indent += 4;
            else break;
        }
        return indent;
    }

    // Resolves a single token: number literal, True/False boolean, or variable name.
    // True -> 1, False -> 0
    float ResolveToken(string token)
    {
        token = token.Trim();

        if (token == "True")  return 1f;
        if (token == "False") return 0f;

        if (float.TryParse(token, out float number))
            return number;

        int index = variableNames.IndexOf(token);
        if (index != -1)
        {
            if (objTracker != null) objTracker.usedVars += 1;
            return variableValues[index];
        }

        Debug.LogError("Unknown variable: " + token);
        failed = true;
        return 0f;
    }

    // Evaluates a simple arithmetic expression: supports +, -, *, /, %
    float EvaluateExpression(string expr)
    {
        expr = expr.Trim();

        List<string> addTokens = SplitOnAddSub(expr);
        if (addTokens == null) { failed = true; return 0f; }

        float result = EvaluateMulDiv(addTokens[0]);
        int i = 1;
        while (i < addTokens.Count - 1)
        {
            string op = addTokens[i].Trim();
            float right = EvaluateMulDiv(addTokens[i + 1]);
            if (op == "+") result += right;
            else if (op == "-") result -= right;
            i += 2;
        }
        return result;
    }

    List<string> SplitOnAddSub(string expr)
    {
        var parts = new List<string>();
        int start = 0;
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if ((c == '+' || c == '-') && i > 0)
            {
                char prev = expr[i - 1];
                if (prev != '*' && prev != '/' && prev != '%' && prev != '+' && prev != '-')
                {
                    parts.Add(expr.Substring(start, i - start).Trim());
                    parts.Add(c.ToString());
                    start = i + 1;
                }
            }
        }
        parts.Add(expr.Substring(start).Trim());
        return parts;
    }

    float EvaluateMulDiv(string expr)
    {
        expr = expr.Trim();
        var parts = new List<string>();
        int start = 0;
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if (c == '*' || c == '/' || c == '%')
            {
                parts.Add(expr.Substring(start, i - start).Trim());
                parts.Add(c.ToString());
                start = i + 1;
            }
        }
        parts.Add(expr.Substring(start).Trim());

        float result = ResolveToken(parts[0]);
        int i2 = 1;
        while (i2 < parts.Count - 1)
        {
            string op = parts[i2].Trim();
            float right = ResolveToken(parts[i2 + 1]);
            if (op == "*") result *= right;
            else if (op == "/")
            {
                if (right == 0f) { Debug.LogError("Division by zero"); failed = true; return 0f; }
                result /= right;
            }
            else if (op == "%")
            {
                if (right == 0f) { Debug.LogError("Modulo by zero"); failed = true; return 0f; }
                result %= right;
            }
            i2 += 2;
        }
        return result;
    }

    bool EvaluateCondition(string condition)
    {
        condition = condition.Trim();

        string[] ops = new string[] { "==", "!=", "<=", ">=", "<", ">" };
        foreach (string op in ops)
        {
            int idx = condition.IndexOf(op);
            if (idx > 0)
            {
                string left  = condition.Substring(0, idx).Trim();
                string right = condition.Substring(idx + op.Length).Trim();
                float l = EvaluateExpression(left);
                float r = EvaluateExpression(right);
                switch (op)
                {
                    case "==": return l == r;
                    case "!=": return l != r;
                    case "<=": return l <= r;
                    case ">=": return l >= r;
                    case "<":  return l < r;
                    case ">":  return l > r;
                }
            }
        }

        if (condition == "True")  return true;
        if (condition == "False") return false;

        float val = EvaluateExpression(condition);
        if (failed) return false;
        return val != 0f;
    }

    void SetVariable(string name, float value)
    {
        int index = variableNames.IndexOf(name);
        if (index != -1)
            variableValues[index] = value;
        else
        {
            variableNames.Add(name);
            variableValues.Add(value);
        }
    }

    // -----------------------------------------------------------------------
    // Line Highlighting Helpers
    // -----------------------------------------------------------------------

    void HighlightLine(int lineIndex)
    {
        if (lineIndex >= 0 && lineIndex < codeLines.Length)
            codeLines[lineIndex].color = Color.yellow;
    }

    void ClearLine(int lineIndex)
    {
        if (lineIndex >= 0 && lineIndex < codeLines.Length)
            codeLines[lineIndex].color = Color.white;
    }

    void ErrorLine(int lineIndex)
    {
        if (lineIndex >= 0 && lineIndex < codeLines.Length)
            codeLines[lineIndex].color = Color.red;
    }

    // -----------------------------------------------------------------------
    // Main Interpreter
    // -----------------------------------------------------------------------

    IEnumerator PerformActions()
    {
        variableNames.Clear();
        variableValues.Clear();
        failed = false;

        if (objTracker != null)
            objTracker.ResetCounters();

        yield return StartCoroutine(ExecuteBlock(0, textLines.Count, 0));

        foreach (var codeLine in codeLines)
            codeLine.color = Color.white;
    }

    IEnumerator ExecuteBlock(int startLine, int endLine, int expectedIndent)
    {
        int i = startLine;
        while (i < endLine && !failed)
        {
            string rawLine = textLines[i];
            string line = rawLine.TrimStart();

            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            int lineIndent = GetIndent(rawLine);

            if (lineIndent < expectedIndent) break;
            if (lineIndent > expectedIndent) { i++; continue; }

            HighlightLine(i);
            yield return new WaitForSeconds(0.05f);

            // ----------------------------------------------------------------
            // if statement
            // ----------------------------------------------------------------
            Match ifMatch = Regex.Match(line, @"^if\s+(.+?)\s*:\s*$");
            if (ifMatch.Success)
            {
                if (objTracker != null) objTracker.usedConditionals += 1;

                string condition = ifMatch.Groups[1].Value;
                bool condResult = EvaluateCondition(condition);

                int bodyIndent = expectedIndent + 4;
                int bodyStart = i + 1;
                int bodyEnd = FindBlockEnd(bodyStart, endLine, bodyIndent);

                int elseStart = -1;
                int elseBodyEnd = bodyEnd;
                int j = bodyEnd;

                while (j < endLine)
                {
                    string nextRaw = textLines[j];
                    string nextTrimmed = nextRaw.TrimStart();
                    int nextIndent = GetIndent(nextRaw);

                    if (string.IsNullOrWhiteSpace(nextTrimmed)) { j++; continue; }
                    if (nextIndent != expectedIndent) break;

                    Match elifMatch = Regex.Match(nextTrimmed, @"^elif\s+(.+?)\s*:\s*$");
                    Match elseMatch = Regex.Match(nextTrimmed, @"^else\s*:\s*$");

                    if (elifMatch.Success || elseMatch.Success)
                    {
                        elseStart = j;
                        elseBodyEnd = FindBlockEnd(j + 1, endLine, bodyIndent);
                        break;
                    }
                    break;
                }

                if (condResult)
                {
                    yield return StartCoroutine(ExecuteBlock(bodyStart, bodyEnd, bodyIndent));
                    i = (elseStart != -1) ? elseBodyEnd : bodyEnd;
                }
                else
                {
                    if (elseStart != -1)
                    {
                        string elseRaw = textLines[elseStart].TrimStart();
                        Match elifMatch2 = Regex.Match(elseRaw, @"^elif\s+(.+?)\s*:\s*$");

                        if (elifMatch2.Success)
                        {
                            string savedLine = textLines[elseStart];
                            textLines[elseStart] = new string(' ', expectedIndent) + "if " + elifMatch2.Groups[1].Value + ":";
                            yield return StartCoroutine(ExecuteBlock(elseStart, endLine, expectedIndent));
                            textLines[elseStart] = savedLine;
                        }
                        else
                        {
                            yield return StartCoroutine(ExecuteBlock(elseStart + 1, elseBodyEnd, bodyIndent));
                        }
                        i = elseBodyEnd;
                    }
                    else
                    {
                        i = bodyEnd;
                    }
                }
                ClearLine(i - 1);
                continue;
            }

            // ----------------------------------------------------------------
            // while loop
            // ----------------------------------------------------------------
            Match whileMatch = Regex.Match(line, @"^while\s+(.+?)\s*:\s*$");
            if (whileMatch.Success)
            {
                if (objTracker != null) objTracker.usedLoops += 1;

                string condition = whileMatch.Groups[1].Value;
                int bodyIndent = expectedIndent + 4;
                int bodyStart = i + 1;
                int bodyEnd = FindBlockEnd(bodyStart, endLine, bodyIndent);

                int iterations = 0;
                while (EvaluateCondition(condition) && !failed)
                {
                    iterations++;
                    if (iterations > MAX_LOOP_ITERATIONS)
                    {
                        Debug.LogWarning("Loop exceeded " + MAX_LOOP_ITERATIONS + " iterations. Stopping.");
                        ErrorLine(i);
                        breakAndReset();
                        yield break;
                    }
                    HighlightLine(i);
                    yield return StartCoroutine(ExecuteBlock(bodyStart, bodyEnd, bodyIndent));
                }

                i = bodyEnd;
                ClearLine(i - 1);
                continue;
            }

            // ----------------------------------------------------------------
            // for loop
            // ----------------------------------------------------------------
            Match forMatch = Regex.Match(line,
                @"^for\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+in\s+range\s*\(\s*(.+?)\s*\)\s*:\s*$");
            if (forMatch.Success)
            {
                if (objTracker != null) objTracker.usedLoops += 1;

                string loopVar = forMatch.Groups[1].Value;
                string rangeArgs = forMatch.Groups[2].Value;

                float rangeStart = 0f, rangeStop = 0f;
                string[] rangeParts = rangeArgs.Split(',');
                if (rangeParts.Length == 1)
                {
                    rangeStop = EvaluateExpression(rangeParts[0].Trim());
                }
                else if (rangeParts.Length == 2)
                {
                    rangeStart = EvaluateExpression(rangeParts[0].Trim());
                    rangeStop  = EvaluateExpression(rangeParts[1].Trim());
                }
                else
                {
                    Debug.LogError("range() only supports 1 or 2 arguments.");
                    failed = true;
                    ErrorLine(i);
                    break;
                }

                int bodyIndent = expectedIndent + 4;
                int bodyStart = i + 1;
                int bodyEnd = FindBlockEnd(bodyStart, endLine, bodyIndent);

                int iterations = 0;
                for (float f = rangeStart; f < rangeStop && !failed; f++)
                {
                    iterations++;
                    if (iterations > MAX_LOOP_ITERATIONS)
                    {
                        Debug.LogWarning("Loop exceeded " + MAX_LOOP_ITERATIONS + " iterations. Stopping.");
                        ErrorLine(i);
                        breakAndReset();
                        yield break;
                    }
                    SetVariable(loopVar, f);
                    HighlightLine(i);
                    yield return StartCoroutine(ExecuteBlock(bodyStart, bodyEnd, bodyIndent));
                }

                i = bodyEnd;
                ClearLine(i - 1);
                continue;
            }

            // ----------------------------------------------------------------
            // Player movement commands
            // ----------------------------------------------------------------

            Match addMatch = Regex.Match(line, @"^player\.x\s*\+=\s*(.+)$");
            if (addMatch.Success)
            {
                if (objTracker != null) objTracker.usedLines += 1;
                float amount = EvaluateExpression(addMatch.Groups[1].Value);
                if (!failed) { playerMovement.MoveRight(amount); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            Match subMatch = Regex.Match(line, @"^player\.x\s*-=\s*(.+)$");
            if (subMatch.Success)
            {
                if (objTracker != null) objTracker.usedLines += 1;
                float amount = EvaluateExpression(subMatch.Groups[1].Value);
                if (!failed) { playerMovement.MoveLeft(amount); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            Match jumpMatch = Regex.Match(line, @"^player\.Jump\s*\(\s*(.+?)\s*\)$");
            if (jumpMatch.Success)
            {
                if (objTracker != null) objTracker.usedLines += 1;
                float height = EvaluateExpression(jumpMatch.Groups[1].Value);
                if (!failed) { playerMovement.Jump(height); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            Match jrMatch = Regex.Match(line, @"^player\.JumpRight\s*\(\s*(.+?),\s*(.+?)\s*\)$");
            if (jrMatch.Success)
            {
                if (objTracker != null) objTracker.usedLines += 1;
                float distance = EvaluateExpression(jrMatch.Groups[1].Value);
                float height   = EvaluateExpression(jrMatch.Groups[2].Value);
                if (!failed) { playerMovement.JumpRight(distance, height); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            Match jlMatch = Regex.Match(line, @"^player\.JumpLeft\s*\(\s*(.+?),\s*(.+?)\s*\)$");
            if (jlMatch.Success)
            {
                if (objTracker != null) objTracker.usedLines += 1;
                float distance = EvaluateExpression(jlMatch.Groups[1].Value);
                float height   = EvaluateExpression(jlMatch.Groups[2].Value);
                if (!failed) { playerMovement.JumpLeft(distance, height); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            Match setX = Regex.Match(line, @"^player\.x\s*=\s*(.+)$");
            if (setX.Success)
            {
                if (objTracker != null) objTracker.usedLines += 1;
                float x = EvaluateExpression(setX.Groups[1].Value);
                if (!failed) { playerMovement.SetX(x); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            Match setY = Regex.Match(line, @"^player\.y\s*=\s*(.+)$");
            if (setY.Success)
            {
                if (objTracker != null) objTracker.usedLines += 1;
                float y = EvaluateExpression(setY.Groups[1].Value);
                if (!failed) { playerMovement.SetY(y); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            // ----------------------------------------------------------------
            // Variable assignment
            // ----------------------------------------------------------------

            Match varAdd = Regex.Match(line, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\+=\s*(.+)$");
            if (varAdd.Success && varAdd.Groups[1].Value != "player")
            {
                if (objTracker != null) objTracker.usedLines += 1;
                string varName = varAdd.Groups[1].Value;
                float current = ResolveToken(varName);
                float val = EvaluateExpression(varAdd.Groups[2].Value);
                if (!failed) { SetVariable(varName, current + val); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            Match varSub = Regex.Match(line, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*-=\s*(.+)$");
            if (varSub.Success && varSub.Groups[1].Value != "player")
            {
                if (objTracker != null) objTracker.usedLines += 1;
                string varName = varSub.Groups[1].Value;
                float current = ResolveToken(varName);
                float val = EvaluateExpression(varSub.Groups[2].Value);
                if (!failed) { SetVariable(varName, current - val); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            Match varMul = Regex.Match(line, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\*=\s*(.+)$");
            if (varMul.Success)
            {
                if (objTracker != null) objTracker.usedLines += 1;
                string varName = varMul.Groups[1].Value;
                float current = ResolveToken(varName);
                float val = EvaluateExpression(varMul.Groups[2].Value);
                if (!failed) { SetVariable(varName, current * val); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            Match varDiv = Regex.Match(line, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\/=\s*(.+)$");
            if (varDiv.Success)
            {
                if (objTracker != null) objTracker.usedLines += 1;
                string varName = varDiv.Groups[1].Value;
                float current = ResolveToken(varName);
                float val = EvaluateExpression(varDiv.Groups[2].Value);
                if (!failed && val != 0f) { SetVariable(varName, current / val); yield return new WaitForSeconds(lineDelay); }
                ClearLine(i); i++; continue;
            }

            Match setVar = Regex.Match(line, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(.+)$");
            if (setVar.Success && setVar.Groups[1].Value != "player")
            {
                if (objTracker != null) objTracker.usedLines += 1;
                string varName = setVar.Groups[1].Value;
                float varValue = EvaluateExpression(setVar.Groups[2].Value);
                if (!failed)
                {
                    SetVariable(varName, varValue);
                    Debug.Log("Set variable " + varName + " to " + varValue);
                    yield return new WaitForSeconds(lineDelay);
                }
                ClearLine(i); i++; continue;
            }

            // ----------------------------------------------------------------
            // Unrecognized line
            // ----------------------------------------------------------------
            ErrorLine(i);
            failed = true;
            yield return new WaitForSeconds(lineDelay);
            break;
        }
    }

    int FindBlockEnd(int startLine, int endLine, int bodyIndent)
    {
        for (int j = startLine; j < endLine; j++)
        {
            string raw = textLines[j];
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (GetIndent(raw) < bodyIndent)
                return j;
        }
        return endLine;
    }

    // -----------------------------------------------------------------------
    // Public control methods
    // -----------------------------------------------------------------------

    // Stops the running coroutine and clears line highlights.
    // Called by Movement when a spike is hit — does NOT move the player yet,
    // since the death anim still needs to play at the current position.
    public void StopCode()
    {
        if (codeRunning != null)
        {
            StopCoroutine(codeRunning);
            codeRunning = null;
        }

        foreach (var codeLine in codeLines)
            codeLine.color = Color.white;
    }

    // Resets all interpreter state and snaps the player back to their start position.
    // Called by Movement after the death animation finishes.
    public void FullReset()
    {
        variableNames.Clear();
        variableValues.Clear();
        failed = false;
        playerMovement.gameObject.transform.position = playerMovement.basePos;
    }

    // Legacy reset used by loop overflow and other in-code errors.
    // Stops everything and resets position immediately (no death anim).
    public void breakAndReset()
    {
        StopCode();
        FullReset();
    }
}
