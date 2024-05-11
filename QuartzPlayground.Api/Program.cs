using AspNetCore.Authentication.Basic;
using Microsoft.AspNetCore.Authorization;
using Quartz;
using QuartzPlayground.Api;
using SilkierQuartz;
using SilkierQuartz.Authorization;
using System.Security.Claims;

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

services
    .AddAuthentication("BasicAuthentication")
    .AddBasic("BasicAuthentication", options =>
    {
        options.Realm = "SilkierQuartz";
        options.Events = new BasicEvents
        {
            OnValidateCredentials = context =>
            {
                if (context.Username == "admin" && context.Password == "admin")
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                        new Claim(ClaimTypes.Name, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                    };
                    context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                    context.Success();
                }
                return Task.CompletedTask;
            }
        };
    });

services.AddAuthorization(options =>
{
    options.AddPolicy(SilkierQuartzAuthenticationOptions.AuthorizationPolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

services.AddSingleton(new SilkierQuartzAuthenticationOptions
{
    AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyAuthenticated
});

services.AddSingleton(new SilkierQuartzOptions()
{
    ProductName = "Job Management - ", // SilkierQuartz will be appended by default
    EnableEditor = true,
    VirtualPathRoot = string.Empty,
    UseLocalTime = true,
    DefaultDateFormat = "dd-MM-yyyy",
    DefaultTimeFormat = "HH:mm:ss"
});

services.AddScoped<IAuthorizationHandler, SilkierQuartzDefaultAuthorizationHandler>();

var app = builder.Build();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var scheduler = await app.Services.GetRequiredService<ISchedulerFactory>().GetScheduler();
app.UseSilkierQuartz(config =>
{
    config.Scheduler = scheduler;
});

app.Run();