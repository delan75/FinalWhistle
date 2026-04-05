using FinalWhistle.Application.Services;
using FinalWhistle.Areas.Admin.Models;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class GroupsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IStandingsService _standingsService;

    public GroupsController(ApplicationDbContext context, IStandingsService standingsService)
    {
        _context = context;
        _standingsService = standingsService;
    }

    public async Task<IActionResult> Index()
    {
        var tournament = await _context.Tournaments.FirstOrDefaultAsync();
        if (tournament == null) return NotFound();

        var standings = await _standingsService.GetAllGroupStandingsAsync(tournament.Id);
        var progressRows = await _context.Groups
            .Where(g => g.TournamentId == tournament.Id)
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new
            {
                g.Id,
                TotalMatches = g.Matches.Count(m => m.Stage == MatchStage.GroupStage),
                CompletedMatches = g.Matches.Count(m => m.Stage == MatchStage.GroupStage && m.Status == MatchStatus.Completed),
                NextKickoff = g.Matches
                    .Where(m => m.Stage == MatchStage.GroupStage && m.Status != MatchStatus.Completed)
                    .OrderBy(m => m.KickoffTime)
                    .Select(m => (DateTime?)m.KickoffTime)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var progressByGroup = progressRows.ToDictionary(row => row.Id);
        var groups = standings.Select(standing =>
        {
            progressByGroup.TryGetValue(standing.GroupId, out var progress);

            return new AdminGroupOverviewViewModel
            {
                GroupId = standing.GroupId,
                GroupName = standing.GroupName,
                Teams = standing.Teams,
                TotalMatches = progress?.TotalMatches ?? 0,
                CompletedMatches = progress?.CompletedMatches ?? 0,
                NextKickoff = progress?.NextKickoff
            };
        }).ToList();

        var knockoutAlreadyGenerated = await _context.Matches.AnyAsync(m =>
            m.TournamentId == tournament.Id && m.Stage == MatchStage.RoundOf16);

        var viewModel = new AdminGroupsIndexViewModel
        {
            TournamentName = tournament.Name,
            Season = tournament.Season,
            TotalGroupMatches = groups.Sum(g => g.TotalMatches),
            CompletedGroupMatches = groups.Sum(g => g.CompletedMatches),
            CompletedGroups = groups.Count(g => g.IsComplete),
            KnockoutAlreadyGenerated = knockoutAlreadyGenerated,
            Groups = groups
        };

        return View(viewModel);
    }
}
