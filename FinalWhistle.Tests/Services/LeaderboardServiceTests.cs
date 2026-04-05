using FinalWhistle.Application.Services;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinalWhistle.Tests.Services;

public class LeaderboardServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetTopUsersAsync_SortsByTotalPoints()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new LeaderboardService(context);

        context.Predictions.AddRange(
            new Prediction { Id = 1, MatchId = 1, UserId = "user1", PointsAwarded = 3 },
            new Prediction { Id = 2, MatchId = 2, UserId = "user1", PointsAwarded = 1 },
            new Prediction { Id = 3, MatchId = 3, UserId = "user2", PointsAwarded = 3 },
            new Prediction { Id = 4, MatchId = 4, UserId = "user2", PointsAwarded = 3 },
            new Prediction { Id = 5, MatchId = 5, UserId = "user3", PointsAwarded = 1 }
        );
        await context.SaveChangesAsync();

        // Act
        var leaderboard = await service.GetTopUsersAsync(10);

        // Assert
        Assert.Equal(3, leaderboard.Count);
        Assert.Equal("user2", leaderboard[0].UserId);
        Assert.Equal(6, leaderboard[0].TotalPoints);
        Assert.Equal("user1", leaderboard[1].UserId);
        Assert.Equal(4, leaderboard[1].TotalPoints);
        Assert.Equal("user3", leaderboard[2].UserId);
        Assert.Equal(1, leaderboard[2].TotalPoints);
    }

    [Fact]
    public async Task GetTopUsersAsync_AppliesTiebreakerByExactScores()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new LeaderboardService(context);

        context.Predictions.AddRange(
            // User1: 6 points (2 exact scores)
            new Prediction { Id = 1, MatchId = 1, UserId = "user1", PointsAwarded = 3 },
            new Prediction { Id = 2, MatchId = 2, UserId = "user1", PointsAwarded = 3 },
            // User2: 6 points (1 exact score, 3 correct results)
            new Prediction { Id = 3, MatchId = 3, UserId = "user2", PointsAwarded = 3 },
            new Prediction { Id = 4, MatchId = 4, UserId = "user2", PointsAwarded = 1 },
            new Prediction { Id = 5, MatchId = 5, UserId = "user2", PointsAwarded = 1 },
            new Prediction { Id = 6, MatchId = 6, UserId = "user2", PointsAwarded = 1 }
        );
        await context.SaveChangesAsync();

        // Act
        var leaderboard = await service.GetTopUsersAsync(10);

        // Assert
        Assert.Equal("user1", leaderboard[0].UserId);
        Assert.Equal(2, leaderboard[0].ExactScores);
        Assert.Equal("user2", leaderboard[1].UserId);
        Assert.Equal(1, leaderboard[1].ExactScores);
    }

    [Fact]
    public async Task GetTopUsersAsync_LimitsResults()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new LeaderboardService(context);

        for (int i = 1; i <= 150; i++)
        {
            context.Predictions.Add(new Prediction { Id = i, MatchId = 1, UserId = $"user{i}", PointsAwarded = i });
        }
        await context.SaveChangesAsync();

        // Act
        var leaderboard = await service.GetTopUsersAsync(100);

        // Assert
        Assert.Equal(100, leaderboard.Count);
    }

    [Fact]
    public async Task GetUserRankAsync_ReturnsCorrectRank()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new LeaderboardService(context);

        context.Predictions.AddRange(
            new Prediction { Id = 1, MatchId = 1, UserId = "user1", PointsAwarded = 10 },
            new Prediction { Id = 2, MatchId = 2, UserId = "user2", PointsAwarded = 8 },
            new Prediction { Id = 3, MatchId = 3, UserId = "user3", PointsAwarded = 6 }
        );
        await context.SaveChangesAsync();

        // Act
        var rank = await service.GetUserRankAsync("user2");

        // Assert
        Assert.NotNull(rank);
        Assert.Equal(2, rank!.Rank);
    }

    [Fact]
    public async Task GetUserRankAsync_ReturnsNullForNonExistentUser()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new LeaderboardService(context);

        context.Predictions.Add(new Prediction { Id = 1, MatchId = 1, UserId = "user1", PointsAwarded = 10, SubmittedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        // Act
        var rank = await service.GetUserRankAsync("nonexistent");

        // Assert
        Assert.Null(rank);
    }

    [Fact]
    public async Task GetTopUsersAsync_CalculatesCorrectResultsCount()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var service = new LeaderboardService(context);

        context.Predictions.AddRange(
            new Prediction { Id = 1, MatchId = 1, UserId = "user1", PointsAwarded = 3 },
            new Prediction { Id = 2, MatchId = 2, UserId = "user1", PointsAwarded = 1 },
            new Prediction { Id = 3, MatchId = 3, UserId = "user1", PointsAwarded = 1 },
            new Prediction { Id = 4, MatchId = 4, UserId = "user1", PointsAwarded = 0 }
        );
        await context.SaveChangesAsync();

        // Act
        var leaderboard = await service.GetTopUsersAsync(10);

        // Assert
        var user = leaderboard.First();
        Assert.Equal(1, user.ExactScores);
        Assert.Equal(2, user.CorrectResults);
        Assert.Equal(4, user.PredictionsCount);
    }

    [Fact]
    public async Task GetTopUsersAsync_UsesUserEmailWhenAvailable()
    {
        var context = GetInMemoryDbContext();
        var service = new LeaderboardService(context);

        context.Users.Add(new ApplicationUser { Id = "user1", Email = "fan@example.com", UserName = "fan@example.com", CreatedAt = DateTime.UtcNow });
        context.Predictions.Add(new Prediction { Id = 1, MatchId = 1, UserId = "user1", PointsAwarded = 3 });
        await context.SaveChangesAsync();

        var leaderboard = await service.GetTopUsersAsync(10);

        Assert.Single(leaderboard);
        Assert.Equal("fan@example.com", leaderboard[0].Username);
    }

    [Fact]
    public async Task GetTopUsersAsync_UsesUnknownWhenUserRecordIsMissing()
    {
        var context = GetInMemoryDbContext();
        var service = new LeaderboardService(context);

        context.Predictions.Add(new Prediction { Id = 1, MatchId = 1, UserId = "missing-user", PointsAwarded = 3 });
        await context.SaveChangesAsync();

        var leaderboard = await service.GetTopUsersAsync(10);

        Assert.Single(leaderboard);
        Assert.Equal("Unknown", leaderboard[0].Username);
    }
}
