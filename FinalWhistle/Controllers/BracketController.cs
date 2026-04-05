using FinalWhistle.Application.Models;
using FinalWhistle.Application.Services;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using FinalWhistle.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Controllers;

public class BracketController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBracketService _bracketService;

    public BracketController(ApplicationDbContext context, IBracketService bracketService)
    {
        _context = context;
        _bracketService = bracketService;
    }

    public async Task<IActionResult> Index()
    {
        var tournament = await _context.Tournaments.FirstOrDefaultAsync();
        if (tournament == null) return NotFound();

        var matches = await _bracketService.GetBracketMatchesAsync(tournament.Id);
        var totalGroupMatches = await _context.Matches.CountAsync(m =>
            m.TournamentId == tournament.Id && m.Stage == MatchStage.GroupStage);
        var completedGroupMatches = await _context.Matches.CountAsync(m =>
            m.TournamentId == tournament.Id &&
            m.Stage == MatchStage.GroupStage &&
            m.Status == MatchStatus.Completed);
        var allGroupMatchesCompleted = totalGroupMatches > 0 && totalGroupMatches == completedGroupMatches;
        var viewModel = BracketPresentationBuilder.Build(matches, allGroupMatchesCompleted);

        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> GenerateRoundOf16()
    {
        var tournament = await _context.Tournaments.FirstOrDefaultAsync();
        if (tournament == null) return NotFound();

        var success = await _bracketService.GenerateRoundOf16Async(tournament.Id);

        if (success)
            TempData["Success"] = "Round of 16 bracket generated successfully!";
        else
            TempData["Error"] = "Failed to generate bracket. Ensure all group matches are completed.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AdvanceWinners(MatchStage stage)
    {
        var tournament = await _context.Tournaments.FirstOrDefaultAsync();
        if (tournament == null) return NotFound();

        var success = await _bracketService.AdvanceWinnersAsync(stage, tournament.Id);

        if (success)
            TempData["Success"] = $"Winners advanced to {GetNextStageName(stage)}!";
        else
            TempData["Error"] = $"Failed to advance winners. Ensure all {stage} matches are completed.";

        return RedirectToAction(nameof(Index));
    }

    private string GetNextStageName(MatchStage stage)
    {
        return stage switch
        {
            MatchStage.RoundOf16 => "Quarter Finals",
            MatchStage.QuarterFinal => "Semi Finals",
            MatchStage.SemiFinal => "Final",
            _ => "Next Round"
        };
    }
}
