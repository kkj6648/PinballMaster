using ExcelDataReader;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ExcelToSkillJsonConverter : EditorWindow
{
    [MenuItem("Tools/Excel To Skill JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<ExcelToSkillJsonConverter>("Skill Data Converter");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Select Skill Excel And Convert"))
        {
            string path = EditorUtility.OpenFilePanel(
                "Select Skill Excel File",
                "",
                "xlsx"
            );

            if (!string.IsNullOrEmpty(path))
            {
                Convert(path);
            }
        }
    }

    private void Convert(string filePath)
    {
        using (var stream = File.Open(
                   filePath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.ReadWrite))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                DataSet result = reader.AsDataSet();

                if (result.Tables.Count <= 0)
                {
                    Debug.LogError("Excel sheet not found.");
                    return;
                }

                DataTable table = result.Tables[0];

                SkillDatabase database = new SkillDatabase
                {
                    skills = new List<SkillData>()
                };


                int nameCol = FindColumnIndex(table, "Name");
                int levelCol = FindColumnIndex(table, "Level");
                int typeCol = FindColumnIndex(table, "Type");

                int damageCol = FindColumnIndex(table, "Damage");
                int durationCol = FindColumnIndex(table, "Duration");
                int stackCol = FindColumnIndex(table, "Stack");
                int dotCol = FindColumnIndex(table, "DOT");

                int rateCol = FindColumnIndex(table, "Rate");
                int speedCol = FindColumnIndex(table, "Speed");

                int additionalDamageCol = FindColumnIndex(table, "AdditionalDamage");

                if (additionalDamageCol < 0)
                {
                    additionalDamageCol = FindColumnIndex(table, "AddtionalDamage");

                }

                int rowDamageCol = FindColumnIndex(table, "RowDamage");
                int boomDamageCol = FindColumnIndex(table, "BoomDamage");
                int fragmentDamageCol = FindColumnIndex(table, "FragmentDamage");

                int criticalRateCol = FindColumnIndex(table, "CriticalRate");


                int explanationCol = FindColumnIndex(table, "Explanation");



                if (nameCol < 0 || levelCol < 0 || typeCol < 0 || explanationCol< 0 ) 
                {
                    Debug.LogError("Excel must have columns: Name, Level, Type");
                    return;
                }

                HashSet<string> duplicateCheck = new HashSet<string>();


                // Insert Info
                for (int row = 1; row < table.Rows.Count; row++)
                {

                    string skillName = GetCellString(table, row, nameCol).Trim();

                    int level = GetCellInt(table, row, levelCol);

                    string type = GetCellString(table, row, typeCol).Trim().ToLower();

                    string explanation = GetCellString(table, row, explanationCol).Trim().ToLower();


                    if (string.IsNullOrEmpty(skillName))
                        continue;

                    if (level <= 0)
                    {
                        Debug.LogWarning(
                            $"Invalid skill level at Excel row {row + 1}"
                        );

                        continue;
                    }

                    if (type != "active" && type != "passive" && type != "fallback")
                    {
                        Debug.LogWarning($"Invalid skill type at Excel row {row + 1}: {type}");
                        continue;
                    }

                    string duplicateKey = $"{skillName.ToLower()}_{level}";


                    if (!duplicateCheck.Add(duplicateKey))
                    {
                        Debug.LogWarning($"Duplicate skill data at Excel row {row + 1}: " + $"{skillName} Lv.{level}");

                        continue;
                    }

                    SkillData data = new SkillData
                    {
                        name = skillName,
                        level = level,
                        type = type,

                        damage = GetCellInt(
                            table,
                            row,
                            damageCol
                        ),

                        duration = GetCellFloat(
                            table,
                            row,
                            durationCol
                        ),

                        maxStack = GetCellInt(
                            table,
                            row,
                            stackCol
                        ),

                        dotDamage = GetCellInt(
                            table,
                            row,
                            dotCol
                        ),

                        rate = GetCellFloat(
                            table,
                            row,
                            rateCol
                        ),

                        speed = GetCellFloat(
                            table,
                            row,
                            speedCol
                        ),

                        additionalDamage = GetCellInt(
                            table,
                            row,
                            additionalDamageCol
                        ),

                        rowDamage = GetCellInt(
                            table,
                            row,
                            rowDamageCol
                        ),

                        boomDamage = GetCellInt(
                            table,
                            row,
                            boomDamageCol
                        ),

                        fragmentDamage = GetCellInt(
                            table,
                            row,
                            fragmentDamageCol
                        ),

                        criticalRate = GetCellFloat(
                            table,
                            row,
                            criticalRateCol
                        ),

                        explanation = explanation

                    };

                    database.skills.Add(data);
                }

        
                database.skills.Sort((a, b) =>
                {
                    int nameCompare = string.Compare(
                        a.name,
                        b.name,
                        System.StringComparison.OrdinalIgnoreCase
                    );

                    if (nameCompare != 0)
                        return nameCompare;

                    return a.level.CompareTo(b.level);
                });

                string json = JsonConvert.SerializeObject(
                    database,
                    Formatting.Indented
                );

                string folderPath =
                    Application.dataPath + "/SkillData";

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string savePath =
                    folderPath + "/SkillData.json";

                File.WriteAllText(savePath, json);

                AssetDatabase.Refresh();

                Debug.Log(
                    $"Skill JSON Convert Success: {savePath}\n" +
                    $"Converted Skill Level Count: {database.skills.Count}"
                );
            }
        }
    }

    private int FindColumnIndex(
        DataTable table,
        string columnName)
    {
        if (table == null || table.Rows.Count <= 0)
            return -1;

        string target = columnName.Trim().ToLower();

        for (int col = 0; col < table.Columns.Count; col++)
        {
            string value = GetCellString(
                table,
                0,
                col
            );

            if (string.IsNullOrWhiteSpace(value))
                continue;

            value = value.Trim().ToLower();

            if (value == target)
                return col;
        }

        return -1;
    }

    private string GetCellString(
        DataTable table,
        int row,
        int col)
    {
        if (!IsValidCell(table, row, col))
            return "";

        object value = table.Rows[row][col];

        if (value == null || value == System.DBNull.Value)
            return "";

        return value.ToString();
    }

    private int GetCellInt(
        DataTable table,
        int row,
        int col)
    {
        if (!IsValidCell(table, row, col))
            return 0;

        object value = table.Rows[row][col];

        if (value == null || value == System.DBNull.Value)
            return 0;

        if (value is double doubleValue)
        {
            return Mathf.RoundToInt((float)doubleValue);
        }

        if (value is float floatValue)
        {
            return Mathf.RoundToInt(floatValue);
        }

        if (value is int intValue)
        {
            return intValue;
        }

        string text = value.ToString().Trim();

        if (int.TryParse(text, out int intResult))
        {
            return intResult;
        }

        if (float.TryParse(text, out float floatResult))
        {
            return Mathf.RoundToInt(floatResult);
        }

        return 0;
    }

    private float GetCellFloat(
        DataTable table,
        int row,
        int col)
    {
        if (!IsValidCell(table, row, col))
            return 0f;

        object value = table.Rows[row][col];

        if (value == null || value == System.DBNull.Value)
            return 0f;

        if (value is double doubleValue)
        {
            return (float)doubleValue;
        }

        if (value is float floatValue)
        {
            return floatValue;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        string text = value.ToString().Trim();

        if (float.TryParse(text, out float result))
        {
            return result;
        }

        return 0f;
    }

    private bool IsValidCell(
        DataTable table,
        int row,
        int col)
    {
        if (table == null)
            return false;

        if (row < 0 || row >= table.Rows.Count)
            return false;

        if (col < 0 || col >= table.Columns.Count)
            return false;

        return true;
    }
}