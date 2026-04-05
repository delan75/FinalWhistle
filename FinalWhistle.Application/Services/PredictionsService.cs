using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Application.Services;

public interface IPredictionsService
{
    Task<bool> SubmitPredictionAsync(int matchId, string userId, int homeGoals, int awayGoals);
    Task<Prediction?> GetUserPredictionAsync(int matchId, string userId);
    Task<List<Prediction>> GetUserPredictionsAsync(string userId);
    Task<int> CalculateUserTotalPointsAsync(string userId);
    Task AwardPointsForMatchAsync(int matchId);
}

public class PredictionsService : IPredictionsService
{
    private const int MaxAllowedPredictedGoals = 20;
    private readonly DbContext _context;

    public PredictionsService(DbContext context)
    {
        _context = context;
    }

    public async Task<bool> SubmitPredictionAsync(int matchId, string userId, int homeGoals, int awayGoals)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        if (homeGoals < 0 || awayGoals < 0)
            return false;

        if (homeGoals > MaxAllowedPredictedGoals || awayGoals > MaxAllowedPredictedGoals)
            return false;

        var match = await _context.Set<Match>()
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match == null)
            return false;

        if (match.IsLockedForPredictions || match.KickoffTime <= DateTime.UtcNow)
            return false;

        var existing = await _context.Set<Prediction>()
            .FirstOrDefaultAsync(p => p.MatchId == matchId && p.UserId == userId);

        if (existing != null)
        {
            existing.PredictedHomeGoals = homeGoals;
            existing.PredictedAwayGoals = awayGoals;
            existing.SubmittedAt = DateTime.UtcNow;
        }
        else
        {
            var prediction = new Prediction
            {
                MatchId = matchId,
                UserId = userId,
                PredictedHomeGoals = homeGoals,
                PredictedAwayGoals = awayGoals,
                SubmittedAt = DateTime.UtcNow,
                PointsAwarded = null
            };
            _context.Set<Prediction>().Add(prediction);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Prediction?> GetUserPredictionAsync(int matchId, string userId)
    {
        return await _context.Set<Prediction>()
            .Include(p => p.Match)
                .ThenInclude(m => m.HomeTeam)
            .Include(p => p.Match)
                .ThenInclude(m => m.AwayTeam)
            .Include(p => p.Match)
                .ThenInclude(m => m.Result)
            .FirstOrDefaultAsync(p => p.MatchId == matchId && p.UserId == userId);
    }

    public async Task<List<Prediction>> GetUserPredictionsAsync(string userId)
    {
        return await _context.Set<Prediction>()
            .Include(p => p.Match)
                .ThenInclude(m => m.HomeTeam)
            .Include(p => p.Match)
                .ThenInclude(m => m.AwayTeam)
            .Include(p => p.Match)
                .ThenInclude(m => m.Result)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Match.KickoffTime)
            .ToListAsync();
    }

    public async Task<int> CalculateUserTotalPointsAsync(string userId)
    {
        return await _context.Set<Prediction>()
            .Where(p => p.UserId == userId && p.PointsAwarded.HasValue)
            .SumAsync(p => p.PointsAwarded!.Value);
    }

    public async Task AwardPointsForMatchAsync(int matchId)
    {
        var match = await _context.Set<Match>()
            .Include(m => m.Result)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match?.Result == null || match.Status != MatchStatus.Completed)
            return;

        var predictions = await _context.Set<Prediction>()
            .Where(p => p.MatchId == matchId)
            .ToListAsync();

        foreach (var prediction in predictions)
        {
            prediction.PointsAwarded = CalculatePoints(
                prediction.PredictedHomeGoals,
                prediction.PredictedAwayGoals,
                match.Result.HomeGoals,
                match.Result.AwayGoals
            );
        }

        await _context.SaveChangesAsync();
    }

    private int CalculatePoints(int predHome, int predAway, int actualHome, int actualAway)
    {
        // Exact score: 3 points
        if (predHome == actualHome && predAway == actualAway)
            return 3;

        // Correct result (win/draw/loss): 1 point
        var predResult = predHome > predAway ? "W" : predHome < predAway ? "L" : "D";
        var actualResult = actualHome > actualAway ? "W" : actualHome < actualAway ? "L" : "D";

        if (predResult == actualResult)
            return 1;

        // Wrong: 0 points
        return 0;
    }
}
