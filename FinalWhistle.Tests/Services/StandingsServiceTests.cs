using FinalWhistle.Application.Services;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalWhistle.Tests.Services;

public class StandingsServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetGroupStandingAsync_CalculatesPointsCorrectly()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new StandingsService(context);

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        var group = new Group { Id = 1, TournamentId = 1, Name = "Group A", DisplayOrder = 1 };
        var team1 = new Team { Id = 1, Name = "Team A", Slug = "team-a", GroupId = 1, CreatedAt = DateTime.UtcNow };
        var team2 = new Team { Id = 2, Name = "Team B", Slug = "team-b", GroupId = 1, CreatedAt = DateTime.UtcNow };

        var match = new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            GroupId = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            Status = MatchStatus.Completed
        };

        var result = new MatchResult
        {
            Id = 1,
            MatchId = 1,
            HomeGoals = 3,
            AwayGoals = 1
        };

        context.Tournaments.Add(tournament);
        context.Groups.Add(group);
        context.Teams.AddRange(team1, team2);
        context.Matches.Add(match);
        context.MatchResults.Add(result);
        await context.SaveChangesAsync();

        // Act
        var standings = await service.GetGroupStandingAsync(1);

        // Assert
        Assert.NotNull(standings);
        Assert.Equal(2, standings.Teams.Count);
        
        var winner = standings.Teams.First(t => t.TeamId == 1);
        Assert.Equal(3, winner.Points);
        Assert.Equal(1, winner.Won);
        Assert.Equal(3, winner.GoalsFor);
        Assert.Equal(1, winner.GoalsAgainst);
        Assert.Equal(2, winner.GoalDifference);

        var loser = standings.Teams.First(t => t.TeamId == 2);
        Assert.Equal(0, loser.Points);
        Assert.Equal(1, loser.Lost);
    }

    [Fact]
    public async Task GetGroupStandingAsync_AppliesTiebreakerRules()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new StandingsService(context);

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        var group = new Group { Id = 1, TournamentId = 1, Name = "Group A", DisplayOrder = 1 };
        var team1 = new Team { Id = 1, Name = "Team A", Slug = "team-a", GroupId = 1, CreatedAt = DateTime.UtcNow };
        var team2 = new Team { Id = 2, Name = "Team B", Slug = "team-b", GroupId = 1, CreatedAt = DateTime.UtcNow };
        var team3 = new Team { Id = 3, Name = "Team C", Slug = "team-c", GroupId = 1, CreatedAt = DateTime.UtcNow };

        context.Tournaments.Add(tournament);
        context.Groups.Add(group);
        context.Teams.AddRange(team1, team2, team3);

        // Team A: 1-0 win (3 pts, GD +1)
        context.Matches.Add(new Match { Id = 1, TournamentId = 1, Stage = MatchStage.GroupStage, GroupId = 1, HomeTeamId = 1, AwayTeamId = 3, Status = MatchStatus.Completed });
        context.MatchResults.Add(new MatchResult { Id = 1, MatchId = 1, HomeGoals = 1, AwayGoals = 0 });

        // Team B: 2-1 win (3 pts, GD +1)
        context.Matches.Add(new Match { Id = 2, TournamentId = 1, Stage = MatchStage.GroupStage, GroupId = 1, HomeTeamId = 2, AwayTeamId = 3, Status = MatchStatus.Completed });
        context.MatchResults.Add(new MatchResult { Id = 2, MatchId = 2, HomeGoals = 2, AwayGoals = 1 });

        await context.SaveChangesAsync();

        // Act
        var standings = await service.GetGroupStandingAsync(1);

        // Assert - Team B should be first (more goals for with same points and GD)
        Assert.Equal(2, standings.Teams[0].TeamId);
        Assert.Equal(1, standings.Teams[1].TeamId);
    }

    [Fact]
    public async Task GetAllGroupStandingsAsync_ReturnsAllGroups()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new StandingsService(context);

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        var groupA = new Group { Id = 1, TournamentId = 1, Name = "Group A", DisplayOrder = 1 };
        var groupB = new Group { Id = 2, TournamentId = 1, Name = "Group B", DisplayOrder = 2 };

        context.Tournaments.Add(tournament);
        context.Groups.AddRange(groupA, groupB);
        context.Teams.Add(new Team { Id = 1, Name = "Team A", Slug = "team-a", GroupId = 1, CreatedAt = DateTime.UtcNow });
        context.Teams.Add(new Team { Id = 2, Name = "Team B", Slug = "team-b", GroupId = 2, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act
        var allStandings = await service.GetAllGroupStandingsAsync(1);

        // Assert
        Assert.Equal(2, allStandings.Count);
        Assert.Equal("Group A", allStandings[0].GroupName);
        Assert.Equal("Group B", allStandings[1].GroupName);
    }

    [Fact]
    public async Task GetGroupStandingAsync_IgnoresIncompleteMatches()
    {
        var context = GetInMemoryDbContext();
        var service = new StandingsService(context);

        context.Tournaments.Add(new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow });
        context.Groups.Add(new Group { Id = 1, TournamentId = 1, Name = "Group A", DisplayOrder = 1 });
        context.Teams.AddRange(
            new Team { Id = 1, Name = "Team A", Slug = "team-a", GroupId = 1, CreatedAt = DateTime.UtcNow },
            new Team { Id = 2, Name = "Team B", Slug = "team-b", GroupId = 1, CreatedAt = DateTime.UtcNow });
        context.Matches.AddRange(
            new Match { Id = 1, TournamentId = 1, Stage = MatchStage.GroupStage, GroupId = 1, HomeTeamId = 1, AwayTeamId = 2, Status = MatchStatus.Completed },
            new Match { Id = 2, TournamentId = 1, Stage = MatchStage.GroupStage, GroupId = 1, HomeTeamId = 2, AwayTeamId = 1, Status = MatchStatus.Scheduled });
        context.MatchResults.Add(new MatchResult { Id = 1, MatchId = 1, HomeGoals = 1, AwayGoals = 0 });
        await context.SaveChangesAsync();

        var standings = await service.GetGroupStandingAsync(1);

        var winner = standings.Teams.Single(t => t.TeamId == 1);
        Assert.Equal(1, winner.Played);
        Assert.Equal(3, winner.Points);
    }

    [Fact]
    public async Task GetGroupStandingAsync_ReturnsEmptyStandingForMissingGroup()
    {
        var context = GetInMemoryDbContext();
        var service = new StandingsService(context);

        var standing = await service.GetGroupStandingAsync(999);

        Assert.NotNull(standing);
        Assert.Equal(0, standing.GroupId);
        Assert.Empty(standing.Teams);
    }
}
