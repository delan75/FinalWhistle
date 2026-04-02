using FinalWhistle.Application.Models;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Application.Services;

public interface IStandingsService
{
    Task<List<GroupStanding>> GetAllGroupStandingsAsync(int tournamentId);
    Task<GroupStanding> GetGroupStandingAsync(int groupId);
}

public class StandingsService : IStandingsService
{
    private readonly DbContext _context;

    public StandingsService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<GroupStanding>> GetAllGroupStandingsAsync(int tournamentId)
    {
        var groups = await _context.Set<Group>()
            .Where(g => g.TournamentId == tournamentId)
            .OrderBy(g => g.DisplayOrder)
            .Include(g => g.Teams)
            .ToListAsync();

        var matches = await _context.Set<Match>()
            .Where(m => m.TournamentId == tournamentId && m.Stage == MatchStage.GroupStage && m.Status == MatchStatus.Completed)
            .Include(m => m.Result)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .ToListAsync();

        var standings = new List<GroupStanding>();

        foreach (var group in groups)
        {
            var groupMatches = matches.Where(m => m.GroupId == group.Id).ToList();
            var teamStandings = CalculateStandings(group.Teams.ToList(), groupMatches);
            
            standings.Add(new GroupStanding
            {
                GroupId = group.Id,
                GroupName = group.Name,
                Teams = teamStandings
            });
        }

        return standings;
    }

    public async Task<GroupStanding> GetGroupStandingAsync(int groupId)
    {
        var group = await _context.Set<Group>()
            .Include(g => g.Teams)
            .Include(g => g.Matches.Where(m => m.Status == MatchStatus.Completed))
                .ThenInclude(m => m.Result)
            .Include(g => g.Matches.Where(m => m.Status == MatchStatus.Completed))
                .ThenInclude(m => m.HomeTeam)
            .Include(g => g.Matches.Where(m => m.Status == MatchStatus.Completed))
                .ThenInclude(m => m.AwayTeam)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return new GroupStanding();

        var teamStandings = CalculateStandings(group.Teams.ToList(), group.Matches.ToList());

        return new GroupStanding
        {
            GroupId = group.Id,
            GroupName = group.Name,
            Teams = teamStandings
        };
    }

    private List<TeamStanding> CalculateStandings(List<Team> teams, List<Match> matches)
    {
        var standings = teams.Select(t => new TeamStanding
        {
            TeamId = t.Id,
            TeamName = t.Name,
            TeamSlug = t.Slug,
            FlagUrl = t.FlagUrl
        }).ToList();

        foreach (var match in matches.Where(m => m.Result != null))
        {
            var homeStanding = standings.First(s => s.TeamId == match.HomeTeamId);
            var awayStanding = standings.First(s => s.TeamId == match.AwayTeamId);

            homeStanding.Played++;
            awayStanding.Played++;

            homeStanding.GoalsFor += match.Result!.HomeGoals;
            homeStanding.GoalsAgainst += match.Result.AwayGoals;
            awayStanding.GoalsFor += match.Result.AwayGoals;
            awayStanding.GoalsAgainst += match.Result.HomeGoals;

            if (match.Result.HomeGoals > match.Result.AwayGoals)
            {
                homeStanding.Won++;
                homeStanding.Points += 3;
                awayStanding.Lost++;
            }
            else if (match.Result.HomeGoals < match.Result.AwayGoals)
            {
                awayStanding.Won++;
                awayStanding.Points += 3;
                homeStanding.Lost++;
            }
            else
            {
                homeStanding.Drawn++;
                awayStanding.Drawn++;
                homeStanding.Points++;
                awayStanding.Points++;
            }
        }

        // FIFA tiebreaker rules
        var sorted = standings.OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.GoalDifference)
            .ThenByDescending(s => s.GoalsFor)
            .ThenBy(s => s.TeamName)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].Position = i + 1;
        }

        return sorted;
    }
}
