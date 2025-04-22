namespace TaikoLocalServer.Services.Interfaces;

public interface IDanScoreDatumService
{
    public Task<List<DanScoreDatum>> GetDanScoreDataList(uint baid, DanType danType);

    public Task ClearDanScores(uint danId, DanType danType);
}