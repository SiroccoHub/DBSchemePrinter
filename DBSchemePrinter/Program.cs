using System.Linq;
using System.Threading.Tasks;
using DBSchemePrinter.Domain;

namespace DBSchemePrinter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var setting = new Setting();
            await DbSchemePrinter.Run(setting, args.FirstOrDefault());
        }
    }
}
