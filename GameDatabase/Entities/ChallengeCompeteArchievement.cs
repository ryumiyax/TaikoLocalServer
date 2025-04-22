
namespace GameDatabase.Entities;

public partial class ChallengeCompeteArchievement
{
    public uint Baid { get; set; }
    public uint ChallengeCount { get; set; } = 0;
    public uint ChallengeWin { get; set; } = 0;
    public uint ChallengeLose { get; set; } = 0;
    public uint CompeteCount { get; set; } = 0;
    public uint CompeteGold { get; set; } = 0;
    public uint CompeteSilver { get; set; } = 0;
    public uint CompeteCopper { get; set; } = 0;
    public uint OfficialCompeteCount { get; set; } = 0;
    public uint OfficialCompeteGold { get; set; } = 0;
    public uint OfficialCompeteSilver { get; set; } = 0;
    public uint OfficialCompeteCopper { get; set; } = 0;
}
