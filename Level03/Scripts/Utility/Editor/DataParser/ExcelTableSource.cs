using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using UniRx.InternalUtil;
using Utility;

namespace DataParser
{
    [CreateAssetMenu(fileName = "New ExcelTableSource", menuName = "Settings/Data/Excel Table Source", order = 0)]
    public class ExcelTableSource : TableSource
    {
        [field: SerializeField]
        public DefaultAsset Target { get; private set; }

        /// <summary>
        /// ISheet로부터 메타데이터 파싱
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns></returns>
        private static SheetMetadata ParseMetadata(ISheet sheet)
        {
            var comment = sheet.GetCellComment(0, 0);
            if (comment == null)
            {
                Debug.LogWarning($"{sheet.SheetName} 메타데이터 변환 실패: A1에 메모가 존재하지 않음");
                return null;
            }

            var richText = comment.String;
            if (richText == null)
            {
                Debug.LogWarning($"{sheet.SheetName} 메타데이터 변환 실패: string is null");
                return null;
            }

            var raw = richText.String;
            if (string.IsNullOrWhiteSpace(raw))
            {
                Debug.LogWarning($"{sheet.SheetName} 메타데이터 변환 실패: 메모가 비어 있음.");
                return null;
            }

            return ParseMetadata(sheet.SheetName, raw);
        }

        // 지정된 path에서 workbook 얻어오기
        private static IWorkbook ReadBook(string excelPath)
        {
            using var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (Path.GetExtension(excelPath) == ".xls") 
                return new HSSFWorkbook(stream);
            else 
                return new XSSFWorkbook(stream);
        }

        private static void WriteBook(string excelPath, IWorkbook book)
        {
            using var stream = File.Open(excelPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            stream.Position = 0;
            book.Write(stream);
            stream.SetLength(stream.Position);
        }

        // cell으로부터 필드 이름 가져오기; 메모 있으면 메모 기반, 없으면 셀 자체 문자열 
        private static string GetFieldNameFromCellOrNull(ICell cell)
        {
            if(cell == null || cell.CellType == CellType.Blank) return null;
            
            /*
            // 메모 있으면 맨 첫 줄만 반영
            var comment = cell.CellComment?.String?.String;
            if (comment != null)
            {
                return comment.Contains('\n') ? comment[..comment.IndexOf('\n')] : comment;
            }
            */

            // 메모 없으면 값 그대로 사용
            return cell.StringCellValue;

        }
        // 특정 row의 시작 ~ 끝 읽어서 List화
        private static List<string> GetFieldNamesFromRow(IRow row, int startColumnNum)
        {
            var endColumnNum = row.LastCellNum;
            var fieldNames = new List<string>(endColumnNum - startColumnNum);
            fieldNames.Add(FilePathKey); // 첫 번째 cell은 무조건 Path: 파일 이름 설정
            for (int i = startColumnNum + 1; i < endColumnNum; i++)
            {
                var cell = row.GetCell(i);
                var fieldName = GetFieldNameFromCellOrNull(cell);
                if (fieldName == null) break;
                fieldNames.Add(fieldName);
            }

            return fieldNames;
        }

        /// <summary>
        /// 특정 cell로부터 값 얻어오기
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="isFormulaEvaluate"></param>
        /// <returns></returns>
        private static object GetCellValueOrNull(ICell cell, bool isFormulaEvaluate = false)
        {
            if (cell == null) return null;
            var type = isFormulaEvaluate ? cell.CachedFormulaResultType : cell.CellType;

            switch(type)
            {
                case CellType.String:
                    // if (fieldInfo.FieldType.IsEnum) return Enum.Parse(fieldInfo.FieldType, cell.StringCellValue);
                    // else return cell.StringCellValue;
                    return cell.StringCellValue;
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Numeric:
                    // return Convert.ChangeType(cell.NumericCellValue, fieldInfo.FieldType);
                    return cell.NumericCellValue;
                case CellType.Formula:
                    if(isFormulaEvaluate) return null;
                    return GetCellValueOrNull(cell, true); 
                default:
                    // if(fieldInfo.FieldType.IsValueType)
                    // {
                        // return Activator.CreateInstance(fieldInfo.FieldType);
                    // }
                    return null;
            }
        }

        /// <summary>
        /// 특정 cell에 값 설정
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool SetCellValue(ICell cell, object value)
        {
            if (cell == null) return false;
            var type = cell.CellType;

            // 이미 해당 자리에 어떤 식이 있는 경우 (식으로부터 도출된 값인 경우)
            // -> 값 설정하지 않음
            if (type == CellType.Formula)
            {
                return true;
            }

            switch (value)
            {
                case string str:
                    cell.SetCellValue(str);
                    break;
                case bool b:
                    cell.SetCellValue(b);
                    break;
                case double d:
                    cell.SetCellValue(d);
                    break;
                case float f:
                    cell.SetCellValue(f);
                    break;
                case int i:
                    cell.SetCellValue(i);
                    break;
                case short s:
                    cell.SetCellValue(s);
                    break;
                case byte b:
                    cell.SetCellValue(b);
                    break;
                case uint i:
                    cell.SetCellValue(i);
                    break;
                case ushort s:
                    cell.SetCellValue(s);
                    break;
                case sbyte b:
                    cell.SetCellValue(b);
                    break;
                default:
                    DebugX.LogWarning($"{value}({value.GetType()})은 엑셀 저장 시 지원되지 않는 타입");
                    return false;
            }

            return true;
        }
        
        public override List<Table> Import()
        {
            if (!Target)
            {
                Debug.LogError($"{this}: Target이 비어있습니다.", this);
                return null;
            }

            var tables = new List<Table>();
            var book = ReadBook(AssetDatabase.GetAssetPath(Target));
            var sheetCount = book.NumberOfSheets;
            for (int i = 0; i < sheetCount; i++)
            {
                var sheet = book.GetSheetAt(i);
                var title = sheet.SheetName;
                if (!FilterTableName(title))
                {
                    if (ShowDebugLog)
                    {
                        DebugX.Log($"Skipped {title} by filter");
                    }
                    continue;
                }
                // 데이터테이블 공통 - (0, 0)에는 시트의 메타데이터 포함, :로 구분.
                // 일반 데이터 테이블의 경우 첫 번째 열 (0, x)은 무조건 SO의 path를 지정해야 해서 이름 무의미.
                // 싱글톤 데이터 테이블의 경우 첫 번째 열의 첫 번째 행 (0, 0)은 사용하지 않는 것으로 합의
                var meta = ParseMetadata(sheet);
                if (meta == null)
                {
                    DebugX.LogWarning($"{this}: {Target}에서 시트 {sheet.SheetName}의 메타데이터가 유효하지 않음");
                    continue;
                }

                var table = CreateTable(sheet, meta);
                if (table == null)
                {
                    continue;
                }
                tables.Add(table);
            }

            return tables;
        }

        private Table CreateTable(ISheet sheet, SheetMetadata meta)
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
                    var propertyNameRow = sheet.GetRow(firstRowNum);
                    var propertyNames = GetFieldNamesFromRow(propertyNameRow, firstColumnNum);
                    if(ShowDebugLog) DebugX.Log($"propertyNames: [{propertyNames.JoinToString()}]");
                    
                    // 존재하는 row 저장 (header 제외)
                    var rows = new List<IRow>();
                    for (int rowIndex = firstRowNum + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        var row = sheet.GetRow(rowIndex);
                        rows.Add(row);
                    }

                    // 각 propertyName에 대해 값 저장
                    var columnIndex = 0;
                    foreach (var propertyName in propertyNames)
                    {
                        var list = new List<object>();
                        foreach (var row in rows)
                        {
                            var cell = row.GetCell(firstColumnNum + columnIndex);
                            var value = GetCellValueOrNull(cell);
                            list.Add(value);
                        }
                        table.Data.Add(propertyName, list);
                        ++columnIndex;
                    }
                    break;
                }
                // Singleton 하게 읽는 경우, 첫 번째 열은 Property, 두 번째 열은 Value.
                // 0번째 행은 무조건 무시됨
                case TableType.Singleton:
                {
                    var firstColumnNum = offset.x;
                    var firstRowNum = offset.y;

                    // 각 row에 대해 ... (0번째 행에 대해서는 무시)
                    for (int rowIndex = Math.Max(firstRowNum, 1) ; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        var row = sheet.GetRow(rowIndex);
                        // property 이름 cell
                        var propertyCell = row.GetCell(firstColumnNum);
                        var propertyName = GetFieldNameFromCellOrNull(propertyCell);
                        // 실제 값 cell
                        var valueCell = row.GetCell(firstColumnNum + 1);
                        var value = GetCellValueOrNull(valueCell);
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
            if (!Target)
            {
                Debug.LogError($"{this}: Target이 비어있습니다.", this);
                return false;
            }

            var excelPath = AssetDatabase.GetAssetPath(Target);
            var book = ReadBook(excelPath);
            // 상식적으로, 같은 엑셀 파일 내에 같은 Path를 가지는 것으로 판단하지 않음
            var tableByPath = tables.ToDictionary(it => it.Path, it => it);
            var sheetCount = book.NumberOfSheets;
            for (int i = 0; i < Math.Min(sheetCount, tables.Count); i++)
            {
                var sheet = book.GetSheetAt(i);
                var meta = ParseMetadata(sheet);
                if (meta == null)
                {
                    DebugX.LogWarning($"{this}: {Target}에서 시트 {sheet.SheetName}의 메타데이터가 유효하지 않음");
                    continue;
                }

                if (!tableByPath.TryGetValue(meta.Path, out var table))
                {
                    DebugX.LogWarning($"{this}: {Target}에서 시트 {sheet.SheetName}에 {meta.Path}가 대응되는 Table이 없음");
                    continue;
                }

                tableByPath.Remove(meta.Path);
                
                SaveTable(sheet, meta, table);
            }

            foreach (var (path, table) in tableByPath)
            {
                DebugX.LogWarning($"{this}: {Target}에서 {path}에 대응되는 Sheet가 없음");
            }

            try
            {
                WriteBook(excelPath, book);
            }
            catch (Exception e)
            {
                DebugX.LogError($"{name} 저장에 실패했습니다.");
                DebugX.LogError(e);
                return false;
            }
            return tableByPath.Count <= 0;
        }

        private void SaveTable(ISheet sheet, SheetMetadata meta, Table table)
        {
            var offset = meta.Offset;
            switch (meta.Type)
            {
                case TableType.General:
                {
                    var firstRowNum = offset.y;
                    var firstColumnNum = offset.x;
                    var propertyNameRow = sheet.GetRow(firstRowNum);
                    var propertyNames = GetFieldNamesFromRow(propertyNameRow, firstColumnNum);

                    // 존재하는 row 캐시 (header 제외)
                    var rows = new List<IRow>();
                    for (int rowIndex = firstRowNum + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        var row = sheet.GetRow(rowIndex);
                        rows.Add(row);
                    }
                    for (int x = 0; x < propertyNames.Count; x++)
                    {
                        var propertyName = propertyNames[x];
                        var columnIndex = x + firstColumnNum;
                        if (!table.Data.TryGetValue(propertyName, out var values))
                        {
                            DebugX.LogWarning($"{this}: {Target} - {sheet.SheetName}에서 {propertyName}에 대응되는 Table List가 없음");
                            continue;
                        }
                        for (int y = 0; y < values.Count; y++)
                        {
                            var row = rows[y];
                            var cell = row.GetCell(columnIndex);
                            
                            var newValue = values[y];
                            if (cell != null && newValue != null && !SetCellValue(cell, newValue))
                            {
                                DebugX.LogWarning($"{this}: {Target}에서 ({x}, {y}) 값 저장 실패");
                                continue;
                            }
                            /* // 저장 기록 변화를 나타내려 했던 실패한 코드들
                            if (newValue != null)
                            {
                                var newValueType = newValue.GetType();
                                var oldValue = Convert.ChangeType(GetCellValueOrNull(cell), newValueType);
                                var oldValueType = oldValue.GetType();
                                var newValueConverted = Convert.ChangeType(newValue, newValueType);
                                
                                if (Equals(oldValue, newValueConverted))
                                {
                                    DebugX.Log($"{this}: {Target.name} Updated - {propertyName}[{y}]: {oldValue}({oldValueType}) -> {newValueConverted}({newValueType})");
                                }
                            }
                            */
                        }
                    }
                    break;
                }
                case TableType.Singleton:
                {
                    var firstRowNum = offset.y;
                    var firstColumnNum = offset.x;
                    
                    // 각 row에 대해 ... (0번째 행에 대해서는 무시)
                    for (int rowIndex = Math.Max(firstRowNum, 1) ; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        var row = sheet.GetRow(rowIndex);
                        // property 이름 cell
                        var propertyCell = row.GetCell(firstColumnNum);
                        var propertyName = GetFieldNameFromCellOrNull(propertyCell);
                        // 값 cell
                        var valueCell = row.GetCell(firstColumnNum + 1);
                        if (!table.Data.TryGetValue(propertyName, out var values))
                        {
                            DebugX.LogWarning($"{this}: {Target} - {sheet.SheetName}에서 {propertyName}에 대응되는 Table List가 없음");
                            continue;
                        }

                        var newValue = values[0];
                        if (newValue == null)
                        {
                            continue;
                        }
                        if (!SetCellValue(valueCell, newValue))
                        {
                            DebugX.LogWarning($"{this}: {Target}에서 ({propertyName}: {newValue}) 값 저장 실패");
                            continue;
                        }
                        /* // 저장 기록 변화를 나타내려 했던 실패한 코드들
                        var newValueType = newValue.GetType();
                        var oldValue = Convert.ChangeType(GetCellValueOrNull(valueCell), newValueType);

                        if (oldValue != Convert.ChangeType(newValue, newValueType))
                        {
                            DebugX.Log($"{this}: {Target.name} Updated - {propertyName}: {oldValue} -> {newValue}");
                        }
                        */
                    }
                    break;
                }
            }
        }

        public override string ToString()
        {
            return $"{name}(Excel)";
        }
    }
}