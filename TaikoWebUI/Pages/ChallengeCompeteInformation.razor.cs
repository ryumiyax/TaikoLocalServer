using SharedProject.Models;

namespace TaikoWebUI.Pages;

public partial class ChallengeCompeteInformation
{
    [Parameter]
    public int Baid { get; set; }

    [Parameter]
    public int CompId { get; set; }

    private ChallengeCompetition? challengeCompetition;
    private UserSetting? userSetting;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (AuthService.LoginRequired && !AuthService.IsLoggedIn)
        {
            await AuthService.LoginWithAuthToken();
        }

        challengeCompetition = await Client.GetFromJsonAsync<ChallengeCompetition>($"/api/ChallengeCompeteManage/comp/{CompId}");
        userSetting = await Client.GetFromJsonAsync<UserSetting>($"api/UserSettings/{Baid}");

        var title = challengeCompetition?.CompeteMode != CompeteModeType.Chanllenge ? challengeCompetition?.CompeteName : (Localizer["FullChallengeTitle"].ToString()
            .Replace("{From}", challengeCompetition?.Holder?.MyDonName)
            .Replace("{To}", challengeCompetition?.Participants?.Find(p => p.Baid != challengeCompetition?.Baid)?.UserInfo?.MyDonName));

        BreadcrumbsStateContainer.breadcrumbs.Clear();
        if (AuthService.IsLoggedIn && !AuthService.IsAdmin) BreadcrumbsStateContainer.breadcrumbs.Add(new BreadcrumbItem(Localizer["Dashboard"], href: "/"));
        else BreadcrumbsStateContainer.breadcrumbs.Add(new BreadcrumbItem(Localizer["Users"], href: "/Users"));
        BreadcrumbsStateContainer.breadcrumbs.Add(new BreadcrumbItem($"{userSetting?.MyDonName}", href: null, disabled: true));
        if (challengeCompetition?.CompeteMode == CompeteModeType.Chanllenge)
        {
            BreadcrumbsStateContainer.breadcrumbs.Add(new BreadcrumbItem(Localizer["Challenge"], href: $"/ChallengeCompe/{Baid}/Challenge", disabled: false));
        }
        else if (challengeCompetition?.CompeteMode == CompeteModeType.Compete)
        {
            BreadcrumbsStateContainer.breadcrumbs.Add(new BreadcrumbItem(Localizer["Competition"], href: $"/ChallengeCompe/{Baid}/Competition", disabled: false));
        }
        else if (challengeCompetition?.CompeteMode == CompeteModeType.OfficialCompete)
        {
            BreadcrumbsStateContainer.breadcrumbs.Add(new BreadcrumbItem(Localizer["Official Competition"], href: $"/ChallengeCompe/{Baid}/OfficialCompetition", disabled: false));
        }
        BreadcrumbsStateContainer.breadcrumbs.Add(new BreadcrumbItem(title != null ? title : Localizer["Unknown"], href: $"/ChallengeCompe/{Baid}/Information/{CompId}", disabled: false));
        BreadcrumbsStateContainer.NotifyStateChanged();
    }
}
