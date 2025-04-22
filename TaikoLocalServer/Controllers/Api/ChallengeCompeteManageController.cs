using MediatR;
using Microsoft.Extensions.Options;
using OneOf.Types;
using SharedProject.Models;
using SharedProject.Models.Responses;
using TaikoLocalServer.Filters;
using TaikoLocalServer.Settings;

namespace TaikoLocalServer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ChallengeCompeteManageController(IChallengeCompeteService challengeCompeteService, IAuthService authService,
    IOptions<AuthSettings> settings) : BaseController<ChallengeCompeteManageController>
{
    private readonly AuthSettings authSettings = settings.Value;

    [HttpGet]
    [ServiceFilter(typeof(AuthorizeIfRequiredAttribute))]
    public async Task<ActionResult<List<ChallengeCompetition>>> GetAllChallengeCompete()
    {
        List<ChallengeCompeteDatum> datum = await challengeCompeteService.GetAllChallengeCompete();
        List<ChallengeCompetition> converted = new();
        foreach (var data in datum)
        {
            var challengeCompetition = Mappers.ChallengeCompeMappers.MapData(data);
            challengeCompetition = await challengeCompeteService.FillData(challengeCompetition);
            converted.Add(challengeCompetition);
        }

        return Ok(converted);
    }

    [HttpGet("queryPage")]
    [ServiceFilter(typeof(AuthorizeIfRequiredAttribute))]
    public async Task<ActionResult<ChallengeCompetitionResponse>> GetChallengeCompePage(
        [FromQuery] uint mode = 0, [FromQuery] uint baid = 0, [FromQuery] int inProgress = 0, 
        [FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? searchTerm = null
        )
    {
        if (page < 1)
        {
            return BadRequest(new { Message = "Page number cannot be less than 1." });
        }

        if (limit > 200)
        {
            return BadRequest(new { Message = "Limit cannot be greater than 200." });
        }

        if (mode == 0)
        {
            return BadRequest(new { Message = "Invalid mode." });
        }

        ChallengeCompetitionResponse response = await challengeCompeteService.GetChallengeCompetePage((CompeteModeType)mode, baid, inProgress != 0, page, limit, searchTerm);

        return Ok(response);
    }

    [HttpGet("comp/{compId}")]
    [ServiceFilter(typeof(AuthorizeIfRequiredAttribute))]
    public async Task<ActionResult<ChallengeCompetition>> GetChallengeCompe(uint compId)
    {
        var data = await challengeCompeteService.GetFirstOrDefaultCompete(compId);
        if (data == null) return BadRequest(new { Message = $"Compete(CompId={compId}) is Not Exist!"});
        var challengeCompetition = Mappers.ChallengeCompeMappers.MapData(data);
        challengeCompetition = await challengeCompeteService.FillData(challengeCompetition);

        return Ok(challengeCompetition);
    }

    [HttpPost("{baid}/createOfficialCompete")]
    [ServiceFilter(typeof(AuthorizeIfRequiredAttribute))]
    public async Task<IActionResult> CreateOfficialCompete(uint baid, ChallengeCompeteCreateInfo challengeCompeteInfo)
    {
        if (authSettings.AuthenticationRequired)
        {
            var tokenInfo = authService.ExtractTokenInfo(HttpContext);
            if (tokenInfo == null)
            {
                return Unauthorized();
            }

            if (!tokenInfo.Value.isAdmin || tokenInfo.Value.baid != baid)
            {
                return Forbid();
            }
        }

        Logger.LogInformation("CreateOfficialCompete : baid:{Baid} {Request}", baid, JsonFormatter.JsonSerialize(challengeCompeteInfo));

        uint result = await challengeCompeteService.CreateCompete(baid, challengeCompeteInfo);

        return Ok(new CommonActionResultResponse
        {
            Result = result,
        });
    }

    [HttpPost("{baid}/createCompete")]
    [ServiceFilter(typeof(AuthorizeIfRequiredAttribute))]
    public async Task<IActionResult> CreateCompete(uint baid, ChallengeCompeteCreateInfo challengeCompeteInfo)
    {
        Logger.LogInformation("CreateCompete : {Request}", JsonFormatter.JsonSerialize(challengeCompeteInfo));
        uint result = await challengeCompeteService.CreateCompete(baid, challengeCompeteInfo);

        return Ok(new CommonActionResultResponse
        {
            Result = result,
        });
    }

    [HttpPost("{baid}/createChallenge/{targetBaid}")]
    [ServiceFilter(typeof(AuthorizeIfRequiredAttribute))]
    public async Task<IActionResult> CreateChallenge(uint baid, uint targetBaid, ChallengeCompeteCreateInfo challengeCompeteInfo)
    {
        Logger.LogInformation("CreateChallenge : {Request}", JsonFormatter.JsonSerialize(challengeCompeteInfo));
        uint result = await challengeCompeteService.CreateChallenge(baid, targetBaid, challengeCompeteInfo);

        return Ok(new CommonActionResultResponse
        {
            Result = result,
        });
    }

    [HttpGet("{baid}/joinCompete/{compId}")]
    [ServiceFilter(typeof(AuthorizeIfRequiredAttribute))]
    public async Task<ActionResult<bool>> JoinCompete(uint baid, uint compId)
    {
        Logger.LogInformation($"JoinCompete {baid} {compId}");
        uint result = await challengeCompeteService.ParticipateCompete(compId, baid);

        return Ok(new CommonActionResultResponse
        {
            Result = result,
        });
    }

    [HttpGet("{baid}/acceptChallenge/{compId}")]
    [ServiceFilter(typeof(AuthorizeIfRequiredAttribute))]
    public async Task<ActionResult<bool>> AcceptChallenge(uint baid, uint compId)
    {
        Logger.LogInformation($"AcceptChallenge {baid} {compId}");
        uint result = await challengeCompeteService.AnswerChallenge(compId, baid, true);

        return Ok(new CommonActionResultResponse
        {
            Result = result,
        });
    }

    [HttpGet("{baid}/rejectChallenge/{compId}")]
    [ServiceFilter(typeof(AuthorizeIfRequiredAttribute))]
    public async Task<ActionResult<bool>> RejectChallenge(uint baid, uint compId)
    {
        Logger.LogInformation($"RejectChallenge {baid} {compId}");
        uint result = await challengeCompeteService.AnswerChallenge(compId, baid, false);

        return Ok(new CommonActionResultResponse
        {
            Result = result,
        });
    }

}