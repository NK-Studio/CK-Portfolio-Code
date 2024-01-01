using System.Collections.Generic;

namespace DataParser
{
    using TableDictionary = Dictionary<string, IList<object>>;
    public enum TableType
    {
        General,
        Singleton,
    }
    /// <summary>
    /// 테이블을 추상화한 클래스입니다. <c>Dictionary&lt;string, IList&lt;object&gt;&gt;</c> 형태로 구성됩니다.
    /// </summary>
    public class Table
    {
        public TableDictionary Data { get; } = new();
        public string Path { get; }
        public TableType Type { get; }

        public Table(string path, TableType type)
        {
            Path = path;
            Type = type;
        }
        public Table(SheetMetadata meta) : this(meta.Path, meta.Type)
        {
        }
        
    }
}