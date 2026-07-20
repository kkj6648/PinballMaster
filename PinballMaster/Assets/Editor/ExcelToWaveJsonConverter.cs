using ExcelDataReader;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ExcelToWaveJsonConverter : EditorWindow
{
    private const int GridWidth = 9;
    private const int GridStartColumn = 1;
    private const int LineColumn = 10;

    private const int InitialSpawnRows = 6;
    private const int UpdateLineCount = 7;


    [MenuItem("Tools/Excel To Wave JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<ExcelToWaveJsonConverter>(
            "Wave Excel Converter"
        );
    }


    private void OnGUI()
    {
        if (GUILayout.Button("Select Excel And Convert"))
        {
            string path = EditorUtility.OpenFilePanel(
                "Select Wave Excel File",
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
        using (FileStream stream = File.Open(
                   filePath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.ReadWrite))
        {
            using (IExcelDataReader reader =
                   ExcelReaderFactory.CreateReader(stream))
            {
                DataSet result = reader.AsDataSet();

                if (result == null ||
                    result.Tables == null ||
                    result.Tables.Count <= 0)
                {
                    Debug.LogError(
                        "Excel file does not contain a worksheet."
                    );

                    return;
                }

                DataTable table = result.Tables[0];

                WaveJsonDatabase database =
                    new WaveJsonDatabase
                    {
                        gridWidth = GridWidth,
                        initialSpawnRows = InitialSpawnRows,
                        waves = new List<WaveJsonData>()
                    };

                int waveId = 1;

                for (int row = 0;
                     row < table.Rows.Count;
                     row++)
                {
                    string waveName =
                        FindWaveNameInRow(table, row);

                    if (string.IsNullOrEmpty(waveName))
                        continue;

                    WaveJsonData wave = ParseWave(
                        table,
                        row,
                        waveId,
                        waveName
                    );

                    database.waves.Add(wave);

                    waveId++;
                }

                string json =
                    JsonConvert.SerializeObject(
                        database,
                        Formatting.Indented
                    );

                string folderPath =
                    Application.dataPath + "/WaveData";

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string savePath =
                    folderPath + "/WaveData.json";

                File.WriteAllText(savePath, json);

                AssetDatabase.Refresh();

                Debug.Log(
                    $"Wave JSON Convert Success: {savePath}"
                );
            }
        }
    }


    private string FindWaveNameInRow(
        DataTable table,
        int row)
    {
        for (int col = 0;
             col < table.Columns.Count;
             col++)
        {
            string value =
                GetCellString(table, row, col);

            if (string.IsNullOrWhiteSpace(value))
                continue;

            string normalizedValue =
                value.Trim().ToLowerInvariant();

            if (normalizedValue.StartsWith("wave"))
            {
                return normalizedValue;
            }
        }

        return null;
    }


    private WaveJsonData ParseWave(
        DataTable table,
        int waveLabelRow,
        int waveId,
        string waveName)
    {

        WaveJsonData wave =
            new WaveJsonData
            {
                waveId = waveId,
                waveName = waveName,

                initialSpawns =
                    new List<EnemySpawnJsonData>(),

                updateLines =
                    new List<WaveUpdateLineJsonData>()
            };

        int initialStartRow =
            waveLabelRow + 1;

        ParseInitialFormation(
            table,
            initialStartRow,
            wave
        );

        int updateStartRow =
            initialStartRow + InitialSpawnRows;

        ParseUpdateLines(
            table,
            updateStartRow,
            wave
        );

        wave.updateLines.Sort(
            (a, b) => a.line.CompareTo(b.line)
        );

        Debug.Log(
            $"{wave.waveName} parsed | " +
            $"Init: {wave.initialSpawns.Count}, " +
            $"Update Lines: {wave.updateLines.Count}, " +
            $"Total Enemies: {GetTotalEnemyCount(wave)}"
        );

        return wave;
    }


    #region Initial Formation

    private void ParseInitialFormation(
        DataTable table,
        int initialStartRow,
        WaveJsonData wave)
    {

        bool[,] occupied =
            new bool[GridWidth, InitialSpawnRows];

        for (int gridY = 0;
             gridY < InitialSpawnRows;
             gridY++)
        {
            int excelRow =
                initialStartRow + gridY;

            if (excelRow >= table.Rows.Count)
                break;

            ParseFormationRow(
                table,
                excelRow,
                gridY,
                occupied,
                wave.initialSpawns,
                wave.waveName,
                "Init"
            );
        }
    }

    #endregion Initial Formation


    #region Update Lines

    private void ParseUpdateLines(
    DataTable table,
    int updateStartRow,
    WaveJsonData wave)
    {
        Dictionary<int, int> lineToExcelRow =
            new Dictionary<int, int>();

        Dictionary<int, WaveUpdateLineJsonData> lineLookup =
            new Dictionary<int, WaveUpdateLineJsonData>();


        for (int rowOffset = 0;
             rowOffset < UpdateLineCount;
             rowOffset++)
        {
            int excelRow =
                updateStartRow + rowOffset;

            if (excelRow >= table.Rows.Count)
                break;

            int line =
                GetCellInt(
                    table,
                    excelRow,
                    LineColumn
                );

            if (line < 1 ||
                line > UpdateLineCount)
            {
                continue;
            }

            lineToExcelRow[line] = excelRow;

            WaveUpdateLineJsonData updateLine =
                new WaveUpdateLineJsonData
                {
                    line = line,
                    spawns =
                        new List<EnemySpawnJsonData>()
                };

            lineLookup[line] = updateLine;
            wave.updateLines.Add(updateLine);
        }


        bool[,] occupied =
            new bool[GridWidth, UpdateLineCount];


        for (int line = UpdateLineCount;
             line >= 1;
             line--)
        {
            if (!lineToExcelRow.TryGetValue(
                    line,
                    out int excelRow))
            {
                continue;
            }

            if (!lineLookup.TryGetValue(
                    line,
                    out WaveUpdateLineJsonData updateLine))
            {
                continue;
            }

            int anchorY =
                line - 1;

            for (int x = 0;
                 x < GridWidth;
                 x++)
            {
                string code =
                    GetCellString(
                        table,
                        excelRow,
                        GridStartColumn + x
                    );

                if (string.IsNullOrWhiteSpace(code))
                    continue;

                code =
                    code.Trim().ToLowerInvariant();

                if (!IsEnemyCode(code))
                    continue;

                Vector2Int size =
                    GetEnemySize(code);

                if (!CanPlaceUpdateFootprint(
                        x,
                        anchorY,
                        size.x,
                        size.y,
                        occupied))
                {
                    Debug.LogWarning(
                        $"{wave.waveName} / Update Line {line}: " +
                        $"Cannot place {code} at x:{x}, " +
                        $"size:{size.x}x{size.y}. " +
                        $"The footprint overlaps or exceeds the grid."
                    );

                    continue;
                }

   
                updateLine.spawns.Add(
                    new EnemySpawnJsonData
                    {
                        enemyCode = code,
                        sizeType =
                            GetSizeTypeName(code),

                        gridX = x,
                        gridY = 0
                    }
                );

                MarkUpdateFootprint(
                    x,
                    anchorY,
                    size.x,
                    size.y,
                    occupied
                );
            }
        }


        wave.updateLines.Sort(
            (a, b) => a.line.CompareTo(b.line)
        );
    }



    private bool CanPlaceUpdateFootprint(
    int startX,
    int backY,
    int width,
    int height,
    bool[,] occupied)
    {
        int gridW =
            occupied.GetLength(0);

        int gridH =
            occupied.GetLength(1);

        int frontY =
            backY - height + 1;

        if (startX < 0 || frontY < 0)
            return false;

        if (startX + width > gridW)
            return false;

        if (backY >= gridH)
            return false;

        for (int x = startX;
             x < startX + width;
             x++)
        {
            for (int y = frontY;
                 y <= backY;
                 y++)
            {
                if (occupied[x, y])
                    return false;
            }
        }

        return true;
    }

    private void MarkUpdateFootprint(
    int startX,
    int backY,
    int width,
    int height,
    bool[,] occupied)
    {
        int frontY =
            backY - height + 1;

        for (int x = startX;
             x < startX + width;
             x++)
        {
            for (int y = frontY;
                 y <= backY;
                 y++)
            {
                occupied[x, y] = true;
            }
        }
    }

    private void ParseUpdateRow(
        DataTable table,
        int excelRow,
        int scheduleY,
        bool[,] occupied,
        List<EnemySpawnJsonData> result,
        string waveName,
        int line)
    {
        for (int gridX = 0;
             gridX < GridWidth;
             gridX++)
        {

            if (occupied[gridX, scheduleY])
                continue;

            int excelColumn =
                GridStartColumn + gridX;

            string code =
                GetCellString(
                    table,
                    excelRow,
                    excelColumn
                );

            if (string.IsNullOrWhiteSpace(code))
                continue;

            code =
                code.Trim().ToLowerInvariant();

            if (!IsEnemyCode(code))
                continue;

            Vector2Int size =
                GetEnemySize(code);

            if (!CanPlace(
                    gridX,
                    scheduleY,
                    size.x,
                    size.y,
                    occupied))
            {
                Debug.LogWarning(
                    $"{waveName} / Update Line {line}: " +
                    $"Cannot place {code} at " +
                    $"x:{gridX}, scheduleY:{scheduleY}, " +
                    $"size:{size.x}x{size.y}"
                );

                continue;
            }

 
            EnemySpawnJsonData spawn =
                new EnemySpawnJsonData
                {
                    enemyCode = code,
                    sizeType =
                        GetSizeTypeName(code),

                    gridX = gridX,
                    gridY = 0
                };

            result.Add(spawn);

            MarkOccupied(
                gridX,
                scheduleY,
                size.x,
                size.y,
                occupied
            );
        }
    }

    #endregion Update Lines


    #region Shared Parsing

    private void ParseFormationRow(
        DataTable table,
        int excelRow,
        int gridY,
        bool[,] occupied,
        List<EnemySpawnJsonData> result,
        string waveName,
        string sectionName)
    {
        for (int gridX = 0;
             gridX < GridWidth;
             gridX++)
        {
            if (occupied[gridX, gridY])
                continue;

            int excelColumn =
                GridStartColumn + gridX;

            string code =
                GetCellString(
                    table,
                    excelRow,
                    excelColumn
                );

            if (string.IsNullOrWhiteSpace(code))
                continue;

            code =
                code.Trim().ToLowerInvariant();

            if (!IsEnemyCode(code))
                continue;

            Vector2Int size =
                GetEnemySize(code);

            if (!CanPlace(
                    gridX,
                    gridY,
                    size.x,
                    size.y,
                    occupied))
            {
                Debug.LogWarning(
                    $"{waveName} / {sectionName}: " +
                    $"Cannot place {code} at " +
                    $"x:{gridX}, y:{gridY}, " +
                    $"size:{size.x}x{size.y}"
                );

                continue;
            }

            EnemySpawnJsonData spawn =
                new EnemySpawnJsonData
                {
                    enemyCode = code,
                    sizeType =
                        GetSizeTypeName(code),

                    gridX = gridX,
                    gridY = gridY
                };

            result.Add(spawn);

            MarkOccupied(
                gridX,
                gridY,
                size.x,
                size.y,
                occupied
            );
        }
    }


    private string GetCellString(
        DataTable table,
        int row,
        int col)
    {
        if (row < 0 ||
            row >= table.Rows.Count)
        {
            return "";
        }

        if (col < 0 ||
            col >= table.Columns.Count)
        {
            return "";
        }

        object value =
            table.Rows[row][col];

        return value?.ToString() ?? "";
    }


    private int GetCellInt(
        DataTable table,
        int row,
        int col)
    {
        string value =
            GetCellString(table, row, col);

        if (string.IsNullOrWhiteSpace(value))
            return -1;

        value = value.Trim();

        if (int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int intResult))
        {
            return intResult;
        }

        if (double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double doubleResult))
        {
            return Mathf.RoundToInt(
                (float)doubleResult
            );
        }

        return -1;
    }

    #endregion Shared Parsing


    #region Enemy Information

    private bool IsEnemyCode(string code)
    {
        return code == "b" ||
               code == "p" ||
               code == "o" ||
               code == "g" ||
               code == "r";
    }


    private Vector2Int GetEnemySize(string code)
    {
        switch (code)
        {
            case "o":
                return new Vector2Int(2, 1);

            case "g":
                return new Vector2Int(1, 2);

            case "r":
                return new Vector2Int(2, 2);

            case "b":
            case "p":
            default:
                return new Vector2Int(1, 1);
        }
    }


    private string GetSizeTypeName(string code)
    {
        switch (code)
        {
            case "o":
                return "Size_2x1";

            case "g":
                return "Size_1x2";

            case "r":
                return "Size_2x2";

            case "b":
            case "p":
            default:
                return "Size_1x1";
        }
    }

    #endregion Enemy Information


    #region Occupied Grid

    private bool CanPlace(
        int x,
        int y,
        int width,
        int height,
        bool[,] occupied)
    {
        int gridW =
            occupied.GetLength(0);

        int gridH =
            occupied.GetLength(1);

        if (x < 0 || y < 0)
            return false;

        if (x + width > gridW)
            return false;

        if (y + height > gridH)
            return false;

        for (int ix = x;
             ix < x + width;
             ix++)
        {
            for (int iy = y;
                 iy < y + height;
                 iy++)
            {
                if (occupied[ix, iy])
                    return false;
            }
        }

        return true;
    }


    private void MarkOccupied(
        int x,
        int y,
        int width,
        int height,
        bool[,] occupied)
    {
        for (int ix = x;
             ix < x + width;
             ix++)
        {
            for (int iy = y;
                 iy < y + height;
                 iy++)
            {
                occupied[ix, iy] = true;
            }
        }
    }

    #endregion Occupied Grid


    private int GetTotalEnemyCount(
        WaveJsonData wave)
    {
        if (wave == null)
            return 0;

        int totalCount = 0;

        if (wave.initialSpawns != null)
        {
            totalCount +=
                wave.initialSpawns.Count;
        }

        if (wave.updateLines != null)
        {
            for (int i = 0;
                 i < wave.updateLines.Count;
                 i++)
            {
                WaveUpdateLineJsonData line =
                    wave.updateLines[i];

                if (line?.spawns == null)
                    continue;

                totalCount +=
                    line.spawns.Count;
            }
        }

        return totalCount;
    }
}