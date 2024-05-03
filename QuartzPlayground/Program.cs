using Microsoft.AspNetCore.Authorization;
using Quartz;
using QuartzPlayground;
using SilkierQuartz;
using SilkierQuartz.Authorization;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddControllers();

services.AddQuartz(options =>
{
    var loggingJobKey = nameof(LoggingJob);

    options
        .AddJob<LoggingJob>(JobKey.Create(loggingJobKey))
        .AddTrigger(trigger =>
            trigger
                .ForJob(loggingJobKey)
                .WithSimpleSchedule(schedule =>
                    schedule.WithIntervalInSeconds(1)
                            .RepeatForever()));
});

services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

services.AddSingleton(new SilkierQuartzOptions { VirtualPathRoot = string.Empty, UseLocalTime = true });
services.AddSingleton(new SilkierQuartzAuthenticationOptions { AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous });
services.AddAuthorization(
    (Action<AuthorizationOptions>)(opts => opts.AddPolicy(
                                          "SilkierQuartz",
                                          (Action<AuthorizationPolicyBuilder>)(builder => builder.AddRequirements(
                                                                                      new SilkierQuartzDefaultAuthorizationRequirement(
                                                                                          SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous))))));
services.AddScoped<IAuthorizationHandler, SilkierQuartzDefaultAuthorizationHandler>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var scheduler = await app.Services.GetRequiredService<ISchedulerFactory>().GetScheduler();
app.UseSilkierQuartz(s => { s.Scheduler = scheduler; });

app.Run();