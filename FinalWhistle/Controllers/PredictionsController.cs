using FinalWhistle.Application.Services;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinalWhistle.Controllers;

[Authorize]
public class PredictionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPredictionsService _predictionsService;

    public PredictionsController(ApplicationDbContext context, IPredictionsService predictionsService)
    {
        _context = context;
        _predictionsService = predictionsService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var upcomingMatches = await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Group)
            .Include(m => m.Predictions.Where(p => p.UserId == userId))
            .Where(m => m.HomeTeamId != null && m.AwayTeamId != null)
            .Where(m => m.Status == MatchStatus.Scheduled && m.KickoffTime > DateTime.UtcNow)
            .OrderBy(m => m.KickoffTime)
            .Take(20)
            .ToListAsync();

        return View(upcomingMatches);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int matchId, int homeGoals, int awayGoals)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var success = await _predictionsService.SubmitPredictionAsync(matchId, userId, homeGoals, awayGoals);

        if (success)
            TempData["Success"] = "Prediction saved successfully!";
        else
            TempData["Error"] = "Failed to save prediction. Match may have started or is locked.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> MyPredictions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var predictions = await _predictionsService.GetUserPredictionsAsync(userId);

        return View(predictions);
    }
}
