using ConsoleApp.Sample.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using pORM.Core.Interfaces;

namespace ConsoleApp.Sample.Services;

public class ExampleService : BackgroundService
{
    private readonly IGlobalContext _context;
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(IGlobalContext context, ILogger<ExampleService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested is false)
        {
            // Retrieve the table instance for LogEntry.
            ITable<LogEntry> table = _context.GetTable<LogEntry>();

            // Create a new log entry.
            LogEntry log = new()
            {
                Timestamp = DateTime.UtcNow,
                Message = "Sample log entry created."
            };

            // Add the log entry to the database.
            bool result = await table.AddAsync(log);
            if (result)
                _logger.LogInformation("Log entry added at {Time}", log.Timestamp);
            else
                _logger.LogWarning("Failed to add log entry at {Time}", log.Timestamp);

            // Wait for 30 seconds before checking again.
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
    }
}