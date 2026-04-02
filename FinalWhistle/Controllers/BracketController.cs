using FinalWhistle.Application.Models;
using FinalWhistle.Application.Services;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
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

        var viewModel = new BracketViewModel
        {
            RoundOf16 = matches.Where(m => m.Stage == MatchStage.RoundOf16)
                .Select(m => MapToBracketMatch(m)).ToList(),
            QuarterFinals = matches.Where(m => m.Stage == MatchStage.QuarterFinal)
                .Select(m => MapToBracketMatch(m)).ToList(),
            SemiFinals = matches.Where(m => m.Stage == MatchStage.SemiFinal)
                .Select(m => MapToBracketMatch(m)).ToList(),
            Final = matches.FirstOrDefault(m => m.Stage == MatchStage.Final) is { } final 
                ? MapToBracketMatch(final) : null,
            ThirdPlace = matches.FirstOrDefault(m => m.Stage == MatchStage.ThirdPlace) is { } third 
                ? MapToBracketMatch(third) : null,
            CanGenerateR16 = !matches.Any(m => m.Stage == MatchStage.RoundOf16),
            CanAdvanceToQF = matches.Count(m => m.Stage == MatchStage.RoundOf16 && m.Status == MatchStatus.Completed) == 8 
                && !matches.Any(m => m.Stage == MatchStage.QuarterFinal),
            CanAdvanceToSF = matches.Count(m => m.Stage == MatchStage.QuarterFinal && m.Status == MatchStatus.Completed) == 4 
                && !matches.Any(m => m.Stage == MatchStage.SemiFinal),
            CanAdvanceToFinal = matches.Count(m => m.Stage == MatchStage.SemiFinal && m.Status == MatchStatus.Completed) == 2 
                && !matches.Any(m => m.Stage == MatchStage.Final)
        };

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

    private BracketMatch MapToBracketMatch(Domain.Entities.Match match)
    {
        return new BracketMatch
        {
            MatchId = match.Id,
            Stage = match.Stage,
            HomeTeamId = match.HomeTeamId,
            HomeTeamName = match.HomeTeam?.Name ?? "TBD",
            HomeTeamFlag = match.HomeTeam?.FlagUrl ?? string.Empty,
            AwayTeamId = match.AwayTeamId,
            AwayTeamName = match.AwayTeam?.Name ?? "TBD",
            AwayTeamFlag = match.AwayTeam?.FlagUrl ?? string.Empty,
            HomeScore = match.Result?.HomeGoals,
            AwayScore = match.Result?.AwayGoals,
            HasPenalties = match.Result?.HasPenalties ?? false,
            PenaltiesHomeScore = match.Result?.PenaltiesHomeScore,
            PenaltiesAwayScore = match.Result?.PenaltiesAwayScore,
            Status = match.Status,
            KickoffTime = match.KickoffTime
        };
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
