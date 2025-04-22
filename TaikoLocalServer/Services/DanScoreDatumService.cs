using GameDatabase.Context;

namespace TaikoLocalServer.Services;

public class DanScoreDatumService : IDanScoreDatumService
{
    private readonly TaikoDbContext context;

    public DanScoreDatumService(TaikoDbContext context)
    {
        this.context = context;
    }

    public async Task<List<DanScoreDatum>> GetDanScoreDataList(uint baid, DanType danType)
    {
        return await context.DanScoreData.Where(datum => datum.Baid == baid && datum.DanType == danType)
            .Include(datum => datum.DanStageScoreData)
            .ToListAsync();
    }

    public async Task ClearDanScores(uint danId, DanType danType)
    {
        await context.DanScoreData.Where(datum => datum.DanId == danId && datum.DanType == danType)
            .Include(datum => datum.DanStageScoreData)
            .ForEachAsync(datum =>
            {
                context.DanStageScoreData.RemoveRange(datum.DanStageScoreData.ToArray());
                context.DanScoreData.Remove(datum);
            });
        await context.SaveChangesAsync();
        return;
    }
}