using System.IO;
using Microsoft.Extensions.Configuration;

namespace DBSchemePrinter.Domain
{
    public class Setting
    {
        private IConfiguration Configuration { get; }

        public Setting()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("application.json");

            Configuration = builder.Build();
        }

        public string DbConnectionString => Configuration["App:DbConnectionString"];
        public string JpNameTablesFilePath => Path.Join(Directory.GetCurrentDirectory(), Configuration["App:JpNameFilePaths:Tables"]);
        public string JpNameColumnsFilePath => Path.Join(Directory.GetCurrentDirectory(), Configuration["App:JpNameFilePaths:Columns"]);

    }
}
    