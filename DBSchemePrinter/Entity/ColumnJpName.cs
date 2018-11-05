namespace DBSchemePrinter.Entity
{
    public class ColumnJpName
    {
        /// <summary>
        /// カラムの名前
        /// [TableName].[ColumnName]の形式
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// カラムの日本語名
        /// </summary>
        public string JpName { get; set; }
        /// <summary>
        /// カラムの詳細
        /// </summary>
        public string Detail { get; set; }
    }
}