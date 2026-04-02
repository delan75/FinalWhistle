using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Application.Services;

public interface IBracketService
{
    Task<bool> GenerateRoundOf16Async(int tournamentId);
    Task<bool> AdvanceWinnersAsync(MatchStage currentStage, int tournamentId);
    Task<List<Match>> GetBracketMatchesAsync(int tournamentId);
}

public class BracketService : IBracketService
{
    private readonly DbContext _context;
    private readonly IStandingsService _standingsService;

    public BracketService(DbContext context, IStandingsService standingsService)
    {
        _context = context;
        _standingsService = standingsService;
    }

    public async Task<bool> GenerateRoundOf16Async(int tournamentId)
    {
        var existingR16 = await _context.Set<Match>()
            .AnyAsync(m => m.TournamentId == tournamentId && m.Stage == MatchStage.RoundOf16);

        if (existingR16)
            return false;

        var standings = await _standingsService.GetAllGroupStandingsAsync(tournamentId);
        
        var qualifiers = standings
            .SelectMany(g => g.Teams.Where(t => t.Position <= 2))
            .ToList();

        if (qualifiers.Count != 16)
            return false;

        var groupWinners = standings.Select(g => g.Teams.First(t => t.Position == 1)).ToList();
        var groupRunners = standings.Select(g => g.Teams.First(t => t.Position == 2)).ToList();

        var r16Matchups = new List<(int home, int away, string label)>
        {
            (groupWinners[0].TeamId, groupRunners[1].TeamId, "1A vs 2B"),
            (groupWinners[2].TeamId, groupRunners[3].TeamId, "1C vs 2D"),
            (groupWinners[4].TeamId, groupRunners[5].TeamId, "1E vs 2F"),
            (groupWinners[6].TeamId, groupRunners[7].TeamId, "1G vs 2H"),
            (groupWinners[1].TeamId, groupRunners[0].TeamId, "1B vs 2A"),
            (groupWinners[3].TeamId, groupRunners[2].TeamId, "1D vs 2C"),
            (groupWinners[5].TeamId, groupRunners[4].TeamId, "1F vs 2E"),
            (groupWinners[7].TeamId, groupRunners[6].TeamId, "1H vs 2G")
        };

        var baseDate = DateTime.UtcNow.AddDays(30);

        foreach (var (index, matchup) in r16Matchups.Select((m, i) => (i, m)))
        {
            var match = new Match
            {
                TournamentId = tournamentId,
                Stage = MatchStage.RoundOf16,
                HomeTeamId = matchup.home,
                AwayTeamId = matchup.away,
                KickoffTime = baseDate.AddDays(index / 2).AddHours((index % 2) * 4),
                Status = MatchStatus.Scheduled,
                IsLockedForPredictions = false
            };

            _context.Set<Match>().Add(match);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AdvanceWinnersAsync(MatchStage currentStage, int tournamentId)
    {
        var nextStage = GetNextStage(currentStage);
        if (nextStage == null)
            return false;

        var currentMatches = await _context.Set<Match>()
            .Include(m => m.Result)
            .Where(m => m.TournamentId == tournamentId && m.Stage == currentStage && m.Status == MatchStatus.Completed)
            .OrderBy(m => m.KickoffTime)
            .ToListAsync();

        var expectedCount = GetExpectedMatchCount(currentStage);
        if (currentMatches.Count != expectedCount)
            return false;

        var existingNext = await _context.Set<Match>()
            .AnyAsync(m => m.TournamentId == tournamentId && m.Stage == nextStage.Value);

        if (existingNext)
            return false;

        var winners = currentMatches.Select(m => GetWinner(m)).ToList();
        var nextMatches = CreateNextRoundMatches(winners, nextStage.Value, tournamentId);

        _context.Set<Match>().AddRange(nextMatches);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Match>> GetBracketMatchesAsync(int tournamentId)
    {
        return await _context.Set<Match>()
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Result)
            .Where(m => m.TournamentId == tournamentId && m.Stage != MatchStage.GroupStage)
            .OrderBy(m => m.Stage)
            .ThenBy(m => m.KickoffTime)
            .ToListAsync();
    }

    private int GetWinner(Match match)
    {
        if (match.Result == null)
            return 0;

        var homeGoals = match.Result.HomeGoals + (match.Result.ExtraTimeHomeGoals ?? 0);
        var awayGoals = match.Result.AwayGoals + (match.Result.ExtraTimeAwayGoals ?? 0);

        if (homeGoals > awayGoals)
            return match.HomeTeamId!.Value;
        if (awayGoals > homeGoals)
            return match.AwayTeamId!.Value;

        if (match.Result.HasPenalties)
        {
            return match.Result.PenaltiesHomeScore > match.Result.PenaltiesAwayScore
                ? match.HomeTeamId!.Value
                : match.AwayTeamId!.Value;
        }

        return 0;
    }

    private List<Match> CreateNextRoundMatches(List<int> winners, MatchStage stage, int tournamentId)
    {
        var matches = new List<Match>();
        var baseDate = DateTime.UtcNow.AddDays(GetDaysOffset(stage));

        for (int i = 0; i < winners.Count; i += 2)
        {
            matches.Add(new Match
            {
                TournamentId = tournamentId,
                Stage = stage,
                HomeTeamId = winners[i] != 0 ? winners[i] : null,
                AwayTeamId = winners[i + 1] != 0 ? winners[i + 1] : null,
                KickoffTime = baseDate.AddDays(i / 2).AddHours((i % 2) * 4),
                Status = MatchStatus.Scheduled,
                IsLockedForPredictions = false
            });
        }

        return matches;
    }

    private MatchStage? GetNextStage(MatchStage current)
    {
        return current switch
        {
            MatchStage.RoundOf16 => MatchStage.QuarterFinal,
            MatchStage.QuarterFinal => MatchStage.SemiFinal,
            MatchStage.SemiFinal => MatchStage.Final,
            _ => null
        };
    }

    private int GetExpectedMatchCount(MatchStage stage)
    {
        return stage switch
        {
            MatchStage.RoundOf16 => 8,
            MatchStage.QuarterFinal => 4,
            MatchStage.SemiFinal => 2,
            _ => 0
        };
    }

    private int GetDaysOffset(MatchStage stage)
    {
        return stage switch
        {
            MatchStage.RoundOf16 => 30,
            MatchStage.QuarterFinal => 40,
            MatchStage.SemiFinal => 50,
            MatchStage.Final => 60,
            _ => 0
        };
    }
}
