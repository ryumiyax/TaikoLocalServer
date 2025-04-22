using Quartz;

namespace TaikoLocalServer.Services.Jobs;

public class ChallengeCompeFinishJob : IJob
{
    private readonly IChallengeCompeteService challengeCompeteService;
    public ChallengeCompeFinishJob(IChallengeCompeteService challengeCompeteService)
    {
        this.challengeCompeteService = challengeCompeteService;
    }


    public async Task Execute(IJobExecutionContext context)
    {

        await challengeCompeteService.FinishOrExpireChallengeCompe();
    }
}
