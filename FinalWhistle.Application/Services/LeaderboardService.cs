using FinalWhistle.Application.Models;
using FinalWhistle.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Application.Services;

public interface ILeaderboardService
{
    Task<List<LeaderboardEntry>> GetTopUsersAsync(int count = 50);
    Task<LeaderboardEntry?> GetUserRankAsync(string userId);
}

public class LeaderboardService : ILeaderboardService
{
    private readonly DbContext _context;

    public LeaderboardService(DbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaderboardEntry>> GetTopUsersAsync(int count = 50)
    {
        var userPoints = await _context.Set<Prediction>()
            .Where(p => p.PointsAwarded.HasValue)
            .GroupBy(p => p.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalPoints = g.Sum(p => p.PointsAwarded!.Value),
                PredictionsCount = g.Count(),
                ExactScores = g.Count(p => p.PointsAwarded == 3),
                CorrectResults = g.Count(p => p.PointsAwarded == 1)
            })
            .OrderByDescending(x => x.TotalPoints)
            .ThenByDescending(x => x.ExactScores)
            .ThenBy(x => x.PredictionsCount)
            .Take(count)
            .ToListAsync();

        var userIds = userPoints.Select(x => x.UserId).ToList();
        var users = await _context.Set<ApplicationUser>()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? "Unknown");

        var leaderboard = userPoints.Select((entry, index) => new LeaderboardEntry
        {
            Rank = index + 1,
            UserId = entry.UserId,
            Username = users.GetValueOrDefault(entry.UserId, "Unknown"),
            TotalPoints = entry.TotalPoints,
            PredictionsCount = entry.PredictionsCount,
            ExactScores = entry.ExactScores,
            CorrectResults = entry.CorrectResults
        }).ToList();

        return leaderboard;
    }

    public async Task<LeaderboardEntry?> GetUserRankAsync(string userId)
    {
        var allUsers = await GetTopUsersAsync(10000);
        return allUsers.FirstOrDefault(u => u.UserId == userId);
    }
}
