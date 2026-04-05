using FinalWhistle.Application.Services;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalWhistle.Tests.Services;

public class BracketServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GenerateRoundOf16Async_CreatesEightMatches()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var standingsService = new StandingsService(context);
        var service = new BracketService(context, standingsService);

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        context.Tournaments.Add(tournament);

        // Create 8 groups with 2 teams each
        for (int i = 1; i <= 8; i++)
        {
            var group = new Group { Id = i, TournamentId = 1, Name = $"Group {(char)(64 + i)}", DisplayOrder = i };
            context.Groups.Add(group);

            var team1 = new Team { Id = i * 2 - 1, Name = $"Team {i}A", Slug = $"team-{i}a", GroupId = i, CreatedAt = DateTime.UtcNow };
            var team2 = new Team { Id = i * 2, Name = $"Team {i}B", Slug = $"team-{i}b", GroupId = i, CreatedAt = DateTime.UtcNow };
            context.Teams.AddRange(team1, team2);

            // Create matches to establish standings
            var match = new Match
            {
                Id = i,
                TournamentId = 1,
                Stage = MatchStage.GroupStage,
                GroupId = i,
                HomeTeamId = team1.Id,
                AwayTeamId = team2.Id,
                Status = MatchStatus.Completed
            };
            context.Matches.Add(match);
            context.MatchResults.Add(new MatchResult { Id = i, MatchId = i, HomeGoals = 2, AwayGoals = 0 });
        }

        await context.SaveChangesAsync();

        // Act
        await service.GenerateRoundOf16Async(1);

        // Assert
        var r16Matches = await context.Matches.Where(m => m.Stage == MatchStage.RoundOf16).ToListAsync();
        Assert.Equal(8, r16Matches.Count);
    }

    [Fact]
    public async Task AdvanceWinnersAsync_AdvancesRegularTimeWinner()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var standingsService = new StandingsService(context);
        var service = new BracketService(context, standingsService);

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        context.Tournaments.Add(tournament);

        AddTeams(context, 16);
        AddCompletedKnockoutRound(
            context,
            MatchStage.RoundOf16,
            tournamentId: 1,
            matchCount: 8,
            createResult: i => new MatchResult
            {
                HomeGoals = i + 2,
                AwayGoals = i,
                HasExtraTime = false
            });
        await context.SaveChangesAsync();

        // Act
        var result = await service.AdvanceWinnersAsync(MatchStage.RoundOf16, 1);

        // Assert
        Assert.True(result);
        var qfMatches = await context.Matches
            .Where(m => m.Stage == MatchStage.QuarterFinal)
            .OrderBy(m => m.KickoffTime)
            .ToListAsync();
        Assert.Equal(4, qfMatches.Count);
        Assert.Equal(1, qfMatches[0].HomeTeamId);
        Assert.Equal(3, qfMatches[0].AwayTeamId);
    }

    [Fact]
    public async Task AdvanceWinnersAsync_AdvancesExtraTimeWinner()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var standingsService = new StandingsService(context);
        var service = new BracketService(context, standingsService);

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        context.Tournaments.Add(tournament);

        AddTeams(context, 8);
        AddCompletedKnockoutRound(
            context,
            MatchStage.QuarterFinal,
            tournamentId: 1,
            matchCount: 4,
            createResult: _ => new MatchResult
            {
                HomeGoals = 1,
                AwayGoals = 1,
                HasExtraTime = true,
                ExtraTimeHomeGoals = 1,
                ExtraTimeAwayGoals = 0
            });
        await context.SaveChangesAsync();

        // Act
        var result = await service.AdvanceWinnersAsync(MatchStage.QuarterFinal, 1);

        // Assert
        Assert.True(result);
        var sfMatches = await context.Matches
            .Where(m => m.Stage == MatchStage.SemiFinal)
            .OrderBy(m => m.KickoffTime)
            .ToListAsync();
        Assert.Equal(2, sfMatches.Count);
        Assert.Equal(1, sfMatches[0].HomeTeamId);
        Assert.Equal(3, sfMatches[0].AwayTeamId);
    }

    [Fact]
    public async Task AdvanceWinnersAsync_AdvancesPenaltyWinner()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var standingsService = new StandingsService(context);
        var service = new BracketService(context, standingsService);

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        context.Tournaments.Add(tournament);

        AddTeams(context, 4);
        AddCompletedKnockoutRound(
            context,
            MatchStage.SemiFinal,
            tournamentId: 1,
            matchCount: 2,
            createResult: i => new MatchResult
            {
                HomeGoals = 2,
                AwayGoals = 2,
                HasExtraTime = true,
                ExtraTimeHomeGoals = 0,
                ExtraTimeAwayGoals = 0,
                HasPenalties = true,
                PenaltiesHomeScore = 5 + i,
                PenaltiesAwayScore = 4 + i
            });
        await context.SaveChangesAsync();

        // Act
        var result = await service.AdvanceWinnersAsync(MatchStage.SemiFinal, 1);

        // Assert
        Assert.True(result);
        var finalMatches = await context.Matches.Where(m => m.Stage == MatchStage.Final).ToListAsync();
        Assert.Single(finalMatches);
        Assert.Equal(1, finalMatches[0].HomeTeamId);
        Assert.Equal(3, finalMatches[0].AwayTeamId);
    }

    [Fact]
    public async Task GetBracketMatchesAsync_ReturnsAllKnockoutMatches()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var standingsService = new StandingsService(context);
        var service = new BracketService(context, standingsService);

        context.Matches.AddRange(
            new Match { Id = 1, TournamentId = 1, Stage = MatchStage.RoundOf16, HomeTeamId = 1, AwayTeamId = 2, Status = MatchStatus.Completed },
            new Match { Id = 2, TournamentId = 1, Stage = MatchStage.QuarterFinal, HomeTeamId = 3, AwayTeamId = 4, Status = MatchStatus.Scheduled },
            new Match { Id = 3, TournamentId = 1, Stage = MatchStage.GroupStage, HomeTeamId = 5, AwayTeamId = 6, Status = MatchStatus.Completed }
        );
        await context.SaveChangesAsync();

        // Act
        var bracketMatches = await service.GetBracketMatchesAsync(1);

        // Assert
        Assert.Equal(2, bracketMatches.Count);
        Assert.DoesNotContain(bracketMatches, m => m.Stage == MatchStage.GroupStage);
    }

    [Fact]
    public async Task GenerateRoundOf16Async_ReturnsFalse_WhenRoundAlreadyExists()
    {
        var context = GetInMemoryDbContext();
        var standingsService = new StandingsService(context);
        var service = new BracketService(context, standingsService);

        context.Matches.Add(new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.RoundOf16,
            HomeTeamId = 1,
            AwayTeamId = 2,
            Status = MatchStatus.Scheduled
        });
        await context.SaveChangesAsync();

        var result = await service.GenerateRoundOf16Async(1);

        Assert.False(result);
    }

    [Fact]
    public async Task AdvanceWinnersAsync_ReturnsFalse_WhenNextRoundAlreadyExists()
    {
        var context = GetInMemoryDbContext();
        var standingsService = new StandingsService(context);
        var service = new BracketService(context, standingsService);

        AddTeams(context, 16);
        AddCompletedKnockoutRound(
            context,
            MatchStage.RoundOf16,
            tournamentId: 1,
            matchCount: 8,
            createResult: i => new MatchResult { HomeGoals = i + 1, AwayGoals = i, HasExtraTime = false });
        context.Matches.Add(new Match
        {
            Id = 100,
            TournamentId = 1,
            Stage = MatchStage.QuarterFinal,
            HomeTeamId = 1,
            AwayTeamId = 2,
            Status = MatchStatus.Scheduled
        });
        await context.SaveChangesAsync();

        var result = await service.AdvanceWinnersAsync(MatchStage.RoundOf16, 1);

        Assert.False(result);
    }

    private static void AddTeams(ApplicationDbContext context, int teamCount)
    {
        for (int i = 1; i <= teamCount; i++)
        {
            context.Teams.Add(new Team
            {
                Id = i,
                Name = $"Team {i}",
                Slug = $"team-{i}",
                GroupId = 1,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private static void AddCompletedKnockoutRound(
        ApplicationDbContext context,
        MatchStage stage,
        int tournamentId,
        int matchCount,
        Func<int, MatchResult> createResult)
    {
        var kickoff = DateTime.UtcNow;

        for (int i = 0; i < matchCount; i++)
        {
            var matchId = i + 1;
            var match = new Match
            {
                Id = matchId,
                TournamentId = tournamentId,
                Stage = stage,
                HomeTeamId = (i * 2) + 1,
                AwayTeamId = (i * 2) + 2,
                KickoffTime = kickoff.AddHours(i),
                Status = MatchStatus.Completed
            };

            var result = createResult(i);
            result.Id = matchId;
            result.MatchId = matchId;

            context.Matches.Add(match);
            context.MatchResults.Add(result);
        }
    }
}
