using SharedProject.Models;
using SharedProject.Models.Responses;

namespace TaikoLocalServer.Services.Interfaces;

public interface IChallengeCompeteService
{
    public Task<bool> HasChallengeCompete(uint baid);

    public Task<List<ChallengeCompeteDatum>> GetInProgressChallengeCompete(uint baid);

    public Task<List<ChallengeCompeteDatum>> GetAllChallengeCompete();

    public Task<ChallengeCompetitionResponse> GetChallengeCompetePage(CompeteModeType mode, uint baid, bool inProgress, int page, int limit, string? search);

    public Task<ChallengeCompeteDatum?> GetFirstOrDefaultCompete(uint compId);

    public Task<uint> CreateCompete(uint baid, ChallengeCompeteCreateInfo challengeCompeteInfo);

    public Task<uint> ParticipateCompete(uint compId, uint baid);

    public Task<uint> CreateChallenge(uint baid, uint targetBaid, ChallengeCompeteCreateInfo challengeCompeteInfo);

    public Task<uint> AnswerChallenge(uint compId, uint baid, bool accept);

    public Task UpdateBestScore(uint baid, SongPlayDatum playData, short option, List<uint> createdBestIds, List<uint> newBaids);

    public Task<ChallengeCompetition> FillData(ChallengeCompetition challenge);

    public Task<List<uint>> GetChallengeSongIds(uint baid);

    public Task FinishOrExpireChallengeCompe();
}
