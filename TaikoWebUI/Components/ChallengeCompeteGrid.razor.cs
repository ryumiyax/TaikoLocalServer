using TaikoWebUI.Pages.Dialogs;
namespace TaikoWebUI.Components;

public partial class ChallengeCompeteGrid
{
    [Parameter] public int Baid { get; set; }
    [Parameter] public int Mode { get; set; }
    [Parameter] public Dictionary<uint, MusicDetail>? MusicDetailDictionary { get; set; } = null;
    [Parameter] public string? SongNameLanguage { get; set; } = null;

    private ChallengeCompetitionResponse? response = new();
    private CancellationTokenSource? cts;
    private int TotalPages { get; set; } = 0;
    private bool isLoading = true;
    private int currentPage = 1;
    private readonly int pageSize = 12;
    private string? searchTerm = null;
    private bool inProgress = false;

    private async Task GetChallengeCompetitionData()
    {
        isLoading = true;

        response = await Client.GetFromJsonAsync<ChallengeCompetitionResponse>($"api/ChallengeCompeteManage/queryPage?mode={(uint)Mode}&baid={Baid}&inProgress={(inProgress ? 1 : 0)}&page={currentPage}&limit={pageSize}&searchTerm={searchTerm}");
        response.ThrowIfNull();

        TotalPages = response.TotalPages;
        isLoading = false;
    }

    private async Task UpdateCompete(ChallengeCompetition cc)
    {
        await GetChallengeCompetitionData();
        var updated = response?.List.Find(c => c.CompId == cc.CompId);
        if (updated != null)
        {
            cc.CompeteMode = updated.CompeteMode;
            cc.Participants = updated.Participants;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (AuthService.LoginRequired && !AuthService.IsLoggedIn)
        {
            await AuthService.LoginWithAuthToken();
        }

        SongNameLanguage ??= await LocalStorage.GetItemAsync<string>("songNameLanguage");
        MusicDetailDictionary ??= await GameDataService.GetMusicDetailDictionary();

        if (AuthService.LoginRequired && !AuthService.IsLoggedIn)
        {
            await AuthService.LoginWithAuthToken();
        }

        await GetChallengeCompetitionData();
        

        BreadcrumbsStateContainer.breadcrumbs.Clear();
        BreadcrumbsStateContainer.breadcrumbs.Add(new BreadcrumbItem((Mode == 1 ? Localizer["Challenge"] : Mode == 2 ? Localizer["Competition"] : Localizer["Official Competition"]), href: $"/ChallengeCompe/{Baid}/" + (Mode == 1 ? "Challenge" : Mode == 2 ? "Competition" : "OfficialCompetition")));
        BreadcrumbsStateContainer.NotifyStateChanged();
    }

    private async Task OnPageChange(int page)
    {
        currentPage = page;
        await GetChallengeCompetitionData();
    }

    private async Task Debounce(Func<Task> action, int delayInMilliseconds)
    {
        // Cancel the previous task
        cts?.Cancel();

        // Create a new CancellationTokenSource
        cts = new CancellationTokenSource();

        try
        {
            // Wait for the delay
            await Task.Delay(delayInMilliseconds, cts.Token);

            // Execute the action
            await action();
        }
        catch (TaskCanceledException)
        {
            // Ignore the exception
        }
    }

    private async Task OnSearch(string search)
    {
        searchTerm = search;
        currentPage = 1;

        // Debounce the GetUsersData method
        await Debounce(GetChallengeCompetitionData, 500); // 500 milliseconds delay
    }

    private async Task OpenDialogAsync(int mode, int maxSongs, int maxDays = 30, int maxParticipant = 20)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true };
        var parameters = new DialogParameters
        {
            { "Mode", mode },
            { "MaxSongs", maxSongs },
            { "MaxDays", maxDays },
            { "MaxParticipant", maxParticipant },
            { "Baid", Baid }
        };

        var dialogRet = await DialogService.ShowAsync<AddChallengeCompetitionDialog>(Localizer["Create"], parameters, options);
        if (dialogRet != null)
        {
            var result = await dialogRet.GetReturnValueAsync<bool?>();
            if (result != null && result == true) await GetChallengeCompetitionData();
        }
    }
}