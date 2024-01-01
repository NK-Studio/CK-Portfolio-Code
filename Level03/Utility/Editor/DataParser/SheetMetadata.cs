using System;
using System.IO;
using NPOI.SS.UserModel;
using UnityEngine;

namespace DataParser
{
    public class SheetMetadata
    {
        public string SheetName;
        public string Path;
        public Vector2Int Offset = new(0, 0);
        public TableType Type = TableType.General;
        
        public SheetMetadata(string sheetName)
        {
            SheetName = sheetName;
        }
        
        public bool IsValid => !string.IsNullOrWhiteSpace(Path) && Offset.x >= 0 && Offset.y >= 0;
        public override string ToString()
        {
            return $"{{offset: {Offset}, path: '{Path}', type: {Type.ToString()}}}";
        }
        
        /// <summary>
        /// raw한 key: value 형태의 값으로부터 데이터 파싱
        /// </summary>
        /// <param name="key"></param>
        /// <param name="rawValue"></param>
        /// <param name="errorOrNull"></param>
        public void ParseAndInsertValue(string key, string rawValue, out string errorOrNull)
        {
            switch (key.ToLower())
            {
                case "path":
                {
                    if (!Directory.Exists(rawValue) && !File.Exists(rawValue))
                    {
                        errorOrNull = $"{rawValue} is invalid path";
                        return;
                    }
                    Path = rawValue;
                    if (rawValue.EndsWith(".asset"))
                    {
                        Type = TableType.Singleton;
                    }
                    break;      
                }
                case "type":
                {
                    if (!Enum.TryParse(typeof(TableType), rawValue, out var type))
                    {
                        errorOrNull = $"{rawValue} is invalid ParseType";
                        return;
                    }
                    break;
                }
                case "offset":
                {
                    var split = rawValue.Split(',');
                    if (split.Length < 2)
                    {
                        errorOrNull = $"cannot parse as vector {rawValue}";
                        return;
                    }

                    if (!int.TryParse(split[0].Trim(), out int x) 
                        || !int.TryParse(split[1].Trim(), out int y))
                    {
                        errorOrNull = $"{rawValue} is not an int vector";
                        return;
                    }

                    if (x < 0 || y < 0)
                    {
                        errorOrNull = $"{rawValue} must be positive";
                        return;
                    }
                    Offset.x = x;
                    Offset.y = y;
                    break;
                }
            }

            errorOrNull = null;
        }

        
    }
}