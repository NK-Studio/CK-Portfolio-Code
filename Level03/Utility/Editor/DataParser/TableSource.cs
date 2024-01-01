using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Utility;

namespace DataParser
{
    static class ReflectionExtensions
    {
        public static Type GetActualType(this MemberInfo info)
        {
            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    return (info as FieldInfo)!.FieldType;
                case MemberTypes.Property:
                    return (info as PropertyInfo)!.PropertyType;
            }

            return null;
        }
        public static object GetActualValue(this MemberInfo info, object obj)
        {
            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    return (info as FieldInfo)!.GetValue(obj);
                case MemberTypes.Property:
                    return (info as PropertyInfo)!.GetValue(obj);
            }

            return null;
        }
        public static void SetActualValue(this MemberInfo info, object obj, object value)
        {
            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    (info as FieldInfo)!.SetValue(obj, value);
                    return;
                case MemberTypes.Property:
                    (info as PropertyInfo)!.SetValue(obj, value);
                    return;
            }
        }

        public static object GetValue(this Dictionary<string, MemberInfo> dict, string path, object target)
        {
            // '.' 을 통해 특정 멤버를 참조해야 하는 경우 dict에 보관된 멤버 정보 기준으로 참조
            var split = path.Split(".");
            var value = target;
            string currentPath = split[0];
            value = dict[currentPath].GetActualValue(value); // 최초 path에 대한 값
            // DebugX.Log($"GetValue({target} - {path})[0] {currentPath}: {value}");
            for (int i = 1; i < split.Length; i++)
            {
                currentPath += $".{split[i]}";
                value = dict[currentPath].GetActualValue(value);
                // DebugX.Log($"GetValue({target} - {path})[{i}] {currentPath}: {value}");
            }

            return value;
            // DebugX.Log($"[{type}] <color=yellow>{GetMemberType(member)}</color> {memberName.Replace(".", "<color=lime>::</color>")} <color=cyan>{value}</color>");
        }

        public static void SetValue(this Dictionary<string, MemberInfo> dict, string path, object target, object value)
        {
            // '.' 을 통해 특정 멤버를 참조해야 하는 경우 dict에 보관된 멤버 정보 기준으로 참조
            var split = path.Split(".");
            var targetObject = target;
            
            string currentPath = split[0];
            // 멤버 내부 계층구조가 없는 경우, root target 기준으로 즉시 설정
            if (split.Length <= 1)
            {
                var member = dict[currentPath];
                member.SetActualValue(targetObject, value);
                return;
            }
            // 계층구조가 있는 경우, 최하단 직전까지 member 탐색
            targetObject = dict[currentPath].GetActualValue(targetObject); // 최초 path에 대한 값
            for (int i = 1; i < split.Length - 1; i++)
            {
                currentPath += $".{split[i]}";
                targetObject = dict[currentPath].GetActualValue(targetObject);
            }
            currentPath += $".{split[^1]}";
            dict[currentPath].SetActualValue(targetObject, value);
            // DebugX.Log($"{currentPath}(real: {dict[currentPath].Name}) set to {value} on {targetObject}");
        }
        /// <summary>
        /// IsValueType && !IsEnum && !IsPrimitive
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsStruct(this Type type)
        {
            return (type.IsValueType && !type.IsEnum && !type.IsPrimitive);
        }
    }
    
    
    /// <summary>
    /// Excel, Google SpreadSheet 등에 대응되는 테이블 Source입니다.
    /// </summary>
    public abstract class TableSource : ScriptableObject
    {
        public const string FilePathKey = "Path";
        public const char PathSeparator = ';';
        public bool ShowDebugLog = true;

        [Tooltip("비워두면 전체 테이블, 입력 시 해당 이름의 테이블만 가져옵니다.")]
        public string TableNameFilter = "";

        protected bool FilterTableName(string tableName) 
            => string.IsNullOrEmpty(TableNameFilter) || tableName == TableNameFilter;

        /// <summary>
        /// Import 과정에서 얻은 Metadata Path 정보. Export 시 사용됨. 
        /// </summary>
        [field: SerializeField, Tooltip("Source에서 얻어온 Sheet들의 path 정보입니다. 임의 수정 시 테이블과 일치하도록 수정해야 합니다.")]
        public List<string> RootPaths { get; private set; } = new();
        
        /// <summary>
        /// Import 과정에서 얻은 row별 Path 정보. Export 시 사용됨. ;로 구분됨. 비어있을 수 있음. 
        /// </summary>
        [field: SerializeField, Tooltip("Source에서 각 Sheet의 row별 세부 path 정보입니다. singleton인 경우 비어있을 수 있습니다. ;로 구분합니다. 임의 수정 시 테이블과 일치하도록 수정해야 합니다.")]
        public List<string> Paths { get; private set; } = new();

        /// <summary>
        /// Source로부터 Table을 얻어옵니다.
        /// </summary>
        /// <returns></returns>
        public abstract List<Table> Import();
        /// <summary>
        /// Source에 Table을 저장합니다.
        /// </summary>
        /// <param name="tables"></param>
        /// <returns></returns>
        public abstract bool Export(List<Table> tables);

        [Button("불러오기")]
        private void ImportAndApply()
        {
            // 1. 테이블 불러오기 (각 source에 맞게)
            var tables = Import();
            
            RootPaths.Clear();
            RootPaths.Capacity = Math.Max(RootPaths.Capacity, tables.Count);
            Paths.Clear();
            Paths.Capacity = Math.Max(Paths.Capacity, tables.Count);
            // 2. 지정된 정보를 각 ScriptableObject에 반영
            foreach (var table in tables)
            {
                RootPaths.Add(table.Path);
                switch (table.Type)
                {
                    // General의 경우, Data["Path"] 값을 가져와 해당 SO에 반영
                    case TableType.General:
                    {
                        ApplyGeneralTable(table);
                        break;
                    }
                    // Singleton에 경우 Path값 그대로 SO에 반영
                    case TableType.Singleton:
                    {
                        ApplySingletonTable(table);
                        break;
                    }
                }
            }
            DebugX.Log($"{this} 불러오기 완료.");
        }

        /// <summary>
        /// General 형태의 테이블을 SO에 적용합니다.
        /// </summary>
        /// <param name="table"></param>
        private void ApplyGeneralTable(Table table)
        {
            var list = table.Data[FilePathKey];
            var paths = new string[list.Count];
            for (int row = 0; row < list.Count; row++)
            {
                var rawPath = list[row] as string;
                if (rawPath == null)
                {
                    if(ShowDebugLog)
                        DebugX.LogWarning($"Path {list[row]} is not an string");
                    continue;
                }

                paths[row] = rawPath;
                var path = Path.Combine(table.Path, rawPath);
                
                try
                {
                    Apply(table, path, row);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                    continue;
                }
            }
            Paths.Add(string.Join(PathSeparator, paths));
        }

        /// <summary>
        /// Singleton 형태의 테이블을 SO에 적용합니다.
        /// </summary>
        /// <param name="table"></param>
        private void ApplySingletonTable(Table table)
        {
            Paths.Add(string.Empty);
            var path = table.Path;
                
            // try
            // {
                Apply(table, path, 0);
            // }
            // catch (Exception e)
            // {
                // Debug.LogWarning(e);
                // return;
            // }
        }

        private static bool IsValidMember(MemberInfo info) =>
            info.MemberType == MemberTypes.Field || info.MemberType == MemberTypes.Property;

        private static bool IsRecursiveBlacklist(Type type) =>
            type == typeof(string);
        
        private Dictionary<string, MemberInfo> GetMemberInfosRecursively(object obj, string prefix = "")
        {
            if (obj == null) return null;
            // 해당 object type에 대해 ...
            var type = obj.GetType();
            // 모든 멤버 얻어옴 - 메소드, 필드, 프로퍼티 등 다양함
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            // 그 중 필드와 프로퍼티만 얻어옴
            var validMembers = members.Where(IsValidMember).ToList();
            
            // 필드와 프로퍼티를 이름 기준으로 associate
            var dict = new Dictionary<string, MemberInfo>();
            foreach (var member in validMembers)
            {
                // 해당 멤버의 실제 타입 얻어오기 (FieldInfo::FieldType, PropertyInfo::PropertyType)
                var memberType = member.GetActualType();
                
                // 클래스 & Serializable한 class인 경우: 내부 필드가 더 있다고 가정
                var isClass = memberType.IsClass || memberType.IsStruct();
                var isSerializable = memberType.GetCustomAttribute<SerializableAttribute>() != null;
                var isNotRecursiveBlacklist = !IsRecursiveBlacklist(memberType);
                if (isClass && isSerializable && isNotRecursiveBlacklist)
                {
                    // 실제 멤버 값 가져오기
                    var memberValue = member.GetActualValue(obj);
                    // 해당 실제 멤버 값의 하위 멤버 정보 다 들고오기
                    var innerDict = GetMemberInfosRecursively(memberValue, prefix+member.Name+".");
                    // 들고온 거 다 넣음
                    if(innerDict != null) foreach (var (memberName, value) in innerDict)
                    {
                        dict.Add(memberName, value);   
                    }
                    else
                    {
                        DebugX.LogWarning($"{prefix}{member.Name} failed to get member infos (innerDict == null, memberValue: {memberValue})");
                    }
                }
                // 자기 자신도 넣음
                dict.Add(prefix+member.Name, member);
            }

            // root 기준에서 전체 출력해 보기
            if(prefix == "" && ShowDebugLog) foreach (var (memberName, member) in dict)
            {
                var value = dict.GetValue(memberName, obj);
                DebugX.Log($"[{type}] <color=yellow>{member.GetActualType()}</color> {memberName.Replace(".", "<color=lime>::</color>")} <color=cyan>{value}</color>");
            }

            return dict;
        }

        /// <summary>
        /// 테이블의 각 list들의 특정 index를 지정된 path에 대응되는 SO에 적용합니다.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="filePath"></param>
        /// <param name="index"></param>
        private void Apply(Table table, string filePath, int index)
        {
            var fileName = filePath.Split('/')[^1];
            // 해당하는 Path의 SO 읽어오기
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(filePath.EndsWith(".asset") ? filePath : filePath+".asset");

            // SO의 클래스 필드 읽어온 뒤 Dictionary<string Path, MemberInfo> 화
            var memberByPath = GetMemberInfosRecursively(asset);
            
            
            var updated = false;
            foreach (var (path, values) in table.Data)
            {
                if(path == FilePathKey) continue;
                
                // table에는 있는 데이터가 property에 없는 경우는 단순하게 무시
                if (!memberByPath.TryGetValue(path, out var property))
                {
                    continue;
                }
                // 데이터 갱신
                var rawValue = values[index];
                if (rawValue == null)
                {
                    continue;
                }

                var propertyType = property.GetActualType();
                object value;
                try
                {
                    var rawValueType = rawValue.GetType();
                    // use raw value
                    if (propertyType == rawValueType)
                    {
                        value = rawValue;
                    }
                    // enum parsing
                    else if (rawValueType == typeof(string) && propertyType.IsEnum 
                        && Enum.TryParse(propertyType, (string)rawValue, true, out var result)
                    ) {
                        value = result;
                    }
                    // force convert
                    else
                    {
                        value = Convert.ChangeType(rawValue, propertyType);
                    }
                }
                catch (Exception e)
                {
                    DebugX.LogWarning(e);
                    continue;
                }
                var newValue = value;
                var oldValue = memberByPath.GetValue(path, asset);
                memberByPath.SetValue(path, asset, newValue);
                // property.SetValue(asset, newValue);
                bool changed = !object.Equals(oldValue, newValue);
                if (ShowDebugLog || changed)
                {
                    DebugX.Log($"[{fileName}] <color=cyan>{path}</color>: <color=green>{oldValue}</color> -> <color=lime>{newValue}</color>");
                }
                updated = updated || changed;
            }

            if (updated)
            {
                Debug.Log($"Updated {filePath}", asset);
                EditorUtility.SetDirty(asset);
            }
        }

        // TODO 계층화된 거 export 구현
        [Button("저장하기")]
        private void StoreAndExport()
        {
            var tables = new List<Table>();

            for (int i = 0; i < RootPaths.Count; i++)
            {
                var rootPath = RootPaths[i];
                var paths = Paths[i];
                var tableType = string.IsNullOrEmpty(paths) ? TableType.Singleton : TableType.General;
                var table = new Table(rootPath, tableType);

                switch (tableType)
                {
                    case TableType.General:
                    {
                        var pathList = paths.Split(PathSeparator).ToList();
                        var elementCount = pathList.Count;
                        // Path 초기화
                        table.Data.Add(FilePathKey, pathList.Select(it => (object)it).ToList());

                        static IList<object> CreateBuffer(int count) => Enumerable.Repeat<object>(null, count).ToList();
                        
                        for (int row = 0; row < elementCount; row++)
                        {
                            var path = Path.Combine(rootPath, pathList[row]);
                            // 해당하는 Path의 SO 읽어오기
                            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path.EndsWith(".asset") ? path : path+".asset");

                            // SO의 클래스 필드 읽어온 뒤 Dictionary<PropertyName, Property> 화
                            var memberByPath = GetMemberInfosRecursively(asset);
                            
                            // SO로부터 읽어와서 list에 넣기
                            foreach (var (memberPath, member) in memberByPath)
                            {
                                if (!table.Data.TryGetValue(memberPath, out var list))
                                {
                                    // 리스트 없으면 지정된 크기 리스트 만들기
                                    list = CreateBuffer(elementCount);
                                    table.Data.Add(memberPath, list);
                                }
                                // row 위치에 값 삽입
                                list[row] = memberByPath.GetValue(memberPath, asset);
                            }
                        }
                        break;
                    }
                    case TableType.Singleton:
                    {
                        var path = rootPath;
                        // 해당하는 Path의 SO 읽어오기
                        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path.EndsWith(".asset") ? path : path+".asset");

                        // SO의 클래스 필드 읽어온 뒤 Dictionary<PropertyName, Property> 화
                        var memberByPath = GetMemberInfosRecursively(asset);
                            
                        // SO로부터 읽어와서 singleton list에 넣기
                        foreach (var (memberPath, member) in memberByPath)
                        {
                            table.Data.Add(memberPath, new SingletonList<object>(memberByPath.GetValue(memberPath, asset)));
                        }
                        break;
                    }
                }
                
                if(ShowDebugLog) DebugX.Log($"saved {table.Path}");
                if(ShowDebugLog) foreach (var (key, list) in table.Data)
                {
                    DebugX.Log($"- {key}: [{list.JoinToString(", ", o => o != null ? o.ToString() : "NULL")}]");
                }
                tables.Add(table);
            }

            if (Export(tables))
            {
                DebugX.Log($"{this} 저장에 성공했습니다.");
            }
            else
            {
                DebugX.LogWarning($"{this} 저장에 실패했습니다!");
            }
        }

        protected static SheetMetadata ParseMetadata(string sheetName, string source)
        {
            var meta = new SheetMetadata(sheetName);
            var lines = source.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                var split = line.Split(':');
                if (split.Length < 2)
                {
                    Debug.LogWarning($"{sheetName} 메타데이터 '{line}' 변환 실패 - :로 구분되어야 함");
                    return null;
                }

                var key = split[0].Trim();
                // 최초 ':' 외에는 일반 문자열로 취급
                var value = string.Join(' ', split, 1, split.Length - 1).Trim();

                meta.ParseAndInsertValue(key, value, out var errorOrNull);
                if (errorOrNull != null)
                {
                    Debug.LogWarning($"{sheetName} 메타데이터 '{line}' 변환 실패 - {errorOrNull}");
                    return null;
                }
            }

            if (!meta.IsValid)
            {
                Debug.LogWarning($"{sheetName} 메타데이터 변환 실패: 유효하지 않음 - {meta}");
                return null;
            }

            return meta;
        }
    }
    
    
}