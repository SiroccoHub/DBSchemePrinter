namespace DBSchemePrinter.Entity
{
    public class IndexInfomation
    {
        /// <summary>
        /// テーブル名
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// インデックス名
        /// </summary>
        public string IndexName { get; set; }
        /// <summary>
        /// カラム名
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// 降順フラグ
        /// </summary>
        public bool IsDescendingKey { get; set; }
    }
}