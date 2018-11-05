namespace DBSchemePrinter.Entity
{
    public class ColumnInfomation
    {
        /// <summary>
        /// テーブル名
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// カラム名
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// カラム名(日本語)
        /// </summary>
        public string  ColumnJpName { get; set; }
        /// <summary>
        /// プライマリーキーフラグ
        /// </summary>
        public bool IsPrimaryKey { get; set; }
        /// <summary>
        /// NotNullフラグ
        /// </summary>
        public bool IsNotNull { get; set; }
        /// <summary>
        /// 属性(カラムの型)
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// サイズ
        /// </summary>
        public int? Size { get; set; }
        /// <summary>
        /// 初期値
        /// </summary>
        public string InitialValue { get; set; }
        /// <summary>
        /// 項目説明
        /// </summary>
        public string Detail { get; set; }
    }
}