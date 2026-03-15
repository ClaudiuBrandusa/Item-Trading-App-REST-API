using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shared.Installers;

namespace Infrastructure.Installers;

public sealed class OutboxJobsProcessorInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddQuartz(configure =>
        {
            var jobKey = new JobKey(nameof(ProcessOutboxMessagesJob));
	
            configure
                .AddJob<ProcessOutboxMessagesJob>(jobKey!, (IJobConfigurator cfg) => cfg.WithIdentity(jobKey))
                .AddTrigger(trigger =>
                    trigger.ForJob(jobKey)
                        .WithSimpleSchedule(
                            schedule =>
                                schedule.WithIntervalInSeconds(10)
                                    .RepeatForever()
                        )
                );
        });
        
        services.AddQuartzHostedService();
    }
}