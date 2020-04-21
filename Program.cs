using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Cobalt.Database;

namespace Cobalt
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SqliteDatabase.Initialize();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
