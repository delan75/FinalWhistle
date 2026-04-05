using FinalWhistle.Application.Services;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalWhistle.Tests.Security;

public class SecurityAndValidationTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task PredictionsService_RejectsNegativeScores()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        var match = new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            HomeTeamId = 1,
            AwayTeamId = 2,
            KickoffTime = DateTime.UtcNow.AddHours(2),
            Status = MatchStatus.Scheduled,
            IsLockedForPredictions = false
        };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SubmitPredictionAsync(1, "user1", -1, 2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PredictionsService_RejectsExcessiveScores()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        var match = new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            HomeTeamId = 1,
            AwayTeamId = 2,
            KickoffTime = DateTime.UtcNow.AddHours(2),
            Status = MatchStatus.Scheduled,
            IsLockedForPredictions = false
        };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SubmitPredictionAsync(1, "user1", 100, 2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PredictionsService_RejectsNonExistentMatch()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        // Act
        var result = await service.SubmitPredictionAsync(999, "user1", 2, 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PredictionsService_RejectsEmptyUserId()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        var match = new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            HomeTeamId = 1,
            AwayTeamId = 2,
            KickoffTime = DateTime.UtcNow.AddHours(2),
            Status = MatchStatus.Scheduled,
            IsLockedForPredictions = false
        };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SubmitPredictionAsync(1, "", 2, 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PredictionsService_PreventsPredictionAfterKickoff()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        var match = new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            HomeTeamId = 1,
            AwayTeamId = 2,
            KickoffTime = DateTime.UtcNow.AddHours(-1),
            Status = MatchStatus.Live,
            IsLockedForPredictions = true
        };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SubmitPredictionAsync(1, "user1", 2, 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BracketService_PreventsAdvancementWithIncompleteMatches()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var standingsService = new StandingsService(context);
        var service = new BracketService(context, standingsService);

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        context.Tournaments.Add(tournament);

        // Add incomplete R16 match
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

        // Act
        var result = await service.AdvanceWinnersAsync(MatchStage.RoundOf16, 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task StandingsService_HandlesEmptyGroup()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new StandingsService(context);

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        var group = new Group { Id = 1, TournamentId = 1, Name = "Group A", DisplayOrder = 1 };

        context.Tournaments.Add(tournament);
        context.Groups.Add(group);
        await context.SaveChangesAsync();

        // Act
        var standings = await service.GetGroupStandingAsync(1);

        // Assert
        Assert.NotNull(standings);
        Assert.Empty(standings.Teams);
    }

    [Fact]
    public async Task LeaderboardService_HandlesNoUsers()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new LeaderboardService(context);

        // Act
        var leaderboard = await service.GetTopUsersAsync(10);

        // Assert
        Assert.NotNull(leaderboard);
        Assert.Empty(leaderboard);
    }

    [Fact]
    public async Task PredictionsService_HandlesDrawPrediction()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        var match = new Match { Id = 1, TournamentId = 1, Stage = MatchStage.GroupStage, HomeTeamId = 1, AwayTeamId = 2, Status = MatchStatus.Completed };
        var result = new MatchResult { Id = 1, MatchId = 1, HomeGoals = 1, AwayGoals = 1 };
        var prediction = new Prediction { Id = 1, MatchId = 1, UserId = "user1", PredictedHomeGoals = 1, PredictedAwayGoals = 1 };

        context.Matches.Add(match);
        context.MatchResults.Add(result);
        context.Predictions.Add(prediction);
        await context.SaveChangesAsync();

        // Act
        await service.AwardPointsForMatchAsync(1);

        // Assert
        var updatedPrediction = await context.Predictions.FindAsync(1);
        Assert.NotNull(updatedPrediction);
        Assert.Equal(3, updatedPrediction.PointsAwarded);
    }

    [Fact]
    public async Task PredictionsService_HandlesHighScoringMatch()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        var match = new Match { Id = 1, TournamentId = 1, Stage = MatchStage.GroupStage, HomeTeamId = 1, AwayTeamId = 2, Status = MatchStatus.Completed };
        var result = new MatchResult { Id = 1, MatchId = 1, HomeGoals = 5, AwayGoals = 4 };
        var prediction = new Prediction { Id = 1, MatchId = 1, UserId = "user1", PredictedHomeGoals = 5, PredictedAwayGoals = 4 };

        context.Matches.Add(match);
        context.MatchResults.Add(result);
        context.Predictions.Add(prediction);
        await context.SaveChangesAsync();

        // Act
        await service.AwardPointsForMatchAsync(1);

        // Assert
        var updatedPrediction = await context.Predictions.FindAsync(1);
        Assert.NotNull(updatedPrediction);
        Assert.Equal(3, updatedPrediction.PointsAwarded);
    }

    [Fact]
    public async Task BracketService_HandlesNullTeamIds()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var standingsService = new StandingsService(context);
        var service = new BracketService(context, standingsService);

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        context.Tournaments.Add(tournament);

        var match = new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.QuarterFinal,
            HomeTeamId = null,
            AwayTeamId = null,
            Status = MatchStatus.Scheduled
        };

        context.Matches.Add(match);
        await context.SaveChangesAsync();

        // Act
        var bracketMatches = await service.GetBracketMatchesAsync(1);

        // Assert
        Assert.Single(bracketMatches);
        Assert.Null(bracketMatches[0].HomeTeamId);
        Assert.Null(bracketMatches[0].AwayTeamId);
    }
}
