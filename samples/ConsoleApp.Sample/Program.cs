using ConsoleApp.Sample.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using pORM.Mysql;

namespace ConsoleApp.Sample;

public class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        // Under the hood it uses MysqlConnector. Use their docs configure the connection properly.
        // See https://mysqlconnector.net/connection-options/
        string connectionString = "Server=YOURSERVER;User ID=YOURUSERID;Password=YOURPASSWORD";
        
        builder.Services.AddDatabase(connectionString);
        builder.Services.AddHostedService<ExampleService>();
        
        IHost app = builder.Build();
        
        await app.RunAsync();
    }
}