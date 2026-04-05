using FinalWhistle.Application.Services;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalWhistle.Tests.Services;

public class PredictionsServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SubmitPredictionAsync_CreatesNewPrediction()
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
        var result = await service.SubmitPredictionAsync(1, "user1", 2, 1);

        // Assert
        Assert.True(result);
        var prediction = await context.Predictions.FirstOrDefaultAsync();
        Assert.NotNull(prediction);
        Assert.Equal(2, prediction.PredictedHomeGoals);
        Assert.Equal(1, prediction.PredictedAwayGoals);
    }

    [Fact]
    public async Task SubmitPredictionAsync_RejectsLockedMatch()
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
        Assert.Empty(context.Predictions);
    }

    [Fact]
    public async Task AwardPointsForMatchAsync_CalculatesExactScore()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        var match = new Match { Id = 1, TournamentId = 1, Stage = MatchStage.GroupStage, HomeTeamId = 1, AwayTeamId = 2, Status = MatchStatus.Completed };
        var result = new MatchResult { Id = 1, MatchId = 1, HomeGoals = 2, AwayGoals = 1 };
        var prediction = new Prediction { Id = 1, MatchId = 1, UserId = "user1", PredictedHomeGoals = 2, PredictedAwayGoals = 1 };

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
    public async Task AwardPointsForMatchAsync_CalculatesCorrectResult()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        var match = new Match { Id = 1, TournamentId = 1, Stage = MatchStage.GroupStage, HomeTeamId = 1, AwayTeamId = 2, Status = MatchStatus.Completed };
        var result = new MatchResult { Id = 1, MatchId = 1, HomeGoals = 3, AwayGoals = 1 };
        var prediction = new Prediction { Id = 1, MatchId = 1, UserId = "user1", PredictedHomeGoals = 2, PredictedAwayGoals = 0 };

        context.Matches.Add(match);
        context.MatchResults.Add(result);
        context.Predictions.Add(prediction);
        await context.SaveChangesAsync();

        // Act
        await service.AwardPointsForMatchAsync(1);

        // Assert
        var updatedPrediction = await context.Predictions.FindAsync(1);
        Assert.NotNull(updatedPrediction);
        Assert.Equal(1, updatedPrediction.PointsAwarded);
    }

    [Fact]
    public async Task AwardPointsForMatchAsync_CalculatesWrongPrediction()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        var match = new Match { Id = 1, TournamentId = 1, Stage = MatchStage.GroupStage, HomeTeamId = 1, AwayTeamId = 2, Status = MatchStatus.Completed };
        var result = new MatchResult { Id = 1, MatchId = 1, HomeGoals = 1, AwayGoals = 2 };
        var prediction = new Prediction { Id = 1, MatchId = 1, UserId = "user1", PredictedHomeGoals = 2, PredictedAwayGoals = 0 };

        context.Matches.Add(match);
        context.MatchResults.Add(result);
        context.Predictions.Add(prediction);
        await context.SaveChangesAsync();

        // Act
        await service.AwardPointsForMatchAsync(1);

        // Assert
        var updatedPrediction = await context.Predictions.FindAsync(1);
        Assert.NotNull(updatedPrediction);
        Assert.Equal(0, updatedPrediction.PointsAwarded);
    }

    [Fact]
    public async Task CalculateUserTotalPointsAsync_SumsAllPoints()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        context.Predictions.AddRange(
            new Prediction { Id = 1, MatchId = 1, UserId = "user1", PointsAwarded = 3 },
            new Prediction { Id = 2, MatchId = 2, UserId = "user1", PointsAwarded = 1 },
            new Prediction { Id = 3, MatchId = 3, UserId = "user1", PointsAwarded = 0 }
        );
        await context.SaveChangesAsync();

        // Act
        var total = await service.CalculateUserTotalPointsAsync("user1");

        // Assert
        Assert.Equal(4, total);
    }

    [Fact]
    public async Task SubmitPredictionAsync_UpdatesExistingPrediction()
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

        var existingPrediction = new Prediction { Id = 1, MatchId = 1, UserId = "user1", PredictedHomeGoals = 1, PredictedAwayGoals = 1 };

        context.Matches.Add(match);
        context.Predictions.Add(existingPrediction);
        await context.SaveChangesAsync();

        // Act
        var result = await service.SubmitPredictionAsync(1, "user1", 3, 2);

        // Assert
        Assert.True(result);
        var updated = await context.Predictions.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal(3, updated.PredictedHomeGoals);
        Assert.Equal(2, updated.PredictedAwayGoals);
    }

    [Fact]
    public async Task SubmitPredictionAsync_AllowsBoundaryScoresAtLimit()
    {
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        context.Matches.Add(new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            HomeTeamId = 1,
            AwayTeamId = 2,
            KickoffTime = DateTime.UtcNow.AddHours(1),
            Status = MatchStatus.Scheduled,
            IsLockedForPredictions = false
        });
        await context.SaveChangesAsync();

        var result = await service.SubmitPredictionAsync(1, "user1", 20, 20);

        Assert.True(result);
        var prediction = await context.Predictions.SingleAsync();
        Assert.Equal(20, prediction.PredictedHomeGoals);
        Assert.Equal(20, prediction.PredictedAwayGoals);
    }

    [Fact]
    public async Task GetUserPredictionAsync_ReturnsPredictionWithMatchDetails()
    {
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        context.Teams.AddRange(
            new Team { Id = 1, Name = "Team A", Slug = "team-a", GroupId = 1, CreatedAt = DateTime.UtcNow },
            new Team { Id = 2, Name = "Team B", Slug = "team-b", GroupId = 1, CreatedAt = DateTime.UtcNow });

        context.Matches.Add(new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            HomeTeamId = 1,
            AwayTeamId = 2,
            KickoffTime = DateTime.UtcNow.AddHours(1),
            Status = MatchStatus.Completed
        });
        context.MatchResults.Add(new MatchResult { Id = 1, MatchId = 1, HomeGoals = 2, AwayGoals = 1 });
        context.Predictions.Add(new Prediction { Id = 1, MatchId = 1, UserId = "user1", PredictedHomeGoals = 2, PredictedAwayGoals = 1 });
        await context.SaveChangesAsync();

        var prediction = await service.GetUserPredictionAsync(1, "user1");

        Assert.NotNull(prediction);
        Assert.Equal("Team A", prediction!.Match.HomeTeam!.Name);
        Assert.Equal("Team B", prediction.Match.AwayTeam!.Name);
        Assert.Equal(2, prediction.Match.Result!.HomeGoals);
    }

    [Fact]
    public async Task GetUserPredictionsAsync_ReturnsNewestMatchesFirst()
    {
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        context.Teams.AddRange(
            new Team { Id = 1, Name = "Team A", Slug = "team-a", GroupId = 1, CreatedAt = DateTime.UtcNow },
            new Team { Id = 2, Name = "Team B", Slug = "team-b", GroupId = 1, CreatedAt = DateTime.UtcNow });

        context.Matches.AddRange(
            new Match
            {
                Id = 1,
                TournamentId = 1,
                Stage = MatchStage.GroupStage,
                HomeTeamId = 1,
                AwayTeamId = 2,
                KickoffTime = DateTime.UtcNow.AddDays(-2),
                Status = MatchStatus.Completed
            },
            new Match
            {
                Id = 2,
                TournamentId = 1,
                Stage = MatchStage.GroupStage,
                HomeTeamId = 2,
                AwayTeamId = 1,
                KickoffTime = DateTime.UtcNow.AddDays(-1),
                Status = MatchStatus.Completed
            });
        context.Predictions.AddRange(
            new Prediction { Id = 1, MatchId = 1, UserId = "user1", PredictedHomeGoals = 1, PredictedAwayGoals = 0 },
            new Prediction { Id = 2, MatchId = 2, UserId = "user1", PredictedHomeGoals = 0, PredictedAwayGoals = 1 });
        await context.SaveChangesAsync();

        var predictions = await service.GetUserPredictionsAsync("user1");

        Assert.Equal(2, predictions.Count);
        Assert.Equal(2, predictions[0].MatchId);
        Assert.Equal(1, predictions[1].MatchId);
    }

    [Fact]
    public async Task AwardPointsForMatchAsync_DoesNothingWhenMatchIsNotCompleted()
    {
        var context = GetInMemoryDbContext();
        var service = new PredictionsService(context);

        context.Matches.Add(new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            HomeTeamId = 1,
            AwayTeamId = 2,
            Status = MatchStatus.Scheduled
        });
        context.MatchResults.Add(new MatchResult { Id = 1, MatchId = 1, HomeGoals = 2, AwayGoals = 1 });
        context.Predictions.Add(new Prediction { Id = 1, MatchId = 1, UserId = "user1", PredictedHomeGoals = 2, PredictedAwayGoals = 1 });
        await context.SaveChangesAsync();

        await service.AwardPointsForMatchAsync(1);

        var prediction = await context.Predictions.FindAsync(1);
        Assert.NotNull(prediction);
        Assert.Null(prediction!.PointsAwarded);
    }
}
