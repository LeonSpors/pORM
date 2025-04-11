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

        /*
         SQL Query to create the Logs table (run manually if needed):

         CREATE TABLE IF NOT EXISTS Logs (
             Id CHAR(36) NOT NULL,
             Timestamp DATETIME NOT NULL,
             Message TEXT NOT NULL,
             PRIMARY KEY (Id)
         );
        */
        
        // Under the hood it uses MysqlConnector.
        // See https://mysqlconnector.net/connection-options/ for all available configuration options.
        const string host = "localhost";
        const string port = "3306";
        const string database = "YOUR_DATABASE";
        const string username = "YOUR_USERNAME";
        const string password = "YOUR_PASSWORD";
        
        string connectionString = $"Server={host};Port={port};Database={database};User id={username};Password={password}";
        
        builder.Services.AddDatabase(connectionString);
        builder.Services.AddHostedService<ExampleService>();
        
        IHost app = builder.Build();
        
        await app.RunAsync();
    }
}