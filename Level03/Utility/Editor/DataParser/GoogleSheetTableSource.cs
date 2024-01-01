using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Sirenix.OdinInspector;
// using Google.Apis.Auth.OAuth2;
using UnityEditor;
using UnityEngine;
using UniRx.InternalUtil;
using Utility;

namespace DataParser
{
    static class GoogleSheetExtensions
    {
        public static object GetCellValueOrNull(this CellData cell) => GetValue(cell.EffectiveValue);
        public static object GetValue(this ExtendedValue value)
        {
            var raw = value;
            if (raw == null) return null;
            
            var number = raw.NumberValue;
            if (number != null) return number;

            var boolean = raw.BoolValue;
            if (boolean != null) return boolean;

            var error = raw.ErrorValue;
            if (error != null) return error;

            return raw.StringValue;
        }
    }
    [CreateAssetMenu(fileName = "New GoogleSheetTableSource", menuName = "Settings/Data/Google Sheet Table Source", order = 0)]
    public class GoogleSheetTableSource : TableSource
    {
        private static readonly ClientSecrets Secrets = new ClientSecrets
        {
            ClientId = "287249352210-ap2js8f3vjrrofgfv9g850fbe9ptq5cl.apps.googleusercontent.com",
            ClientSecret = "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"
        };
        
        [field: SerializeField]
        [field: InfoBox("시트 페이지의 https://docs.google.com/spreadsheets/d/[여기에 위치한 ID] 를 작성해야 합니다.", InfoMessageType.Info)]
        public string SpreadsheetId { get; private set; } = "188lvondKo-3157eTe83q8VBZBzmHT5zOYwm7Tk3u_PE";

        private static SheetMetadata ParseMetadata(Sheet sheet, GridData grid)
        {
            if (sheet == null 
                || sheet.Properties.SheetType.ToUpper() != "GRID" 
                || sheet.Properties.GridProperties.ColumnCount <= 0
                || sheet.Properties.GridProperties.RowCount <= 0
            )
            {
                return null;
            }

            try
            {
                var sheetName = sheet.Properties.Title;
                var primary = grid.RowData[0].Values[0];
                var note = primary.Note;
                if (note == null)
                {
                    return null;
                }
                return ParseMetadata(sheetName, note);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{sheet.Properties.Title} 메타데이터 변환 실패: A1에 메모가 존재하지 않음");
                Debug.LogWarning(e);
                return null;
            }
        }

        [Sirenix.OdinInspector.Button]
        public void Test()
        {
            ReadFromGoogleSheet().Forget();
        }

        // cell으로부터 필드 이름 가져오기; 메모 있으면 메모 기반, 없으면 셀 자체 문자열 
        private static string GetFieldNameFromCellOrNull(CellData cell)
        {
            if(cell == null) return null;
            
            /*
            // 메모 있으면 맨 첫 줄만 반영
            var comment = cell.Note;
            if (comment != null)
            {
                return comment.Contains('\n') ? comment[..comment.IndexOf('\n')] : comment;
            }
            */

            // 메모 없으면 값 그대로 사용
            var raw = cell.GetCellValueOrNull();
            return raw as string;

        }
        // 특정 row의 시작 ~ 끝 읽어서 List화
        private static List<string> GetFieldNamesFromRow(RowData row, int startColumnNum)
        {
            var endColumnNum = row.Values.Count;
            var fieldNames = new List<string>(endColumnNum - startColumnNum);
            fieldNames.Add(FilePathKey); // 첫 번째 cell은 무조건 Path: 파일 이름 설정
            for (int i = startColumnNum + 1; i < endColumnNum; i++)
            {
                var cell = row.Values[i];
                var fieldName = GetFieldNameFromCellOrNull(cell);
                if (fieldName == null) break;
                fieldNames.Add(fieldName);
            }

            return fieldNames;
        }

        private async UniTask<SheetsService> GetSheetsService()
        {
            var pass = Secrets;
            var scopes = new[] { SheetsService.Scope.Spreadsheets };
            if(ShowDebugLog) DebugX.Log("API 계정 인증 중 ...");
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                pass, scopes,
                "Unity2022",
                CancellationToken.None
            );
            DebugX.Log($"계정 인증 성공: {credential.UserId}");
            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "NMProjectJenkins"
            });
            return service;
        }
        // https://velog.io/@eqeq109/%EA%B5%AC%EA%B8%80-%EC%8A%A4%ED%94%84%EB%A0%88%EB%93%9C-%EC%8B%9C%ED%8A%B8-API%EB%A5%BC-%EC%9D%B4%EC%9A%A9%ED%95%B4-%EC%9C%A0%EB%8B%88%ED%8B%B0-%EB%8D%B0%EC%9D%B4%ED%84%B0-%ED%85%8C%EC%9D%B4%EB%B8%94-%EA%B4%80%EB%A6%AC-%EB%A7%A4%EB%8B%88%EC%A0%80-%EB%A7%8C%EB%93%A4%EA%B8%B0-2-%EA%B5%AC%ED%98%84%ED%8E%B8
        private async UniTaskVoid ReadFromGoogleSheet()
        {
            var service = await GetSheetsService();
            if(ShowDebugLog) DebugX.Log("request ...");
            var getAllSpreadSheets = service.Spreadsheets.Get(SpreadsheetId);
            getAllSpreadSheets.IncludeGridData = true;
            var spreadsheet = await getAllSpreadSheets.ExecuteAsync();

            if(ShowDebugLog) DebugX.Log("printing results:");
            foreach (var sheet in spreadsheet.Sheets)
            {
                var title = sheet.Properties.Title;
                if (!FilterTableName(title))
                {
                    continue;
                }
                
                var gp = sheet.Properties.GridProperties;
                DebugX.Log($"[{title}] - {{type: {sheet.Properties.SheetType}, columnCount: {gp.ColumnCount}, rowCount: {gp.RowCount}}}");
                foreach (var gridData in sheet.Data)
                {
                    DebugX.Log($"- ETag: {gridData.ETag}, Start: ({gridData.StartColumn}, {gridData.StartRow})");
                    int rowIndex = 0;
                    int columnIndex = 0;
                    foreach (var row in gridData.RowData)
                    {
                        columnIndex = 0;
                        foreach (var cell in row.Values)
                        {
                            var value = cell.GetCellValueOrNull();
                            var note = cell.Note;
                            DebugX.Log($"[R{rowIndex}, C{columnIndex}]: {value}"+(note != null ? $"(NOTE: '{note.Replace("\n", "<\\n>")}')" : ""));
                            ++columnIndex;
                        }
                        ++rowIndex;
                    }
                }
            }
        }
        
        public override List<Table> Import()
        {
            if (string.IsNullOrWhiteSpace(SpreadsheetId))
            {
                Debug.LogError($"{this}: SpreadSheet ID가 비어있습니다.", this);
                return null;
            }
            var service = GetSheetsService().GetAwaiter().GetResult();
            if(ShowDebugLog) DebugX.Log("request ...");
            var getAllSpreadSheets = service.Spreadsheets.Get(SpreadsheetId);
            getAllSpreadSheets.IncludeGridData = true;
            var spreadsheet = getAllSpreadSheets.Execute();

            var tables = new List<Table>();
            foreach (var sheet in spreadsheet.Sheets)
            {
                
                var gp = sheet.Properties.GridProperties;
                var title = sheet.Properties.Title;                
                if (!FilterTableName(title))
                {
                    if (ShowDebugLog)
                    {
                        DebugX.Log($"Skipped {title} by filter");
                    }
                    continue;
                }
                if(ShowDebugLog) DebugX.Log($"[{title}] - {{type: {sheet.Properties.SheetType}, columnCount: {gp.ColumnCount}, rowCount: {gp.RowCount}}}");

                var gridIndex = 0;
                foreach (var gridData in sheet.Data)
                {
                    var meta = ParseMetadata(sheet, gridData);
                    if (meta == null)
                    {
                        DebugX.LogWarning($"failed to parse Metadata {title}");
                        continue;
                    }
                    if(ShowDebugLog) 
                        DebugX.Log($"[grid {gridIndex}] ETag: {gridData.ETag}, Start: ({gridData.StartColumn}, {gridData.StartRow}), Meta: {meta}");
                    
                    var table = CreateTable(sheet, gridData, meta);
                    if (table == null)
                    {
                        DebugX.LogWarning($"failed to parse Table {title}");
                        continue;
                    }
                    tables.Add(table);
                    DebugX.LogWarning($"parsed Table {title}");
                    break;
                }
            }

            return tables;
        }

        private Table CreateTable(Sheet sheet, GridData grid, SheetMetadata meta)
        {
            var table = new Table(meta);
            var offset = meta.Offset;
            switch (meta.Type)
            {
                // General 하게 읽는 경우, 첫 번째 행은 Property 이름, 첫 번째 열은 파일 이름.
                case TableType.General:
                {
                    var firstRowNum = offset.y;
                    var firstColumnNum = offset.x;
                    var propertyNameRow = grid.RowData[firstRowNum];
                    var propertyNames = GetFieldNamesFromRow(propertyNameRow, firstColumnNum);
                    
                    if(ShowDebugLog) DebugX.Log($"propertyNames: [{propertyNames.JoinToString()}]");

                    var rows = new List<RowData>();
                    for (int i = firstRowNum + 1; i < grid.RowData.Count; i++)
                    {
                        var row = grid.RowData[i];
                        rows.Add(row);
                    }

                    var columnIndex = 0;
                    foreach (var propertyName in propertyNames)
                    {
                        var list = new List<object>();
                        foreach (var row in rows)
                        {
                            if (firstColumnNum + columnIndex >= row.Values.Count)
                            {
                                continue;
                            }
                            var cell = row.Values[firstColumnNum + columnIndex];
                            var value = cell.GetCellValueOrNull();
                            list.Add(value);
                        }
                        table.Data.Add(propertyName, list);
                        ++columnIndex;
                    }
                    break;
                }
                case TableType.Singleton:
                {
                    var firstColumnNum = offset.x;
                    var firstRowNum = offset.y;
                    
                    // 각 row에 대해 ... (0번째 행에 대해서는 무시)
                    for (int rowIndex = Math.Max(firstRowNum, 1) ; rowIndex < grid.RowData.Count; rowIndex++)
                    {
                        var row = grid.RowData[rowIndex];
                        
                        // row.Values는 없는 칸은 없는 것으로 처리하기 때문에, 왼쪽 prefix만 있는 경우는 못 읽을 때도 있음
                        if (firstColumnNum + 1 >= row.Values.Count)
                        {
                            continue;
                        }
                        // property 이름 cell
                        // int index = 0;
                        // foreach (var cell in row.Values)
                        // {
                        // DebugX.Log($"(R{rowIndex}, C{index}) {cell.GetCellValueOrNull()}");
                        // ++index;
                        // }
                        var propertyCell = row.Values[firstColumnNum];
                        var propertyName = GetFieldNameFromCellOrNull(propertyCell);
                        // 실제 값 cell
                        var valueCell = row.Values[firstColumnNum + 1];
                        var value = valueCell.GetCellValueOrNull();
                        if (propertyName == null || value == null)
                        {
                            continue;
                        }
                        table.Data.Add(propertyName, new SingletonList<object>(value));
                    }
                    break;
                }
            }

            if(ShowDebugLog) foreach (var (key, list) in table.Data)
            {
                DebugX.Log($"- {key}: [{list.JoinToString(", ", o => o != null ? o.ToString() : "NULL")}]");
            }
            
            return table;
        }

        public override bool Export(List<Table> tables)
        {
            if (string.IsNullOrWhiteSpace(SpreadsheetId))
            {
                Debug.LogError($"{this}: SpreadSheet ID가 비어있습니다.", this);
                return false;
            }
            var service = GetSheetsService().GetAwaiter().GetResult();
            if(ShowDebugLog) DebugX.Log("request ...");
            
            var getAllSpreadSheets = service.Spreadsheets.Get(SpreadsheetId);
            getAllSpreadSheets.IncludeGridData = true;
            var spreadsheet = getAllSpreadSheets.Execute();
            var tableByPath = tables.ToDictionary(it => it.Path, it => it);
            var data = new List<ValueRange>(spreadsheet.Sheets.Count);
            foreach (var sheet in spreadsheet.Sheets)
            {
                foreach (var grid in sheet.Data)
                {
                    var meta = ParseMetadata(sheet, grid);
                    if (meta == null)
                    {
                        DebugX.LogWarning($"{this}: {SpreadsheetId}에서 시트 {sheet.Properties.Title}의 메타데이터가 유효하지 않음");
                        continue;
                    }

                    if (!tableByPath.TryGetValue(meta.Path, out var table))
                    {
                        DebugX.LogWarning($"{this}: {SpreadsheetId}에서 시트 {sheet.Properties.Title}에 {meta.Path}가 대응되는 Table이 없음");
                        continue;
                    }

                    tableByPath.Remove(meta.Path);

                    var valueRange = SaveTable(sheet, grid, meta, table);
                    if (valueRange == null)
                    {
                        DebugX.LogWarning($"{this}: {SpreadsheetId}에서 시트 {sheet.Properties.Title}에 {meta.Path} valueRange 추출 실패");
                    }
                    data.Add(valueRange);
                    break;
                }
            }

            var body = new BatchUpdateValuesRequest
            {
                Data = data,
                ValueInputOption = "USER_ENTERED"
            };
            var result = service.Spreadsheets.Values.BatchUpdate(body, SpreadsheetId).Execute();
            DebugX.Log($"{result.TotalUpdatedCells}개 셀 업데이트됨");
            return true;
        }

        private ValueRange SaveTable(Sheet sheet, GridData grid, SheetMetadata meta, Table table)
        {
            var rowCount = grid.RowData.Count;
            var columnCount = grid.RowData.Max(it => it.Values.Count);
            if(ShowDebugLog) DebugX.Log($"{meta.Path} saving ... - row: {rowCount}, column: {columnCount}");
            var result = new List<IList<object>>(rowCount);
            // 미리 grid 기반으로 ValueRange 채워놓기.
            for (int y = 0; y < rowCount; y++)
            {
                result.Add(new List<object>(columnCount));
                var row = grid.RowData[y];
                for (int x = 0; x < columnCount; x++)
                {
                    if (x >= row.Values.Count)
                    {
                        result[y].Add(null);
                        if(ShowDebugLog) DebugX.Log($"(R{y}, C{x}) - NULL");
                        continue;
                    }
                    var cell = row.Values[x];
                    result[y].Add(cell.UserEnteredValue.GetValue());
                    if(ShowDebugLog) DebugX.Log($"(R{y}, C{x}) - {cell.UserEnteredValue.GetValue()}");
                }
            }
            var valueRange = new ValueRange
            {
                MajorDimension = "ROWS",
                Range = $"{sheet.Properties.Title}!R1C1:R{rowCount}C{columnCount}",
                Values = result
            };
            
            var offset = meta.Offset;
            switch (meta.Type)
            {
                case TableType.General:
                {
                    var firstRowNum = offset.y;
                    var firstColumnNum = offset.x;
                    var propertyNameRow = grid.RowData[firstRowNum];
                    var propertyNames = GetFieldNamesFromRow(propertyNameRow, firstColumnNum);
                    
                    var rows = new List<RowData>();
                    for (int i = firstRowNum + 1; i < grid.RowData.Count; i++)
                    {
                        var row = grid.RowData[i];
                        rows.Add(row);
                    }

                    var columnIndex = 0;
                    foreach (var propertyName in propertyNames)
                    {
                        if (!table.Data.TryGetValue(propertyName, out var values))
                        {
                            DebugX.LogWarning($"{this}: {SpreadsheetId} - {sheet.Properties.Title}에서 {propertyName}에 대응되는 Table List가 없음");
                            continue;
                        }

                        for (int rowIndex = 0; rowIndex < values.Count; rowIndex++)
                        {
                            var row = rows[rowIndex];
                            if (firstColumnNum + columnCount >= row.Values.Count)
                            {
                                continue;
                            }
                            var cell = row.Values[firstColumnNum + columnIndex];
                            var newValue = values[rowIndex];
                            // 수식은 제외
                            if (cell?.UserEnteredValue?.FormulaValue?.StartsWith("=") == true)
                            {
                                if(ShowDebugLog) DebugX.Log($"skipped ({columnIndex}, {rowIndex}) - {newValue} skipped by formula ({cell.UserEnteredValue.GetValue()})");
                                continue;
                            }
                            result[firstRowNum + 1 + rowIndex][firstColumnNum + columnIndex] = newValue;
                            if(ShowDebugLog) DebugX.Log($"{propertyName}({columnIndex}, {rowIndex}) = ({cell?.EffectiveValue.GetValue()}) -> ({newValue})");
                        }
                        ++columnIndex;
                    }
                    break;
                }
                case TableType.Singleton:
                {
                    var firstRowNum = offset.y;
                    var firstColumnNum = offset.x;

                    // 각 row에 대해 ... (0번째 행에 대해서는 무시)
                    for (int rowIndex = Math.Max(firstRowNum, 1) ; rowIndex < rowCount; rowIndex++)
                    {
                        var row = grid.RowData[rowIndex];
                        if (firstColumnNum + 1 >= row.Values.Count)
                        {
                            continue;
                        }
                        // property 이름 cell
                        var propertyCell = row.Values[firstColumnNum];
                        var propertyName = GetFieldNameFromCellOrNull(propertyCell);
                        // 실제 값 cell
                        var cellColumnIndex = firstColumnNum + 1;
                        var cell = row.Values[cellColumnIndex];
                        var oldValue = cell.GetCellValueOrNull();
                        if (propertyName == null)
                        {
                            continue;
                        }

                        if (!table.Data.TryGetValue(propertyName, out var values))
                        {
                            DebugX.LogWarning($"{this}: {SpreadsheetId} - {sheet.Properties.Title}에서 {propertyName}에 대응되는 Table List가 없음");
                            continue;
                        }

                        var newValue = values[0];
                        if (newValue == null)
                        {
                            continue;
                        }
                        // 수식은 제외
                        if (cell?.UserEnteredValue?.FormulaValue?.StartsWith("=") == true)
                        {
                            if(ShowDebugLog) DebugX.Log($"skipped ({cellColumnIndex}, {rowIndex}) - {newValue} skipped by formula ({cell.UserEnteredValue.GetValue()})");
                            continue;
                        }
                        result[rowIndex][cellColumnIndex] = newValue;
                        if(ShowDebugLog) DebugX.Log($"{propertyName}({cellColumnIndex}, {rowIndex}) = ({cell?.EffectiveValue.GetValue()}) -> ({newValue})");
                    }
                    break;
                }
            }

            return valueRange;
        }


        public override string ToString()
        {
            return $"{name}(GoogleSheet)";
        }
    }
}