using Quartz;
using TaikoLocalServer.Services.Jobs;

namespace TaikoLocalServer.Services.Extentions;

public static class ServiceExtensions
{
    public static IServiceCollection AddTaikoDbServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserDatumService, UserDatumService>();
        services.AddScoped<ISongPlayDatumService, SongPlayDatumService>();
        services.AddScoped<ISongBestDatumService, SongBestDatumService>();
        services.AddScoped<IDanScoreDatumService, DanScoreDatumService>();
        services.AddScoped<IChallengeCompeteService, ChallengeCompeteService>();

        return services;
    }

    public static IServiceCollection AddQuartzJobs(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("ChallengeCompeFinishJob");
            q.AddJob<ChallengeCompeFinishJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("ChallengeCompeFinishJob-trigger")
                .WithCronSchedule("0 0/5 * ? * *")
            );
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        return services;
    }
}