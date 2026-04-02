using FinalWhistle.Application.Models;
using FinalWhistle.Application.Services;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinalWhistle.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPredictionsService _predictionsService;
    private readonly ILeaderboardService _leaderboardService;

    public DashboardController(
        ApplicationDbContext context,
        IPredictionsService predictionsService,
        ILeaderboardService leaderboardService)
    {
        _context = context;
        _predictionsService = predictionsService;
        _leaderboardService = leaderboardService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var predictions = await _predictionsService.GetUserPredictionsAsync(userId);
        var totalPoints = await _predictionsService.CalculateUserTotalPointsAsync(userId);
        var userRank = await _leaderboardService.GetUserRankAsync(userId);

        var upcomingMatches = await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Predictions.Where(p => p.UserId == userId))
            .Where(m => m.HomeTeamId != null && m.AwayTeamId != null)
            .Where(m => m.Status == MatchStatus.Scheduled && m.KickoffTime > DateTime.UtcNow)
            .OrderBy(m => m.KickoffTime)
            .Take(5)
            .ToListAsync();

        var viewModel = new UserDashboardViewModel
        {
            TotalPoints = totalPoints,
            Rank = userRank?.Rank ?? 0,
            TotalPredictions = predictions.Count,
            ExactScores = predictions.Count(p => p.PointsAwarded == 3),
            CorrectResults = predictions.Count(p => p.PointsAwarded == 1),
            WrongPredictions = predictions.Count(p => p.PointsAwarded == 0),
            RecentPredictions = predictions.Take(10).ToList(),
            UpcomingMatches = upcomingMatches.Select(m => new Domain.Entities.Prediction
            {
                MatchId = m.Id,
                Match = m,
                PredictedHomeGoals = m.Predictions.FirstOrDefault()?.PredictedHomeGoals ?? 0,
                PredictedAwayGoals = m.Predictions.FirstOrDefault()?.PredictedAwayGoals ?? 0
            }).ToList()
        };

        return View(viewModel);
    }
}
