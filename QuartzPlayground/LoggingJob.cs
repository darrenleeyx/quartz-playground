using Quartz;

namespace QuartzPlayground;

[DisallowConcurrentExecution]
public class LoggingJob : IJob
{
    private readonly ILogger<LoggingJob> _logger;

    public LoggingJob(ILogger<LoggingJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("{UtcNow}", DateTime.UtcNow);

        return Task.CompletedTask;
    }
}
