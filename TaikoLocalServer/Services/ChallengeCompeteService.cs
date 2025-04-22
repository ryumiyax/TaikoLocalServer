
using GameDatabase.Context;
using GameDatabase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedProject.Models;
using SharedProject.Models.Responses;
using SharedProject.Utils;
using TaikoLocalServer.Settings;
using TaikoWebUI.Pages;
using Throw;

namespace TaikoLocalServer.Services;

public class ChallengeCompeteService : IChallengeCompeteService
{
    private readonly TaikoDbContext context;
    private readonly IGameDataService gameDataService;
    private readonly IUserDatumService userDatumService;
    private readonly CompeteSettings competeSettings;
    private readonly ILogger logger;

    private Dictionary<uint, MusicDetail> musicDetailDict;
    public ChallengeCompeteService(TaikoDbContext context, IGameDataService gameDataService, IUserDatumService userDatumService, IOptions<CompeteSettings> settings, ILogger<ChallengeCompeteService> logger)
    {
        this.context = context;
        this.gameDataService = gameDataService;
        this.userDatumService = userDatumService;
        this.competeSettings = settings.Value;
        this.logger = logger;

        this.musicDetailDict = gameDataService.GetMusicDetailDictionary();
    }

    public async Task<bool> HasChallengeCompete(uint baid)
    {
        return await context.ChallengeCompeteData
            .Include(c => c.Participants)
            .Include(c => c.Songs)
                .ThenInclude(s => s.BestScores)
            .AsSplitQuery()
            .AnyAsync(data =>
                data.State == CompeteState.Normal &&
                data.BeginTime < DateTime.Now &&
                data.EndTime > DateTime.Now &&
                data.Participants.Any(participant => participant.Baid == baid && participant.IsActive) &&
                (
                    // Only Play Once need there is no Score for current Compete
                    !data.OnlyPlayOnce || data.Songs.Any(song => !song.BestScores.Any(s => s.Baid == baid))
                )
            );
    }

    public async Task<List<ChallengeCompeteDatum>> GetInProgressChallengeCompete(uint baid)
    {
        return await context.ChallengeCompeteData
            .Include(c => c.Participants)
            .Include(c => c.Songs)
                .ThenInclude(s => s.BestScores)
            .AsSplitQuery()
            .Where(data =>
                data.State == CompeteState.Normal &&
                data.BeginTime < DateTime.Now &&
                data.EndTime > DateTime.Now &&
                data.Participants.Any(participant => participant.Baid == baid && participant.IsActive) &&
                (
                    // Only Play Once need there is no Score for current Compete
                    !data.OnlyPlayOnce || data.Songs.Any(song => !song.BestScores.Any(s => s.Baid == baid))
                )
            ).ToListAsync();
    }

    public async Task<List<ChallengeCompeteDatum>> GetAllChallengeCompete()
    {
        return await context.ChallengeCompeteData
            .Include(c => c.Participants)
            .Include(c => c.Songs)
                .ThenInclude(s => s.BestScores)
            .AsSplitQuery()
            .Where(data => true).ToListAsync();
    }

    public async Task<ChallengeCompetitionResponse> GetChallengeCompetePage(CompeteModeType mode, uint baid, bool inProgress, int page, int limit, string? search)
    {
        IQueryable<ChallengeCompeteDatum>? query = null;
        string? lowSearch = search != null ? search.ToLower() : null;
        bool isAdmin = context.UserData.Where(u => u.Baid == baid).First().IsAdmin;

        if (mode == CompeteModeType.Chanllenge) 
        {
            query = context.ChallengeCompeteData
                .Include(e => e.Songs).ThenInclude(e => e.BestScores).Include(e => e.Participants).AsSplitQuery()
                .Where(e => e.CompeteMode == CompeteModeType.Chanllenge)
                .Where(e => inProgress == false || ((e.CreateTime < DateTime.Now && DateTime.Now < e.ExpireTime) || (e.BeginTime < DateTime.Now && DateTime.Now < e.EndTime)))
                .Where(e => isAdmin || (e.Baid == baid || e.Participants.Any(p => p.Baid == baid)))
                .Where(e => lowSearch == null || (e.CompId.ToString() == lowSearch || e.CompeteName.ToLower().Contains(lowSearch)));
        } 
        else if (mode == CompeteModeType.Compete)
        {
            query = context.ChallengeCompeteData
                .Include(e => e.Songs).ThenInclude(e => e.BestScores).Include(e => e.Participants).AsSplitQuery()
                .Where(e => e.CompeteMode == CompeteModeType.Compete)
                .Where(e => inProgress == false || (e.BeginTime < DateTime.Now && DateTime.Now < e.EndTime))
                .Where(e => isAdmin || (e.Baid == baid || e.Participants.Any(p => p.Baid == baid) || e.Share == ShareType.EveryOne))
                .Where(e => lowSearch == null || (e.CompId.ToString() == lowSearch || e.CompeteName.ToLower().Contains(lowSearch)));
        }
        else if (mode == CompeteModeType.OfficialCompete)
        {
            query = context.ChallengeCompeteData
                .Include(e => e.Songs).ThenInclude(e => e.BestScores).Include(e => e.Participants).AsSplitQuery()
                .Where(e => e.CompeteMode == CompeteModeType.OfficialCompete)
                .Where(e => inProgress == false || (e.BeginTime < DateTime.Now && DateTime.Now < e.EndTime))
                .Where(e => lowSearch == null || (e.CompId.ToString() == lowSearch || e.CompeteName.ToLower().Contains(lowSearch)));
        }
        if (query == null) return new ChallengeCompetitionResponse();

        var total = await query.CountAsync();
        var totalPage = total / limit;
        if (total % limit > 0) totalPage += 1;

        var challengeCompeteDatum= await query
            .OrderByDescending(e => e.CompId).Skip((page - 1) * limit).Take(limit)
            .ToListAsync();

        List<ChallengeCompetition> converted = new();
        foreach (var data in challengeCompeteDatum)
        {
            var challengeCompetition = Mappers.ChallengeCompeMappers.MapData(data);
            challengeCompetition = await FillData(challengeCompetition);
            converted.Add(challengeCompetition);
        }

        return new ChallengeCompetitionResponse
        {
            List = converted,
            Page = page,
            TotalPages = totalPage,
            Total = total
        };
    }

    public Task<ChallengeCompeteDatum?> GetFirstOrDefaultCompete(uint compId)
    {
        return context.ChallengeCompeteData
                .Include(e => e.Songs).ThenInclude(e => e.BestScores).Include(e => e.Participants)
                .AsSplitQuery().Where(c => c.CompId == compId)
                .FirstOrDefaultAsync();
    }

    public async Task<uint> CreateCompete(uint baid, ChallengeCompeteCreateInfo challengeCompeteInfo)
    {
        bool isAdmin = context.UserData.Where(u => u.Baid == baid).First().IsAdmin;
        if (!isAdmin)
        {
            // Can't create Official Compete if you're not Admin
            if (challengeCompeteInfo.CompeteMode == CompeteModeType.OfficialCompete) return 1;
            // Can't create more Compete than limit if you're not Admin
            var processingCompete = await context.ChallengeCompeteData
                .Where(c => c.Baid == baid && c.CompeteMode == CompeteModeType.Compete && ((c.CreateTime < DateTime.Now && DateTime.Now < c.ExpireTime) || (c.BeginTime < DateTime.Now && DateTime.Now < c.EndTime)))
                .ToListAsync();
            if (processingCompete.Count() >= competeSettings.CreateCompeMax)
            {
                return 2;
            }
        }

        ChallengeCompeteDatum challengeCompeteData = new()
        {
            CompId = context.ChallengeCompeteData.Any() ? context.ChallengeCompeteData.AsEnumerable().Max(c => c.CompId) + 1 : 1,
            CompeteMode = challengeCompeteInfo.CompeteMode,
            State = CompeteState.Normal,
            Baid = baid,
            CompeteName = challengeCompeteInfo.Name,
            CompeteDescribe = challengeCompeteInfo.Desc,
            MaxParticipant = challengeCompeteInfo.MaxParticipant,
            OnlyPlayOnce = challengeCompeteInfo.OnlyPlayOnce,
            CreateTime = DateTime.Now,
            ExpireTime = DateTime.Now.AddDays(challengeCompeteInfo.LastFor),
            BeginTime = DateTime.Now,
            EndTime = DateTime.Now.AddDays(challengeCompeteInfo.LastFor),
            LastFor = challengeCompeteInfo.LastFor,
            RequireTitle = challengeCompeteInfo.RequiredTitle,
            Share = challengeCompeteInfo.ShareType,
            CompeteTarget = challengeCompeteInfo.CompeteTargetType
        };
        await context.AddAsync(challengeCompeteData);
        uint index = 0;
        foreach (var song in challengeCompeteInfo.challengeCompeteSongs)
        {
            ChallengeCompeteSongDatum challengeCompeteSongData = new()
            {
                CompId = challengeCompeteData.CompId,
                SongIndex = index ++,
                SongId = song.SongId,
                Difficulty = song.Difficulty,
                Speed = song.Speed == -1 ? null : (uint)song.Speed,
                IsInverseOn = song.IsInverseOn == -1 ? null : (song.IsInverseOn != 0),
                IsVanishOn = song.IsVanishOn == -1 ? null : (song.IsVanishOn != 0),
                RandomType = song.RandomType == -1 ? null : (RandomType)song.RandomType,
            };
            await context.AddAsync(challengeCompeteSongData);
        }

        if (!isAdmin){ 
            ChallengeCompeteParticipantDatum participantDatum = new()
            {
                CompId = challengeCompeteData.CompId,
                Baid = baid,
                IsActive = true
            };
            await context.AddAsync(participantDatum);
        }

        await context.SaveChangesAsync();
        return 0;
    }

    public async Task<uint> ParticipateCompete(uint compId, uint baid)
    {
        var challengeCompete = await context.ChallengeCompeteData
            .Include(c => c.Participants).Where(c => c.CompId == compId).FirstOrDefaultAsync();
        if (challengeCompete == null) return 1;
        if (challengeCompete.ExpireTime < DateTime.Now) return 2;
        if (challengeCompete.MaxParticipant <= challengeCompete.Participants.Count()) return 3;
        foreach (var participant in challengeCompete.Participants)
        {
            if (participant.Baid == baid) return 4;
        }

        ChallengeCompeteParticipantDatum participantDatum = new() 
        { 
            CompId = challengeCompete.CompId,
            Baid = baid,
            IsActive = true,
        };
        await context.AddAsync(participantDatum);
        await context.ChallengeCompeteData.Where(e => e.CompId == compId)
            .ExecuteUpdateAsync(e => e.SetProperty(d => d.EndTime, e => e.BeginTime.AddDays(e.LastFor)));
        await context.SaveChangesAsync();

        return 0;
    }

    public async Task<uint> CreateChallenge(uint baid, uint targetBaid, ChallengeCompeteCreateInfo challengeCompeteInfo)
    {
        bool isAdmin = context.UserData.Where(u => u.Baid == baid).First().IsAdmin;
        if (!isAdmin)
        {
            var processingChallenges = await context.ChallengeCompeteData
                .Include(e => e.Participants)
                .Where(e => e.Baid == baid && e.CompeteMode == CompeteModeType.Chanllenge && ((e.CreateTime < DateTime.Now && DateTime.Now < e.ExpireTime) || (e.BeginTime < DateTime.Now && DateTime.Now < e.EndTime)))
                .ToListAsync();
            if (processingChallenges.Count() >= competeSettings.CreateCompeMax)
            {
                return 1;
            }
            if (processingChallenges.Exists(e => e.Participants.Exists(p => p.Baid == targetBaid)))
            {
                return 2;
            }
        }

        ChallengeCompeteDatum challengeCompeteData = new()
        {
            CompId = context.ChallengeCompeteData.Any() ? context.ChallengeCompeteData.AsEnumerable().Max(c => c.CompId) + 1 : 1,
            CompeteMode = CompeteModeType.Chanllenge,
            State = CompeteState.Waiting,
            Baid = baid,
            CompeteName = challengeCompeteInfo.Name,
            CompeteDescribe = challengeCompeteInfo.Desc,
            MaxParticipant = 2,
            OnlyPlayOnce = challengeCompeteInfo.OnlyPlayOnce,
            CreateTime = DateTime.Now,
            ExpireTime = DateTime.Now.AddDays(competeSettings.ChallengeExpireDays),
            BeginTime = DateTime.MaxValue,
            EndTime = DateTime.MinValue,
            LastFor = challengeCompeteInfo.LastFor,
            RequireTitle = challengeCompeteInfo.RequiredTitle,
            Share = challengeCompeteInfo.ShareType,
            CompeteTarget = challengeCompeteInfo.CompeteTargetType
        };
        await context.AddAsync(challengeCompeteData);
        uint index = 0;
        foreach (var song in challengeCompeteInfo.challengeCompeteSongs)
        {
            ChallengeCompeteSongDatum challengeCompeteSongData = new()
            {
                CompId = challengeCompeteData.CompId,
                SongIndex = index++,
                SongId = song.SongId,
                Difficulty = song.Difficulty,
                Speed = song.Speed == -1 ? null : (uint)song.Speed,
                IsInverseOn = song.IsInverseOn == -1 ? null : (song.IsInverseOn != 0),
                IsVanishOn = song.IsVanishOn == -1 ? null : (song.IsVanishOn != 0),
                RandomType = song.RandomType == -1 ? null : (RandomType)song.RandomType,
            };
            await context.AddAsync(challengeCompeteSongData);
        }
        ChallengeCompeteParticipantDatum participantDatum = new()
        {
            CompId = challengeCompeteData.CompId,
            Baid = baid,
            IsActive = false
        };
        await context.AddAsync(participantDatum);
        ChallengeCompeteParticipantDatum targetDatum = new()
        {
            CompId = challengeCompeteData.CompId,
            Baid = targetBaid,
            IsActive = false
        };
        await context.AddAsync(targetDatum);
        await context.SaveChangesAsync();

        return 0;
    }

    public async Task<uint> AnswerChallenge(uint compId, uint baid, bool accept)
    {
        var challengeCompete = await context.ChallengeCompeteData
            .Include(c => c.Participants).Where(c => c.CompId == compId).FirstOrDefaultAsync();
        if (challengeCompete == null) return 1;
        if (challengeCompete.ExpireTime < DateTime.Now) return 2;
        if (challengeCompete.Baid == baid) return 3;
        if (!challengeCompete.Participants.Any(p => p.Baid == baid)) return 4;
        if (challengeCompete.State != CompeteState.Waiting) return 5;

        if (accept)
        {
            challengeCompete.State = CompeteState.Normal;
            challengeCompete.BeginTime = DateTime.Now;
            challengeCompete.EndTime = DateTime.Now.AddDays(challengeCompete.LastFor);
            foreach (var participant in challengeCompete.Participants)
            {
                participant.IsActive = true;
                context.Update(participant);
            }
        }
        else
        {
            challengeCompete.State = CompeteState.Rejected;
        }
        context.Update(challengeCompete);
        await context.SaveChangesAsync();

        return 0;
    }

    public async Task UpdateBestScore(uint baid, SongPlayDatum playData, short option, List<uint> createdBestIds, List<uint> newBaids)
    {
        List<ChallengeCompeteDatum> challengeCompetes = context.ChallengeCompeteData
            .Include(e => e.Songs)
                .ThenInclude(s => s.BestScores)
            .Include(e => e.Participants)
            .AsSplitQuery()
            .Where(e => e.State == CompeteState.Normal)
            .Where(e => e.Participants.Any(d => d.Baid == baid && d.IsActive))
            .Where(e => e.Songs.Any(d => d.SongId == playData.SongId && d.Difficulty == playData.Difficulty))
            .Where(e => !e.OnlyPlayOnce || e.Songs.Any(song => !song.BestScores.Any(s => s.Baid == baid)))
            .ToList();
        PlaySetting setting = PlaySettingConverter.ShortToPlaySetting(option);
        foreach (var challengeCompete in challengeCompetes)
        {
            List<ChallengeCompeteSongDatum>? songs = challengeCompete.Songs.FindAll(e => e.SongId == playData.SongId && e.Difficulty == playData.Difficulty);
            foreach (var song in songs)
            {
                if (song == null) continue;
                if (song.Speed != null && song.Speed != setting.Speed) continue;
                if (song.IsVanishOn != null && song.IsVanishOn != setting.IsVanishOn) continue;
                if (song.IsInverseOn != null && song.IsInverseOn != setting.IsInverseOn) continue;
                if (song.RandomType != null && song.RandomType != setting.RandomType) continue;

                ChallengeCompeteBestDatum? bestScore = song.BestScores.Find(e => e.Baid == baid);
                if (bestScore == null)
                {
                    uint newBestId = createdBestIds.Count() == 0 ? (context.ChallengeCompeteBestData.Any() ? context.ChallengeCompeteBestData.AsEnumerable().Max(c => c.BestId) + 1 : 1) : createdBestIds.Max() + 1;
                    var bestData = new ChallengeCompeteBestDatum
                    {
                        BestId = newBestId,
                        CompId = song.CompId,
                        SongIndex = song.SongIndex,
                        Baid = baid,
                        SongId = song.SongId,
                        Difficulty = song.Difficulty,
                        Crown = playData.Crown,
                        Score = playData.Score,
                        ScoreRate = playData.ScoreRate,
                        ScoreRank = playData.ScoreRank,
                        GoodCount = playData.GoodCount,
                        OkCount = playData.OkCount,
                        MissCount = playData.MissCount,
                        ComboCount = playData.ComboCount,
                        HitCount = playData.HitCount,
                        DrumrollCount = playData.DrumrollCount,
                        Skipped = playData.Skipped,
                        PlayCount = 1
                    };
                    createdBestIds.Add(bestData.BestId);
                    await context.AddAsync(bestData);
                }
                else if (!challengeCompete.OnlyPlayOnce && bestScore.Score < playData.Score)
                {
                    bestScore.Crown = playData.Crown;
                    bestScore.Score = playData.Score;
                    bestScore.ScoreRate = playData.ScoreRate;
                    bestScore.ScoreRank = playData.ScoreRank;
                    bestScore.GoodCount = playData.GoodCount;
                    bestScore.OkCount = playData.OkCount;
                    bestScore.MissCount = playData.MissCount;
                    bestScore.ComboCount = playData.ComboCount;
                    bestScore.HitCount = playData.HitCount;
                    bestScore.DrumrollCount = playData.DrumrollCount;
                    bestScore.Skipped = playData.Skipped;
                    bestScore.PlayCount += 1;
                    context.Update(bestScore);
                    if (createdBestIds.Contains(bestScore.BestId))
                    {
                        context.Entry(bestScore).State = EntityState.Added;
                    }
                }
                await FinishChallengeCompe(newBaids, challengeCompete, true);
            }
        }
    }

    public async Task<ChallengeCompetition> FillData(ChallengeCompetition challenge)
    {
        UserDatum? holder = await userDatumService.GetFirstUserDatumOrNull(challenge.Baid);
        challenge.Holder = convert(holder);
        foreach (var participant in challenge.Participants)
        {
            if (participant == null) continue;
            UserDatum? user = await userDatumService.GetFirstUserDatumOrNull(participant.Baid);
            participant.UserInfo = convert(user);
        }
        foreach (var song in challenge.Songs)
        {
            if (song == null) continue;
            song.MusicDetail = musicDetailDict.GetValueOrDefault(song.SongId);
            foreach (var score in song.BestScores)
            {
                UserDatum? user = await userDatumService.GetFirstUserDatumOrNull(score.Baid);
                score.UserAppearance = convert(user);
            }
        }

        return challenge;
    }

    private UserAppearance? convert(UserDatum? user)
    {
        if (user == null) return null;
        return new UserAppearance
        {
            Baid = user.Baid,
            MyDonName = user.MyDonName,
            MyDonNameLanguage = user.MyDonNameLanguage,
            Title = user.Title,
            TitlePlateId = user.TitlePlateId,
            Kigurumi = user.CurrentKigurumi,
            Head = user.CurrentHead,
            Body = user.CurrentBody,
            Face = user.CurrentFace,
            Puchi = user.CurrentPuchi,
            FaceColor = user.ColorFace,
            BodyColor = user.ColorBody,
            LimbColor = user.ColorLimb,
        };
    }

    public async Task<List<uint>> GetChallengeSongIds(uint baid)
    {
        List<uint> songIds = new List<uint>();
        await context.ChallengeCompeteData
            .Include(c => c.Participants)
            .Include(c => c.Songs)
                .ThenInclude(s => s.BestScores)
            .AsSplitQuery()
            .Where(data =>
                data.State == CompeteState.Normal &&
                data.Participants.Any(participant => participant.Baid == baid && participant.IsActive) &&
                (
                    // Only Play Once need there is no Score for current Compete
                    !data.OnlyPlayOnce || data.Songs.Any(song => !song.BestScores.Any(s => s.Baid == baid))
                )
            ).OrderByDescending(data => data.CompeteMode).ForEachAsync(data =>
            {
                foreach (var song in data.Songs)
                {
                    if (!songIds.Contains(song.SongId))
                    {
                        songIds.Add(song.SongId);
                    }
                }
            });
        return songIds;
    }

    public async Task FinishOrExpireChallengeCompe()
    {
        //logger.LogInformation("Finish or Expire ChallengeCompe job triggered!");
        await context.ChallengeCompeteData
            .Where(c => c.State == CompeteState.Waiting)
            .Where(c => c.ExpireTime < DateTime.Now)
            .ForEachAsync(c =>
            {
                c.State = CompeteState.Expired;
                context.Update(c);
            });

        List<uint> newBaids = new();
        await context.ChallengeCompeteData
            .Include(c => c.Participants)
            .Include(c => c.Songs)
                .ThenInclude(s => s.BestScores)
            .AsSplitQuery()
            .Where(c => c.State == CompeteState.Normal)
            .Where(c => c.EndTime < DateTime.Now)
            .ForEachAsync(async c => await FinishChallengeCompe(newBaids, c, false));

        await context.SaveChangesAsync();
    }

    private async Task FinishChallengeCompe(List<uint> newBaids, ChallengeCompeteDatum challengeCompete, bool judgeOnce)
    {
        if (judgeOnce && challengeCompete.OnlyPlayOnce)
        {
            var pCount = challengeCompete.Participants.Count;
            foreach (var song in challengeCompete.Songs)
            {
                if (song.BestScores.Count < pCount) return;
            }
        }

        challengeCompete.State = CompeteState.Finished;
        await UpdateArchievement(newBaids, challengeCompete);
        context.Update(challengeCompete);
        logger.LogInformation("Finished ChallengeCompe CompId={CompId}", challengeCompete.CompId);
    }

    private async Task UpdateArchievement(List<uint> newBaids, ChallengeCompeteDatum challengeCompete)
    {
        var totalScores = new Dictionary<int, uint>();
        foreach (var song in challengeCompete.Songs)
        {
            foreach (var score in song.BestScores)
            {
                if (totalScores.ContainsKey((int)score.Baid))
                {
                    totalScores[(int)score.Baid] += score.Score;
                }
                else if (score.Score > 0)
                {
                    totalScores[(int)score.Baid] = score.Score;
                }
            }
        }
        foreach (var participant in challengeCompete.Participants)
        {
            if (!totalScores.ContainsKey((int) participant.Baid))
            {
                totalScores[(int)participant.Baid] = 0;
            }
        }

        var rank = totalScores
           .OrderByDescending(entry => entry.Value)
           .ThenBy(entry => entry.Key)
           .ToList();

        if (challengeCompete.CompeteMode == CompeteModeType.Chanllenge)
        {
            for (int i = 0; i < rank.Count; i++)
            {
                int realRank = rank[i].Value == 0 ? 1 : rank.FindIndex(e => e.Value == rank[i].Value);
                uint Baid = (uint)rank[i].Key;
                var archievement = await context.ChallengeCompeteArchievementData.Where(e => e.Baid == challengeCompete.Baid).FirstOrDefaultAsync();
                if (archievement == null)
                {
                    archievement = new ChallengeCompeteArchievement()
                    {
                        Baid = Baid,
                        ChallengeCount = 1,
                        ChallengeWin = (uint)(realRank == 0 ? 1 : 0),
                        ChallengeLose = (uint)(realRank == 1 ? 1 : 0),
                    };
                    await context.ChallengeCompeteArchievementData.AddAsync(archievement);
                    newBaids.Add(Baid);
                    logger.LogInformation("Add Challenge Archievement Baid={Archievement} Count={Count} Win={Win} Lose={Lose}", 
                        archievement.Baid, archievement.ChallengeCount, archievement.ChallengeWin, archievement.ChallengeLose);
                }
                else
                {
                    archievement.ChallengeCount += 1;
                    archievement.ChallengeWin += (uint)(realRank == 0 ? 1 : 0);
                    archievement.ChallengeLose += (uint)(realRank == 1 ? 1 : 0);
                    context.ChallengeCompeteArchievementData.Update(archievement);
                    if (newBaids.Contains(archievement.Baid))
                    {
                        context.Entry(archievement).State = EntityState.Added;
                    }
                    logger.LogInformation("Update Challenge Archievement Baid={Archievement} Count={Count} Win={Win} Lose={Lose}", 
                        archievement.Baid, archievement.ChallengeCount, archievement.ChallengeWin, archievement.ChallengeLose);
                }
            }
        }
        else if (challengeCompete.CompeteMode == CompeteModeType.Compete)
        {
            for (int i = 0; i < rank.Count; i++)
            {
                int realRank = rank[i].Value == 0 ? -1 : rank.FindIndex(e => e.Value == rank[i].Value);
                uint Baid = (uint)rank[i].Key;
                var archievement = await context.ChallengeCompeteArchievementData.Where(e => e.Baid == challengeCompete.Baid).FirstOrDefaultAsync();
                if (archievement == null)
                {
                    archievement = new ChallengeCompeteArchievement()
                    {
                        Baid = Baid,
                        CompeteCount = 1,
                        CompeteGold = (uint)(realRank == 0 ? 1 : 0),
                        CompeteSilver = (uint)(realRank == 1 ? 1 : 0),
                        CompeteCopper = (uint)(realRank == 2 ? 1 : 0),
                    };
                    await context.ChallengeCompeteArchievementData.AddAsync(archievement);
                    newBaids.Add(Baid);
                    logger.LogInformation("Add Compete Archievement Baid={Baid} Count={Count} Gold={Gold} Silver={Silver} Coppor={Coppor}", 
                        archievement.Baid, archievement.CompeteCount, archievement.CompeteGold, archievement.CompeteSilver, archievement.CompeteCopper);
                }
                else
                {
                    archievement.CompeteCount += 1;
                    archievement.CompeteGold += (uint)(realRank == 0 ? 1 : 0);
                    archievement.CompeteSilver += (uint)(realRank == 1 ? 1 : 0);
                    archievement.CompeteCopper += (uint)(realRank == 2 ? 1 : 0);
                    context.ChallengeCompeteArchievementData.Update(archievement);
                    if (newBaids.Contains(archievement.Baid))
                    {
                        context.Entry(archievement).State = EntityState.Added;
                    }
                    logger.LogInformation("Update Compete Archievement Baid={Baid} Count={Count} Gold={Gold} Silver={Silver} Coppor={Coppor}",
                        archievement.Baid, archievement.CompeteCount, archievement.CompeteGold, archievement.CompeteSilver, archievement.CompeteCopper);
                }
            }
        }
        else if (challengeCompete.CompeteMode == CompeteModeType.OfficialCompete)
        {
            for (int i = 0; i < rank.Count; i++)
            {
                int realRank = rank[i].Value == 0 ? -1 : rank.FindIndex(e => e.Value == rank[i].Value);
                uint Baid = (uint)rank[i].Key;
                var archievement = await context.ChallengeCompeteArchievementData.Where(e => e.Baid == challengeCompete.Baid).FirstOrDefaultAsync();
                if (archievement == null)
                {
                    archievement = new ChallengeCompeteArchievement()
                    {
                        Baid = Baid,
                        OfficialCompeteCount = 1,
                        OfficialCompeteGold = (uint)(realRank == 0 ? 1 : 0),
                        OfficialCompeteSilver = (uint)(realRank == 1 ? 1 : 0),
                        OfficialCompeteCopper = (uint)(realRank == 2 ? 1 : 0),
                    };
                    await context.ChallengeCompeteArchievementData.AddAsync(archievement);
                    newBaids.Add(Baid);
                    logger.LogInformation("Add Official Compete Archievement Baid={Baid} Count={Count} Gold={Gold} Silver={Silver} Coppor={Coppor}",
                        archievement.Baid, archievement.OfficialCompeteCount, archievement.OfficialCompeteGold, archievement.OfficialCompeteSilver, archievement.OfficialCompeteCopper);
                }
                else
                {
                    archievement.OfficialCompeteCount += 1;
                    archievement.OfficialCompeteGold += (uint)(realRank == 0 ? 1 : 0);
                    archievement.OfficialCompeteSilver += (uint)(realRank == 1 ? 1 : 0);
                    archievement.OfficialCompeteCopper += (uint)(realRank == 2 ? 1 : 0);
                    context.ChallengeCompeteArchievementData.Update(archievement);
                    if (newBaids.Contains(archievement.Baid))
                    {
                        context.Entry(archievement).State = EntityState.Added;
                    }
                    logger.LogInformation("Update Official Compete Archievement Baid={Baid} Count={Count} Gold={Gold} Silver={Silver} Coppor={Coppor}",
                        archievement.Baid, archievement.OfficialCompeteCount, archievement.OfficialCompeteGold, archievement.OfficialCompeteSilver, archievement.OfficialCompeteCopper);
                }
            }
        }
    }
}
