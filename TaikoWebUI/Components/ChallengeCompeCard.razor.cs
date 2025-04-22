using TaikoWebUI.Pages.Dialogs;
using SharedProject.Models;
using TaikoLocalServer.Models.Application;
using TaikoWebUI.Pages;
namespace TaikoWebUI.Components;

public partial class ChallengeCompeCard
{
    [Parameter] public ChallengeCompetition? ChallengeCompetition { get; set; }
    [Parameter] public int Baid { get; set; }
    [Parameter] public EventCallback<ChallengeCompetition> Refresh { get; set; }
    [Parameter] public Dictionary<uint, MusicDetail>? MusicDetailDictionary { get; set; } = null;
    [Parameter] public string? SongNameLanguage { get; set; } = null;

    protected override async Task OnInitializedAsync()
    {
        base.OnInitialized();

        if (AuthService.LoginRequired && !AuthService.IsLoggedIn)
        {
            await AuthService.LoginWithAuthToken();
        }

        SongNameLanguage = await LocalStorage.GetItemAsync<string>("songNameLanguage");
        MusicDetailDictionary = await GameDataService.GetMusicDetailDictionary();
    }

    private string GetSongInfo(ChallengeCompetitionSong song)
    {
        MusicDetailDictionary.ThrowIfNull();
        song.MusicDetail.ThrowIfNull();

        var songName = GameDataService.GetMusicNameBySongId(MusicDetailDictionary, song.MusicDetail.SongId, SongNameLanguage);
        if (song.BestScores.Any(bs => bs.Baid == Baid))
        {
            return songName + " (" + Localizer["Played"] + ")";
        }
        return songName;
    }

    private bool SelfHoldedChallengeCompetiton()
    {
        return ChallengeCompetition?.Baid == Baid || AuthService.IsAdmin;
    }

    private bool ChallengeNeedAnswer()
    {
        return !AuthService.IsAdmin && ChallengeCompetition?.State == CompeteState.Waiting && ChallengeCompetition?.Baid != Baid;
    }

    private bool ParticipatedChallengeCompetition()
    {
        return ChallengeCompetition?.Participants?.Find(p => p.Baid == Baid) != null;
    }

    private bool CanParticipateChallengeCompetition()
    {
        return ChallengeCompetition?.CreateTime < DateTime.Now && DateTime.Now < ChallengeCompetition?.ExpireTime && !ParticipatedChallengeCompetition();
    }

    private string FormatChallengeTitle(string template)
    {
        return template
            .Replace("{From}", ChallengeCompetition?.Holder?.MyDonName)
            .Replace("{To}", ChallengeCompetition?.Participants?.Find(p => p.Baid != ChallengeCompetition?.Baid)?.UserInfo?.MyDonName);
    }

    private void GotoInformation()
    {
        NavigationManager.NavigateTo($"/ChallengeCompe/{Baid}/Information/{ChallengeCompetition?.CompId}", forceLoad: false);
    }

    private async Task AnswerChallenge(bool accept)
    {
        if (ChallengeCompetition == null || ChallengeCompetition.State != CompeteState.Waiting) return;
        var url = accept ? $"api/ChallengeCompeteManage/{Baid}/acceptChallenge/{ChallengeCompetition.CompId}" : $"api/ChallengeCompeteManage/{Baid}/rejectChallenge/{ChallengeCompetition.CompId}";
        var response = await Client.GetFromJsonAsync<CommonActionResultResponse>(url);
        if (response?.Result != 0)
        {
            await ShowError(localizeAnswerChanllenge(response?.Result));
            return;
        }
        await Refresh.InvokeAsync(ChallengeCompetition);

        ChallengeCompetition.State = accept ? CompeteState.Normal : CompeteState.Rejected;
    }

    private string localizeAnswerChanllenge(uint? result)
    {
        return result switch
        {
            1 => Localizer["Challenge Not Found"],
            2 => Localizer["Challenge Expired"],
            3 => Localizer["Can't Accept Other's Challenge"],
            4 => Localizer["Can't Accept Other's Challenge"],
            5 => Localizer["Already Operated"],
            _ => Localizer["Unknown Error"],
        };
    }

    private async Task AnswerCompete()
    {
        if (ChallengeCompetition == null) return;
        var response = await Client.GetFromJsonAsync<CommonActionResultResponse>($"api/ChallengeCompeteManage/{Baid}/joinCompete/{ChallengeCompetition.CompId}");
        if (response?.Result != 0)
        {
            await ShowError(localizeAnswerCompete(response?.Result));
            return;
        }
        await Refresh.InvokeAsync(ChallengeCompetition);
    }

    private string localizeAnswerCompete(uint? result)
    {
        return result switch
        {
            1 => Localizer["Compete Not Found"],
            2 => Localizer["Compete Expired"],
            3 => Localizer["Compete is FullFilled"],
            4 => Localizer["Already Operated"],
            _ => Localizer["Unknown Error"],
        };
    }

    private async Task ShowError(string errorWord)
    {
        var options = new DialogOptions { DisableBackdropClick = true };
        await DialogService.ShowMessageBox(
            Localizer["Error"],
            (MarkupString)
            (string)errorWord,
            Localizer["Dialog OK"],
            null, null, options
        );
    }

    private int GetResultRank(int Baid)
    {
        var totalScores = new Dictionary<int, uint>();
        foreach (var song in ChallengeCompetition!.Songs)
        {
            foreach (var score in song.BestScores)
            {
                if (totalScores.ContainsKey((int)score.Baid))
                {
                    totalScores[(int)score.Baid] += score.Score;
                }
                else if (score.Score != 0)
                {
                    totalScores[(int)score.Baid] = score.Score;
                }
            }
        }
        if (totalScores.Count == 0 && ChallengeCompetition.CompeteMode == CompeteModeType.Chanllenge) return -1; 
        foreach (var participant in ChallengeCompetition!.Participants)
        {
            if (!totalScores.ContainsKey((int) participant.Baid))
            {
                totalScores[(int)participant.Baid] = 0;
            }
        }
        var yourScore = totalScores[Baid];
        if (yourScore == 0 && ChallengeCompetition.CompeteMode != CompeteModeType.Chanllenge) return -1;
        return totalScores
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key)
            .ToList()
            .FindIndex(entry => entry.Value == yourScore);
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

    private bool IsParticipated(uint baid)
    {
        if (ChallengeCompetition != null)
        {
            foreach (var participant in ChallengeCompetition!.Participants)
            {
                if (participant.Baid == baid) return participant.IsActive;
            }
        }
        return false;
    }

    private bool IsMyChallenge(uint baid)
    {
        if (ChallengeCompetition != null)
        {
            foreach (var participant in ChallengeCompetition!.Participants)
            {
                if (participant.Baid == baid) return true;
            }
        }
        return false;
    }
}