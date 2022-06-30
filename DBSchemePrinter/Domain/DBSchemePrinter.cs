using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DBSchemePrinter.Entity;

namespace DBSchemePrinter.Domain
{
    public class DbSchemePrinter
    {
        private static NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static async Task Run(Setting setting, string outputDirectoryPath = null)
        {
            Logger.Info("日本語設定取得");
            var columnJpNames = await GetColumnJpNames(setting);
            var tableJpNames = await GetTableJpNames(setting);

            List<ColumnInfomation> columnInfomations;
            List<IndexInfomation> indexInfomations;
            using (var sqlcon = new SqlConnection(setting.DbConnectionString))
            {
                Logger.Info("テーブル情報取得開始");
                columnInfomations = await GetColumnInfomations(sqlcon);
                Logger.Info("テーブル情報取得終了");
                Logger.Info("インデックス情報取得開始");
                indexInfomations = await GetIndexInfomations(sqlcon);
                Logger.Info("インデックス情報取得終了");

            }

            if (string.IsNullOrEmpty(outputDirectoryPath))
                outputDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "out");

            if (!Directory.Exists(outputDirectoryPath))
            {
                Logger.Info($"出力フォルダ作成 {outputDirectoryPath}");
                Directory.CreateDirectory(outputDirectoryPath);
            }

            var tableGroupedColumnInfomantions = columnInfomations.GroupBy(ci => ci.TableName).ToList();

            Logger.Info($"Markdown作成 !TableList.md");
            var tableListMarkdown = BuildTableListMarkdown(tableJpNames, tableGroupedColumnInfomantions);
            await File.WriteAllTextAsync(Path.Combine(outputDirectoryPath, $"!TableList.md"), tableListMarkdown);

            foreach (var tableGroup in tableGroupedColumnInfomantions)
            {
                Logger.Info($"Markdown作成 {tableGroup.Key}.md");
                var tableInfomationMarkdown = BuildTableInfomationMarkdown(columnJpNames, tableJpNames, indexInfomations, tableGroup);
                await File.WriteAllTextAsync(Path.Combine(outputDirectoryPath, $"{tableGroup.Key}.md"), tableInfomationMarkdown);
            }
        }

        private static string BuildTableListMarkdown(List<TableJpName> tableJpNames, List<IGrouping<string, ColumnInfomation>> tableGroupedColumnInfomantions)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# テーブル一覧");
            sb.AppendLine("| TableName | 概要 |");
            sb.AppendLine("|--------------|--------------------|");
            foreach (var tableName in tableGroupedColumnInfomantions.Select(g => g.Key).OrderBy(x => x))
            {
                var tableJpName = tableJpNames.FirstOrDefault(x => x.Name == tableName);
                sb.AppendLine($"| [{tableName}](./{tableName}.md) | {tableJpName?.JpName ?? "-"} |");
            }
            return sb.ToString();
        }

        private static string BuildTableInfomationMarkdown(List<ColumnJpName> columnJpNames, List<TableJpName> tableJpNames, List<IndexInfomation> indexInfomations, IGrouping<string, ColumnInfomation> tableGroup)
        {
            var sb = new StringBuilder();
            var tableJpName = tableJpNames.FirstOrDefault(x => x.Name == tableGroup.Key);

            sb.AppendLine($"# {tableGroup.Key}");
            sb.AppendLine("## Table Infomation");
            sb.AppendLine($"{tableJpName?.JpName ?? ""}");
            sb.AppendLine("");

            sb.AppendLine("## Column");
            sb.AppendLine("");
            sb.AppendLine("| フィールドID | 項目名称 | PK | NotNULL | 属性 | サイズ | 初期値 | 項目説明 |");
            sb.AppendLine("|--------------|----------|----|---------|----------|--------|--------|--------------------|");
            foreach (var columnInfo in tableGroup)
            {
                var columnJpName =
                    columnJpNames.FirstOrDefault(x => x.Name == $"{tableGroup.Key}.{columnInfo.ColumnName}");
                var isPrimaryKeyString = columnInfo.IsPrimaryKey ? "o" : "";
                var isNotNullString = columnInfo.IsNotNull ? "o" : "";
                var sizeString = columnInfo.Size.HasValue ? columnInfo.Size.Value.ToString() : "";
                sb.AppendLine($"| {columnInfo.ColumnName} | {columnJpName?.JpName} | {isPrimaryKeyString} | {isNotNullString} | {columnInfo.Type} | {sizeString} | {columnInfo.InitialValue} | {columnJpName?.Detail} |");
            }
            sb.AppendLine("");

            sb.AppendLine("## Index");
            sb.AppendLine("");

            var tableIndexInfomations = indexInfomations.Where(x => x.TableName == tableGroup.Key);
            foreach (var indexGroup in tableIndexInfomations.GroupBy(x => x.IndexName))
            {
                sb.AppendLine($"### {indexGroup.Key}");
                sb.AppendLine("");
                sb.AppendLine("| フィールドID | 並び順 |");
                sb.AppendLine("|--------------|--------|");
                foreach (var indexInfomation in indexGroup)
                {
                    var isDescendingKeyString = indexInfomation.IsDescendingKey ? "DESC" : "ASC ";
                    sb.AppendLine($"| {indexInfomation.ColumnName} | {isDescendingKeyString} |");
                }
                sb.AppendLine("");
            }

            return sb.ToString();
        }

        private static async Task<List<ColumnInfomation>> GetColumnInfomations(SqlConnection sqlcon)
        {
            return (await sqlcon.QueryAsync<ColumnInfomation>(
                $@"
                    select 
                       TABLE_NAME as 'TableName',
                       COLUMN_NAME as 'ColumnName',
                       ''  as 'ColumnJpName', 
                       case when EXISTS (
 	                    select 1
 	                    from INFORMATION_SCHEMA.KEY_COLUMN_USAGE usage
 	                    where OBJECTPROPERTY(OBJECT_ID(usage.CONSTRAINT_SCHEMA+'.'+usage.CONSTRAINT_NAME), 'IsPrimaryKey') = 1
 		                      AND usage.TABLE_NAME = col.TABLE_NAME
 		                      AND usage.COLUMN_NAME = col.COLUMN_NAME
 　                    ) then 1 else 0 end as 'IsPrimaryKey',
                       case IS_NULLABLE when 'NO' then 1 else 0 end as 'IsNotNull',
                       ISNULL(DATA_TYPE, '') as 'Type', 
                       CHARACTER_MAXIMUM_LENGTH as 'Size',
                       ISNULL(COLUMN_DEFAULT, '') 'InitialValue',
                       '' as 'Detail'
                     from 
                       INFORMATION_SCHEMA.COLUMNS as col
                     where
                        TABLE_NAME in (select name from sys.objects where type = 'U')
                     order by
                       TABLE_NAME, ORDINAL_POSITION"
                , commandTimeout: 0)).ToList();
        }

        private static async Task<List<IndexInfomation>> GetIndexInfomations(SqlConnection sqlcon)
        {
            return (await sqlcon.QueryAsync<IndexInfomation>(
                $@"
                     select
 	                      t.name TableName
 	                    , i.name IndexName
 	                    , c.name ColumnName
                        , i.type_desc DescType
 	                    , ic.is_descending_key IsDescendingKey
                     from sys.index_columns ic
                         join sys.tables t on t.object_id = ic.object_id
                         join sys.columns c on c.object_id = t.object_id and c.column_id = ic.column_id
                         join sys.indexes i on i.object_id = t.object_id and i.index_id = ic.index_id
                     where i.is_hypothetical = 0
                     order by t.name, i.name, ic.index_column_id"
                , commandTimeout: 0)).ToList();
        }

        private static async Task<List<ColumnJpName>> GetColumnJpNames(Setting setting)
        {
            var lines = await File.ReadAllLinesAsync(setting.JpNameColumnsFilePath);
            var result = new List<ColumnJpName>();

            foreach (var line in lines.Skip(1))
            {
                var elements = line.Split("\t");
                if (elements.Length != 3)
                {
                    Logger.Warn($"日本語カラム名の設定ファイルに異常データがあります。{line}");
                    continue;
                }

                result.Add(new ColumnJpName()
                {
                    Name = elements[0].Trim(new char[] { '"' }),
                    JpName = elements[1].Trim(new char[] { '"' }),
                    Detail = elements[2].Trim(new char[] { '"' }),
                });
            }
            return result;
        }

        private static async Task<List<TableJpName>> GetTableJpNames(Setting setting)
        {
            var lines = await File.ReadAllLinesAsync(setting.JpNameTablesFilePath);
            var result = new List<TableJpName>();

            foreach (var line in lines.Skip(1))
            {
                var elements = line.Split("\t");
                if (elements.Length != 2)
                {
                    Logger.Warn($"日本語テーブル名の設定ファイルに異常データがあります。{line}");
                    continue;
                }

                result.Add(new TableJpName()
                {
                    Name = elements[0].Trim(new char[] { '"' }),
                    JpName = elements[1].Trim(new char[] { '"' }),
                });
            }
            return result;
        }
    }
}
