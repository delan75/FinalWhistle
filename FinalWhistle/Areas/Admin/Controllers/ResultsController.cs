using FinalWhistle.Application.Services;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ResultsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPredictionsService _predictionsService;

    public ResultsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IPredictionsService predictionsService)
    {
        _context = context;
        _userManager = userManager;
        _predictionsService = predictionsService;
    }

    public async Task<IActionResult> Index()
    {
        var matches = await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Group)
            .Include(m => m.Result)
            .Where(m => m.HomeTeamId != null && m.AwayTeamId != null)
            .OrderBy(m => m.KickoffTime)
            .ToListAsync();

        return View(matches);
    }

    public async Task<IActionResult> Enter(int id)
    {
        var match = await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Group)
            .Include(m => m.Result)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null) return NotFound();

        if (match.HomeTeamId == null || match.AwayTeamId == null)
        {
            TempData["Error"] = "Cannot enter result for a match with TBD teams.";
            return RedirectToAction(nameof(Index));
        }

        var result = match.Result ?? new MatchResult
        {
            MatchId = match.Id,
            HomeGoals = 0,
            AwayGoals = 0,
            HasExtraTime = false,
            HasPenalties = false
        };

        ViewBag.Match = match;
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(int id, MatchResult result)
    {
        var match = await _context.Matches
            .Include(m => m.Result)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match == null) return NotFound();

        ModelState.Remove("Match");

        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            
            if (match.Result == null)
            {
                var newResult = new MatchResult
                {
                    MatchId = match.Id,
                    HomeGoals = result.HomeGoals,
                    AwayGoals = result.AwayGoals,
                    HasExtraTime = result.HasExtraTime,
                    ExtraTimeHomeGoals = result.ExtraTimeHomeGoals,
                    ExtraTimeAwayGoals = result.ExtraTimeAwayGoals,
                    HasPenalties = result.HasPenalties,
                    PenaltiesHomeScore = result.PenaltiesHomeScore,
                    PenaltiesAwayScore = result.PenaltiesAwayScore,
                    EnteredByAdminId = user?.Id,
                    EnteredAt = DateTime.UtcNow,
                    RevisionNumber = 1
                };
                _context.MatchResults.Add(newResult);
            }
            else
            {
                match.Result.HomeGoals = result.HomeGoals;
                match.Result.AwayGoals = result.AwayGoals;
                match.Result.HasExtraTime = result.HasExtraTime;
                match.Result.ExtraTimeHomeGoals = result.ExtraTimeHomeGoals;
                match.Result.ExtraTimeAwayGoals = result.ExtraTimeAwayGoals;
                match.Result.HasPenalties = result.HasPenalties;
                match.Result.PenaltiesHomeScore = result.PenaltiesHomeScore;
                match.Result.PenaltiesAwayScore = result.PenaltiesAwayScore;
                match.Result.EnteredByAdminId = user?.Id;
                match.Result.EnteredAt = DateTime.UtcNow;
                match.Result.RevisionNumber++;
            }

            match.Status = MatchStatus.Completed;
            match.IsLockedForPredictions = true;

            await _context.SaveChangesAsync();
            
            // Auto-award points to all predictions for this match
            await _predictionsService.AwardPointsForMatchAsync(match.Id);
            
            TempData["Success"] = "Match result saved and points awarded successfully!";
            return RedirectToAction(nameof(Index));
        }

        match = await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.Id == id);
        
        ViewBag.Match = match;
        return View(result);
    }
}
