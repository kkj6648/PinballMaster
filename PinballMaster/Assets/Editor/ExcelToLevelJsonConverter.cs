using ExcelDataReader;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ExcelToLevelExpJsonConverter : EditorWindow
{
    [MenuItem("Tools/Excel To Level EXP JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<ExcelToLevelExpJsonConverter>("Level EXP Converter");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Select Level EXP Excel And Convert"))
        {
            string path = EditorUtility.OpenFilePanel(
                "Select Level EXP Excel File",
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
     

        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                DataSet result = reader.AsDataSet();
                DataTable table = result.Tables[0];

                LevelExpDatabase database = new LevelExpDatabase();

                int levelCol = FindColumnIndex(table, "level");
                int requiredExpCol = FindColumnIndex(table, "requiredExp");

                if (levelCol < 0 || requiredExpCol < 0)
                {
                    Debug.LogError("Excel must have columns: level, requiredExp");
                    return;
                }

                for (int row = 1; row < table.Rows.Count; row++)
                {
                    int level = GetCellInt(table, row, levelCol);
                    int requiredExp = GetCellInt(table, row, requiredExpCol);

                    if (level <= 0)
                        continue;

                    if (requiredExp <= 0)
                    {
                        Debug.LogWarning($"Invalid requiredExp at row {row + 1}");
                        continue;
                    }

                    LevelExpData data = new LevelExpData();
                    data.level = level;
                    data.requiredExp = requiredExp;

                    database.levels.Add(data);
                }

                database.levels.Sort((a, b) => a.level.CompareTo(b.level));

                string json = JsonConvert.SerializeObject(database, Formatting.Indented);

                string folderPath = Application.dataPath + "/LevelData";

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string savePath = folderPath + "/LevelExpData.json";

                File.WriteAllText(savePath, json);

                AssetDatabase.Refresh();

                Debug.Log($"Level EXP JSON Convert Success: {savePath}");
            }
        }
    }

    private int FindColumnIndex(DataTable table, string columnName)
    {
        if (table.Rows.Count <= 0)
            return -1;

        string target = columnName.Trim().ToLower();

        for (int col = 0; col < table.Columns.Count; col++)
        {
            string value = GetCellString(table, 0, col);

            if (string.IsNullOrEmpty(value))
                continue;

            value = value.Trim().ToLower();

            if (value == target)
            {
                return col;
            }
        }

        return -1;
    }

    private string GetCellString(DataTable table, int row, int col)
    {
        if (row < 0 || row >= table.Rows.Count)
            return "";

        if (col < 0 || col >= table.Columns.Count)
            return "";

        object value = table.Rows[row][col];

        if (value == null)
            return "";

        return value.ToString();
    }

    private int GetCellInt(DataTable table, int row, int col)
    {
        object value = table.Rows[row][col];

        if (value == null)
            return 0;

        if (value is double d)
            return Mathf.RoundToInt((float)d);

        if (value is int i)
            return i;

        string str = value.ToString();

        if (int.TryParse(str, out int result))
            return result;

        return 0;
    }
}