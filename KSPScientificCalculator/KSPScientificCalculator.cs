using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;
using ToolbarControl_NS;

namespace KSPScientificCalculator
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        public void Start()
        {
            ToolbarControl.RegisterMod(CalculatorCore.MODID, CalculatorCore.MODNAME);
            DontDestroyOnLoad(this);
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class CalculatorFlight : CalculatorCore { }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class CalculatorEditor : CalculatorCore { }

    public class CalculatorCore : MonoBehaviour
    {
        internal const string MODID = "KSPScientificCalculator_NS";
        internal const string MODNAME = "Scientific Calculator";
        internal const string WINDOW_TITLE = "Scientific Calculator";
        internal const string SETTINGS_FILE = "GameData/KSPScientificCalculator/PluginData/settings.cfg";

        private const float DefaultWidth = 370f;
        private const float DefaultHeight = 470f;

        private ToolbarControl toolbarControl;
        private Rect windowRect = new Rect(300f, 120f, DefaultWidth, DefaultHeight);
        private int windowId;
        private bool visible;
        private bool degreesMode = true;
        private bool useCompactButtons;
        private string expression = string.Empty;
        private string result = "0";
        private string status = string.Empty;
        private string lastAnswer = "0";
        private Vector2 historyScroll;
        private readonly List<string> history = new List<string>();
        private bool stylesInitialized;
        private GUIStyle displayStyle;
        private GUIStyle resultStyle;
        private GUIStyle historyStyle;
        private GUIStyle statusStyle;
        private string pendingInsert;
        private bool pendingEvaluate;
        private bool pendingBackspace;
        private bool pendingClear;
        private bool pendingToggleSign;
        private bool pendingDegToggle;
        private bool pendingClearHistory;
        private bool pendingClose;

        public void Awake()
        {
            DontDestroyOnLoad(this);
            windowId = Guid.NewGuid().GetHashCode();
            LoadSettings();
        }

        public void Start()
        {
            CreateToolbarButton();
        }

        public void OnDestroy()
        {
            SaveSettings();
            DestroyToolbarButton();
        }

        public void OnApplicationQuit()
        {
            SaveSettings();
        }

        public void Update()
        {
            if (visible)
                HandleKeyboard();
        }

        public void OnGUI()
        {
            if (!visible)
                return;

            InitializeStyles();
            //GUI.skin = HighLogic.Skin;
            windowRect = ClickThruBlocker.GUILayoutWindow(windowId, windowRect, DrawWindow, WINDOW_TITLE);
            ClampWindowToScreen();
        }

        private void CreateToolbarButton()
        {
            if (toolbarControl != null)
                return;

            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(
                OnToolbarTrue,
                OnToolbarFalse,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                MODID,
                "ScientificCalculatorButton",
                "KSPScientificCalculator/PluginData/Textures/icon_38",
                "KSPScientificCalculator/PluginData/Textures/icon_24",
                MODNAME
            );
        }

        private void DestroyToolbarButton()
        {
            if (toolbarControl != null)
            {
                Destroy(toolbarControl);
                toolbarControl = null;
            }
        }

        private void OnToolbarTrue() { visible = true; }
        private void OnToolbarFalse() { visible = false; }

        private void ToggleVisible()
        {
            visible = !visible;
            if (toolbarControl != null)
            {
                if (visible) toolbarControl.SetTrue(false);
                else toolbarControl.SetFalse(false);
            }
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            DrawTopBar();
            DrawDisplayArea();
            DrawButtonGrid();
            DrawHistoryArea();
            DrawStatusArea();
            GUILayout.EndVertical();
            ProcessPendingActions();
            GUI.DragWindow(new Rect(0, 0, 10000, 22));
        }

        private void DrawTopBar()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(degreesMode ? "DEG" : "RAD", GUILayout.Width(56f), GUILayout.Height(26f))) pendingDegToggle = true;
            if (GUILayout.Button("Ans", GUILayout.Width(48f), GUILayout.Height(26f))) pendingInsert = "Ans";
            if (GUILayout.Button("π", GUILayout.Width(38f), GUILayout.Height(26f))) pendingInsert = "pi";
            if (GUILayout.Button("e", GUILayout.Width(38f), GUILayout.Height(26f))) pendingInsert = "e";
            if (GUILayout.Button("±", GUILayout.Width(38f), GUILayout.Height(26f))) pendingToggleSign = true;
            if (GUILayout.Button(useCompactButtons ? "Wide" : "Compact", GUILayout.Width(70f), GUILayout.Height(26f))) useCompactButtons = !useCompactButtons;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(28f), GUILayout.Height(26f))) pendingClose = true;
            GUILayout.EndHorizontal();
        }

        private void DrawDisplayArea()
        {
            GUILayout.BeginVertical(HighLogic.Skin.box);
            GUILayout.Label(string.IsNullOrEmpty(expression) ? " " : expression, displayStyle, GUILayout.MinHeight(42f));
            GUILayout.Label(result, resultStyle, GUILayout.MinHeight(34f));
            GUILayout.EndVertical();
        }

        private void DrawButtonGrid()
        {
            float buttonHeight = useCompactButtons ? 28f : 34f;
            float buttonWidth = useCompactButtons ? 54f : 64f;

            DrawButtonRow(buttonHeight, buttonWidth,
                new ButtonDef("sin(", "sin("),
                new ButtonDef("cos(", "cos("),
                new ButtonDef("tan(", "tan("),
                new ButtonDef("sinh(", "sinh("),
                new ButtonDef("cosh(", "cosh("));

            DrawButtonRow(buttonHeight, buttonWidth,
                new ButtonDef("asin(", "asin("),
                new ButtonDef("acos(", "acos("),
                new ButtonDef("atan(", "atan("),
                new ButtonDef("tanh(", "tanh("),
                new ButtonDef("sqrt(", "sqrt("));

            DrawButtonRow(buttonHeight, buttonWidth,
                new ButtonDef("(", "("),
                new ButtonDef(")", ")"),
                new ButtonDef("abs(", "abs("),
                new ButtonDef("exp(", "exp("),
                new ButtonDef("^", "^"));

            DrawButtonRow(buttonHeight, buttonWidth,
                new ButtonDef("7", "7"),
                new ButtonDef("8", "8"),
                new ButtonDef("9", "9"),
                new ButtonDef("/", "/"),
                new ButtonDef("Del", null, ButtonAction.Backspace));

            DrawButtonRow(buttonHeight, buttonWidth,
                new ButtonDef("4", "4"),
                new ButtonDef("5", "5"),
                new ButtonDef("6", "6"),
                new ButtonDef("*", "*"),
                new ButtonDef("C", null, ButtonAction.Clear));

            DrawButtonRow(buttonHeight, buttonWidth,
                new ButtonDef("1", "1"),
                new ButtonDef("2", "2"),
                new ButtonDef("3", "3"),
                new ButtonDef("-", "-"),
                new ButtonDef("Hist", null, ButtonAction.ClearHistory));

            DrawButtonRow(buttonHeight, buttonWidth,
                new ButtonDef("0", "0"),
                new ButtonDef(".", "."),
                new ButtonDef("Ans", "Ans"),
                new ButtonDef("+", "+"),
                new ButtonDef("=", null, ButtonAction.Evaluate));
        }

        private void DrawButtonRow(float buttonHeight, float buttonWidth, params ButtonDef[] buttons)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < buttons.Length; i++)
            {
                ButtonDef def = buttons[i];
                if (GUILayout.Button(def.Text, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                {
                    switch (def.Action)
                    {
                        case ButtonAction.Insert: pendingInsert = def.InsertText; break;
                        case ButtonAction.Evaluate: pendingEvaluate = true; break;
                        case ButtonAction.Backspace: pendingBackspace = true; break;
                        case ButtonAction.Clear: pendingClear = true; break;
                        case ButtonAction.ClearHistory: pendingClearHistory = true; break;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawHistoryArea()
        {
            GUILayout.BeginVertical(HighLogic.Skin.box, GUILayout.Height(120f));
            GUILayout.Label("History", HighLogic.Skin.label);
            historyScroll = GUILayout.BeginScrollView(historyScroll, GUILayout.Height(92f));
            for (int i = history.Count - 1; i >= 0; i--)
            {
                string entry = history[i];
                if (GUILayout.Button(entry, historyStyle))
                {
                    int pos = entry.IndexOf(" = ", StringComparison.Ordinal);
                    expression = pos > 0 ? entry.Substring(0, pos) : entry;
                    status = "History entry recalled";
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawStatusArea()
        {
            GUILayout.BeginVertical(HighLogic.Skin.box);
            GUILayout.Label(string.IsNullOrEmpty(status) ? "Ready" : status, statusStyle, GUILayout.MinHeight(22f));
            GUILayout.EndVertical();
        }

        private void ProcessPendingActions()
        {
            if (pendingClose) { pendingClose = false; ToggleVisible(); }
            if (pendingDegToggle) { pendingDegToggle = false; degreesMode = !degreesMode; status = degreesMode ? "Angle mode: degrees" : "Angle mode: radians"; }
            if (pendingToggleSign) { pendingToggleSign = false; ToggleSign(); }
            if (pendingInsert != null) { InsertText(pendingInsert); pendingInsert = null; }
            if (pendingBackspace) { pendingBackspace = false; Backspace(); }
            if (pendingClear) { pendingClear = false; expression = string.Empty; result = "0"; status = "Cleared"; }
            if (pendingClearHistory) { pendingClearHistory = false; history.Clear(); status = "History cleared"; }
            if (pendingEvaluate) { pendingEvaluate = false; EvaluateExpression(); }
        }

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.F10)) { ToggleVisible(); return; }
            string input = Input.inputString;
            if (!string.IsNullOrEmpty(input))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    char c = input[i];
                    if (char.IsDigit(c) || c == '.' || c == '+' || c == '-' || c == '*' || c == '/' || c == '%' || c == '(' || c == ')' || c == '^')
                        InsertText(c.ToString());
                }
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Equals)) EvaluateExpression();
            if (Input.GetKeyDown(KeyCode.Backspace)) Backspace();
            if (Input.GetKeyDown(KeyCode.Delete)) { expression = string.Empty; result = "0"; status = "Cleared"; }
        }

        private void InsertText(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                expression += text;
                status = "Input updated";
            }
        }

        private void Backspace()
        {
            if (!string.IsNullOrEmpty(expression))
            {
                expression = expression.Substring(0, expression.Length - 1);
                status = "Deleted last character";
            }
        }

        private void ToggleSign()
        {
            expression = string.IsNullOrEmpty(expression) ? "-" : "-(" + expression + ")";
            status = "Sign toggled";
        }

        private void EvaluateExpression()
        {
            string trimmed = expression == null ? string.Empty : expression.Trim();
            if (string.IsNullOrEmpty(trimmed)) { status = "Enter an expression"; return; }
            try
            {
                ScientificExpressionParser parser = new ScientificExpressionParser(trimmed, degreesMode, ParseDouble(lastAnswer));
                double value = parser.Parse();
                if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Result is not a real finite number");
                result = FormatDouble(value);
                lastAnswer = result;
                history.Add(trimmed + " = " + result);
                while (history.Count > 50) history.RemoveAt(0);
                status = "OK";
            }
            catch (Exception ex)
            {
                result = "Error";
                status = ex.Message;
                Debug.Log("[KSPScientificCalculator] Evaluation error: " + ex);
            }
        }

        private static double ParseDouble(string s)
        {
            double value;
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) value = 0d;
            return value;
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.###############", CultureInfo.InvariantCulture);
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;
            displayStyle = new GUIStyle(HighLogic.Skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 16, wordWrap = true };
            resultStyle = new GUIStyle(HighLogic.Skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 20, fontStyle = FontStyle.Bold };
            historyStyle = new GUIStyle(HighLogic.Skin.button) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            statusStyle = new GUIStyle(HighLogic.Skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 12, wordWrap = true };
            stylesInitialized = true;
        }

        private void ClampWindowToScreen()
        {
            float maxX = Mathf.Max(0f, Screen.width - windowRect.width);
            float maxY = Mathf.Max(0f, Screen.height - windowRect.height);
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, maxX);
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, maxY);
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SETTINGS_FILE)) return;
                ConfigNode node = ConfigNode.Load(SETTINGS_FILE);
                if (node == null) return;
                float x = ParseFloat(node.GetValue("windowX"), windowRect.x);
                float y = ParseFloat(node.GetValue("windowY"), windowRect.y);
                float w = ParseFloat(node.GetValue("windowW"), windowRect.width);
                float h = ParseFloat(node.GetValue("windowH"), windowRect.height);
                windowRect = new Rect(x, y, w, h);
                degreesMode = ParseBool(node.GetValue("degreesMode"), true);
                useCompactButtons = ParseBool(node.GetValue("useCompactButtons"), false);
                visible = ParseBool(node.GetValue("visible"), false);
            }
            catch (Exception ex)
            {
                Debug.Log("[KSPScientificCalculator] LoadSettings failed: " + ex);
            }
        }

        private void SaveSettings()
        {
            try
            {
                string dir = Path.GetDirectoryName(SETTINGS_FILE);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                ConfigNode node = new ConfigNode("KSPScientificCalculatorSettings");
                node.AddValue("windowX", windowRect.x.ToString(CultureInfo.InvariantCulture));
                node.AddValue("windowY", windowRect.y.ToString(CultureInfo.InvariantCulture));
                node.AddValue("windowW", windowRect.width.ToString(CultureInfo.InvariantCulture));
                node.AddValue("windowH", windowRect.height.ToString(CultureInfo.InvariantCulture));
                node.AddValue("degreesMode", degreesMode);
                node.AddValue("useCompactButtons", useCompactButtons);
                node.AddValue("visible", visible);
                node.Save(SETTINGS_FILE);
            }
            catch (Exception ex)
            {
                Debug.Log("[KSPScientificCalculator] SaveSettings failed: " + ex);
            }
        }

        private static float ParseFloat(string value, float fallback)
        {
            float parsed;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }

        private static bool ParseBool(string value, bool fallback)
        {
            bool parsed;
            return bool.TryParse(value, out parsed) ? parsed : fallback;
        }

        private struct ButtonDef
        {
            public readonly string Text;
            public readonly string InsertText;
            public readonly ButtonAction Action;
            public ButtonDef(string text, string insertText) { Text = text; InsertText = insertText; Action = ButtonAction.Insert; }
            public ButtonDef(string text, string insertText, ButtonAction action) { Text = text; InsertText = insertText; Action = action; }
        }

        private enum ButtonAction { Insert, Evaluate, Backspace, Clear, ClearHistory }
    }

    internal class ScientificExpressionParser
    {
        private readonly string text;
        private readonly bool degreesMode;
        private readonly double ansValue;
        private int index;

        public ScientificExpressionParser(string expression, bool degreesMode, double ansValue)
        {
            this.text = expression ?? string.Empty;
            this.degreesMode = degreesMode;
            this.ansValue = ansValue;
            index = 0;
        }

        public double Parse()
        {
            double value = ParseExpression();
            SkipWhitespace();
            if (index < text.Length) throw new Exception("Unexpected token at position " + index);
            return value;
        }

        private double ParseExpression()
        {
            double value = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (Match('+')) value += ParseTerm();
                else if (Match('-')) value -= ParseTerm();
                else break;
            }
            return value;
        }

        private double ParseTerm()
        {
            double value = ParsePower();
            while (true)
            {
                SkipWhitespace();
                if (Match('*')) value *= ParsePower();
                else if (Match('/')) value /= ParsePower();
                else if (Match('%')) value %= ParsePower();
                else break;
            }
            return value;
        }

        private double ParsePower()
        {
            double left = ParseUnary();
            SkipWhitespace();
            if (Match('^')) return Math.Pow(left, ParsePower());
            return left;
        }

        private double ParseUnary()
        {
            SkipWhitespace();
            if (Match('+')) return ParseUnary();
            if (Match('-')) return -ParseUnary();
            return ParsePrimary();
        }

        private double ParsePrimary()
        {
            SkipWhitespace();
            if (Match('('))
            {
                double inner = ParseExpression();
                Expect(')');
                return inner;
            }

            if (IsLetter(CurrentChar()))
            {
                string ident = ParseIdentifier().ToLowerInvariant();
                if (ident == "pi") return Math.PI;
                if (ident == "e") return Math.E;
                if (ident == "ans") return ansValue;
                SkipWhitespace();
                if (!Match('(')) throw new Exception("Function requires parentheses: " + ident);
                double arg = ParseExpression();
                Expect(')');
                return EvaluateFunction(ident, arg);
            }

            return ParseNumber();
        }

        private double EvaluateFunction(string ident, double arg)
        {
            switch (ident)
            {
                case "sin": return Math.Sin(ToRadians(arg));
                case "cos": return Math.Cos(ToRadians(arg));
                case "tan": return Math.Tan(ToRadians(arg));
                case "sinh": return Math.Sinh(arg);
                case "cosh": return Math.Cosh(arg);
                case "tanh": return Math.Tanh(arg);
                case "asin": return FromRadians(Math.Asin(arg));
                case "acos": return FromRadians(Math.Acos(arg));
                case "atan": return FromRadians(Math.Atan(arg));
                case "sqrt": return Math.Sqrt(arg);
                case "ln": return Math.Log(arg);
                case "log": return Math.Log10(arg);
                case "abs": return Math.Abs(arg);
                case "exp": return Math.Exp(arg);
                case "floor": return Math.Floor(arg);
                case "ceil":
                case "ceiling": return Math.Ceiling(arg);
                case "round": return Math.Round(arg);
                default: throw new Exception("Unknown function: " + ident);
            }
        }

        private double ParseNumber()
        {
            SkipWhitespace();
            int start = index;
            bool hasDecimal = false;
            while (index < text.Length)
            {
                char c = text[index];
                if (char.IsDigit(c)) { index++; continue; }
                if (c == '.' && !hasDecimal) { hasDecimal = true; index++; continue; }
                if ((c == 'e' || c == 'E') && index + 1 < text.Length)
                {
                    index++;
                    if (index < text.Length && (text[index] == '+' || text[index] == '-')) index++;
                    while (index < text.Length && char.IsDigit(text[index])) index++;
                    break;
                }
                break;
            }
            if (start == index) throw new Exception("Number expected at position " + index);
            string numberText = text.Substring(start, index - start);
            double value;
            if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) throw new Exception("Invalid number: " + numberText);
            return value;
        }

        private string ParseIdentifier()
        {
            int start = index;
            while (index < text.Length && (IsLetter(text[index]) || char.IsDigit(text[index]) || text[index] == '_')) index++;
            return text.Substring(start, index - start);
        }

        private void Expect(char c)
        {
            SkipWhitespace();
            if (!Match(c)) throw new Exception("Expected '" + c + "' at position " + index);
        }

        private bool Match(char c)
        {
            if (index < text.Length && text[index] == c) { index++; return true; }
            return false;
        }

        private void SkipWhitespace() { while (index < text.Length && char.IsWhiteSpace(text[index])) index++; }
        private char CurrentChar() { return index >= text.Length ? '\0' : text[index]; }
        private static bool IsLetter(char c) { return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'); }
        private double ToRadians(double value) { return degreesMode ? (value * Math.PI / 180d) : value; }
        private double FromRadians(double value) { return degreesMode ? (value * 180d / Math.PI) : value; }
    }
}
