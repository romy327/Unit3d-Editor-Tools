using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Slot & Hotspot Name Validator Tool
/// - Slot Validation: follows slot naming rules (_sep_, _hyp_, _dot_, etc.)
/// - Hotspot Validation: follows hotspot naming rules (_sep_ once, optional _hyp_, no _dot_)
/// - Auto-fix options: remove spaces, trim names, collapse "__" to "_"
/// </summary>
public class SlotNameValidatorWindow : EditorWindow
{
    private enum ValidationMode { Slots, Hotspots }
    private ValidationMode currentMode = ValidationMode.Slots;

    private GameObject targetParent;
    private Vector2 scrollPos;
    private List<(GameObject obj, string message)> invalidNames = new List<(GameObject, string)>();

    // Allowed endings for Slot "_dot_" tokens
    private readonly string[] allowedDotEndings = {
        "PV", "STATE", "MODE",
        "ActuatorCurrentPosition",
        "ActuatorDesiredPosition"
    };

    // Regex rules
    private Regex validPattern = new Regex(@"^[A-Za-z0-9_.]+$");
    private Regex unityDuplicatePattern = new Regex(@"\(\d+\)$");
    private Regex starsOrDashes = new Regex(@"[*-]{3,}");
    private Regex extraUnderscorePattern = new Regex(@"_{2,}");

    [MenuItem("Tools/Validate Slots & Hotspots")]
    public static void ShowWindow()
    {
        GetWindow<SlotNameValidatorWindow>("Slot/Hotspot Validator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Slot & Hotspot Name Validator", EditorStyles.boldLabel);

        // Mode toggle
        currentMode = (ValidationMode)GUILayout.Toolbar((int)currentMode, new string[] { "Validate Slots", "Validate Hotspots" });

        targetParent = (GameObject)EditorGUILayout.ObjectField(
            currentMode == ValidationMode.Slots ? "Slot Parent" : "Hotspot Parent",
            targetParent,
            typeof(GameObject),
            true
        );

        if (GUILayout.Button("Validate Names"))
        {
            if (targetParent == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a parent GameObject.", "OK");
                return;
            }

            if (currentMode == ValidationMode.Slots)
                ValidateSlots();
            else
                ValidateHotspots();
        }

        if (invalidNames.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("Invalid Children:", EditorStyles.boldLabel);

            // Fix buttons for Slots
            if (currentMode == ValidationMode.Slots)
            {
                if (GUILayout.Button("✂ Fix Slot Names (Spaces + Extra Underscores)"))
                {
                    FixSlots();
                }
            }

            // Fix buttons for Hotspots
            if (currentMode == ValidationMode.Hotspots)
            {
                if (GUILayout.Button("✂ Fix Hotspot Names (Spaces + Extra Underscores)"))
                {
                    FixHotspots();
                }
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Height(250));

            for (int i = invalidNames.Count - 1; i >= 0; i--)
            {
                var (obj, msg) = invalidNames[i];

                if (obj == null)
                {
                    invalidNames.RemoveAt(i);
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUI.color = Color.red;
                GUILayout.Label(obj.name);
                GUI.color = Color.white;

                GUILayout.Label("→ " + msg, GUILayout.MaxWidth(400));

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeGameObject = obj;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }
        else if (targetParent != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("✅ No invalid child names found!", EditorStyles.helpBox);
        }
    }

    // ---------------- SLOT VALIDATION ----------------
    private void ValidateSlots()
    {
        invalidNames.Clear();
        HashSet<string> seenNames = new HashSet<string>();

        foreach (Transform child in targetParent.transform)
        {
            string name = child.name;
            string cleanedName = name.Replace("_sep_", "").Replace("_hyp_", "").Replace("_dot_", "");
            List<string> reasons = new List<string>();

            bool isUntagged = IsUntaggedSlot(name);

            if (isUntagged)
            {
                if (string.IsNullOrWhiteSpace(name) || name.Contains(" "))
                    reasons.Add("Contains space or is empty");

                if (!validPattern.IsMatch(cleanedName))
                    reasons.Add("Contains invalid special characters");

                // Check original name for consecutive underscores (detects "__dot_" too)
                if (extraUnderscorePattern.IsMatch(name))
                    reasons.Add("Contains extra underscores");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(name) || name.Contains(" "))
                    reasons.Add("Contains space or is empty");

                if (!validPattern.IsMatch(cleanedName))
                    reasons.Add("Contains invalid special characters");

                if (unityDuplicatePattern.IsMatch(name))
                    reasons.Add("Unity auto-duplicate suffix (e.g., (1))");

                if (starsOrDashes.IsMatch(name))
                    reasons.Add("Contains long repeated * or -");

                // IMPORTANT: check original name for extra underscores so "__dot_" is flagged
                if (extraUnderscorePattern.IsMatch(name))
                    reasons.Add("Contains extra underscores");

                // _sep_ count must be exactly 2
                int sepCount = Regex.Matches(name, "_sep_", RegexOptions.IgnoreCase).Count;
                if (sepCount != 2)
                {
                    reasons.Add($"_sep_ must appear exactly twice (found {sepCount})");
                }
                else
                {
                    if (!(name.StartsWith("Slot_sep_") || name.StartsWith("heightslot_sep_")))
                        reasons.Add("First _sep_ must come immediately after 'Slot_' or 'heightslot_'");
                }

                // _dot_ rules
                int dotCount = Regex.Matches(name, "_dot_", RegexOptions.IgnoreCase).Count;
                if (dotCount > 1)
                {
                    reasons.Add("_dot_ appears more than once");
                }
                else if (dotCount == 1)
                {
                    string afterDot = name.Substring(name.IndexOf("_dot_") + 5);
                    bool allowed = false;
                    foreach (string allowedEnding in allowedDotEndings)
                    {
                        if (afterDot.Equals(allowedEnding, System.StringComparison.OrdinalIgnoreCase))
                        {
                            allowed = true;
                            break;
                        }
                    }
                    if (!allowed)
                        reasons.Add($"Invalid ending after _dot_: \"{afterDot}\"");
                }

                if (seenNames.Contains(name))
                    reasons.Add("Duplicate name in same parent");
                else
                    seenNames.Add(name);
            }

            if (reasons.Count > 0)
                invalidNames.Add((child.gameObject, string.Join(", ", reasons)));
        }
    }

    private bool IsUntaggedSlot(string name)
    {
        return !name.Contains("_hyp_") && !name.Contains("_dot_") &&
               (name.StartsWith("Slot_sep_") || name.StartsWith("heightslot_sep_"));
    }

    // ---------------- HOTSPOT VALIDATION ----------------
    private void ValidateHotspots()
    {
        invalidNames.Clear();
        HashSet<string> seenNames = new HashSet<string>();

        foreach (Transform child in targetParent.transform)
        {
            string name = child.name;
            List<string> reasons = new List<string>();

            if (string.IsNullOrWhiteSpace(name) || name.Contains(" "))
                reasons.Add("Contains space or is empty");

            if (extraUnderscorePattern.IsMatch(name))
                reasons.Add("Contains extra underscores");

            if (!name.StartsWith("Hotspot_sep_"))
                reasons.Add("Must start with 'Hotspot_sep_'");

            // Must contain exactly 1 _sep_
            int sepCount = Regex.Matches(name, "_sep_", RegexOptions.IgnoreCase).Count;
            if (sepCount != 1)
                reasons.Add($"Hotspot must contain exactly 1 '_sep_' (found {sepCount})");

            // No _dot_ allowed
            if (name.Contains("_dot_"))
                reasons.Add("Hotspots cannot contain '_dot_'");

            // Only alphanumeric + underscores allowed
            if (!Regex.IsMatch(name, @"^[A-Za-z0-9_]+$"))
                reasons.Add("Contains invalid characters (only A-Z, 0-9, and _ allowed)");

            // Duplicate check
            if (seenNames.Contains(name))
                reasons.Add("Duplicate Hotspot name in same parent");
            else
                seenNames.Add(name);

            if (reasons.Count > 0)
                invalidNames.Add((child.gameObject, string.Join(", ", reasons)));
        }
    }

    // ---------------- FIXERS ----------------
    private void FixSlots()
    {
        for (int i = invalidNames.Count - 1; i >= 0; i--)
        {
            var (obj, msg) = invalidNames[i];
            if (obj == null) continue;

            Undo.RecordObject(obj, "Fix Slot Name");

            string fixedName = obj.name.Trim();

            // Remove spaces
            fixedName = fixedName.Replace(" ", "");

            // Replace multiple underscores with single
            fixedName = Regex.Replace(fixedName, "_{2,}", "_");

            if (!string.Equals(obj.name, fixedName, System.StringComparison.Ordinal))
            {
                obj.name = fixedName;
                EditorUtility.SetDirty(obj);
            }
        }
        ValidateSlots();
    }

    private void FixHotspots()
    {
        for (int i = invalidNames.Count - 1; i >= 0; i--)
        {
            var (obj, msg) = invalidNames[i];
            if (obj == null) continue;

            Undo.RecordObject(obj, "Fix Hotspot Name");

            string fixedName = obj.name.Trim();

            // Remove spaces
            fixedName = fixedName.Replace(" ", "");

            // Replace multiple underscores with single
            fixedName = Regex.Replace(fixedName, "_{2,}", "_");

            if (!string.Equals(obj.name, fixedName, System.StringComparison.Ordinal))
            {
                obj.name = fixedName;
                EditorUtility.SetDirty(obj);
            }
        }
        ValidateHotspots();
    }
}
