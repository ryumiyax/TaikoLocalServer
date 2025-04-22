using TaikoWebUI.Pages;

namespace TaikoWebUI.Components;

public partial class ChallengeCompeDetail
{
    [Parameter] public ChallengeCompetition? ChallengeCompetition { get; set; }

    private string? SongNameLanguage { get; set; }
    private Dictionary<uint, MusicDetail> musicDetailDictionary = new();
    private Dictionary<int, List<ChallengeCompetitionBestScore>> cache = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (AuthService.LoginRequired && !AuthService.IsLoggedIn)
        {
            await AuthService.LoginWithAuthToken();
        }

        musicDetailDictionary = await GameDataService.GetMusicDetailDictionary();
        SongNameLanguage = await LocalStorage.GetItemAsync<string>("songNameLanguage");
    }

    private string FormatChallengeTitle(string template)
    {
        return template
            .Replace("{From}", ChallengeCompetition?.Holder?.MyDonName)
            .Replace("{To}", ChallengeCompetition?.Participants?.Find(p => p.Baid != ChallengeCompetition?.Baid)?.UserInfo?.MyDonName);
    }

    private List<ChallengeCompetitionBestScore> GetLeaderBoard(int idx, List<ChallengeCompetitionSong> songs, List<ChallengeCompetitionParticipant> participants)
    {
        if (cache.ContainsKey(idx)) return cache[idx];
        Dictionary<uint, ChallengeCompetitionBestScore> userBestMap = new();
        foreach (var song in songs)
        {
            foreach (var bestScore in song.BestScores)
            {
                if (userBestMap.ContainsKey(bestScore.Baid))
                {
                    userBestMap[bestScore.Baid].Score += bestScore.Score;
                    userBestMap[bestScore.Baid].PlayCount += bestScore.PlayCount;
                    userBestMap[bestScore.Baid].GoodCount += bestScore.GoodCount;
                    userBestMap[bestScore.Baid].OkCount += bestScore.OkCount;
                    userBestMap[bestScore.Baid].MissCount += bestScore.MissCount;
                    userBestMap[bestScore.Baid].DrumrollCount += bestScore.DrumrollCount;
                }
                else userBestMap[bestScore.Baid] = new ChallengeCompetitionBestScore()
                {
                    Baid = bestScore.Baid,
                    UserAppearance = bestScore.UserAppearance,
                    Score = bestScore.Score,
                    PlayCount = bestScore.PlayCount,
                    GoodCount = bestScore.GoodCount,
                    OkCount = bestScore.OkCount,
                    MissCount = bestScore.MissCount,
                    DrumrollCount = bestScore.DrumrollCount,
                };
            }
        }
        foreach (var participant in participants)
        {
            if (!userBestMap.ContainsKey(participant.Baid))
            {
                userBestMap[participant.Baid] = new ChallengeCompetitionBestScore()
                {
                    Baid = participant.Baid,
                    UserAppearance = participant.UserInfo,
                    Score = 0,
                    PlayCount = 0,
                    GoodCount = 0,
                    OkCount = 0,
                    MissCount = 0,
                    DrumrollCount = 0,
                };
            }
        }

        var result = userBestMap.Values.ToList();
        result.Sort((left, right) => {
            int res = (int)(right.Score - left.Score);
            return res != 0 ? res : (int)(right.Baid - left.Baid);
        });
        cache[idx] = result;
        return result;
    }

    private string GetState()
    {
        if (ChallengeCompetition?.State == CompeteState.Waiting)
        {
            return $"{Localizer["Waiting"]} ({GetDateTime(ChallengeCompetition!.CreateTime)}-{ChallengeCompetition!.ExpireTime})";
        }
        else if (ChallengeCompetition?.State == CompeteState.Expired)
        {
            return $"{Localizer["Expired"]} ({GetDateTime(ChallengeCompetition!.CreateTime)}-{ChallengeCompetition!.ExpireTime})";
        }
        else if (ChallengeCompetition?.State == CompeteState.Normal)
        {
            return $"{Localizer["In progress"]} ({GetDateTime(ChallengeCompetition!.BeginTime)}-{ChallengeCompetition!.EndTime})";
        }
        else if (ChallengeCompetition?.State == CompeteState.Finished)
        {
            return $"{Localizer["Finished"]} ({GetDateTime(ChallengeCompetition!.BeginTime)}-{ChallengeCompetition!.EndTime})";
        }
        else if (ChallengeCompetition?.State == CompeteState.Rejected)
        {
            return Localizer["Rejected"];
        }

        return "";
    }

    private string GetDateTime(DateTime dateTime)
    {
        return dateTime.ToString();
    }
}
