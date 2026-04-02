using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Controllers;

public class MatchesController : Controller
{
    private readonly ApplicationDbContext _context;

    public MatchesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? groupId, MatchStage? stage, int? teamId)
    {
        var query = _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Group)
            .Include(m => m.Result)
            .AsQueryable();

        if (groupId.HasValue)
            query = query.Where(m => m.GroupId == groupId);

        if (stage.HasValue)
            query = query.Where(m => m.Stage == stage);

        if (teamId.HasValue)
            query = query.Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId);

        var matches = await query
            .OrderBy(m => m.KickoffTime)
            .ToListAsync();

        ViewBag.Groups = new SelectList(await _context.Groups.OrderBy(g => g.DisplayOrder).ToListAsync(), "Id", "Name");
        ViewBag.Teams = new SelectList(await _context.Teams.OrderBy(t => t.Name).ToListAsync(), "Id", "Name");
        ViewBag.SelectedGroupId = groupId;
        ViewBag.SelectedStage = stage;
        ViewBag.SelectedTeamId = teamId;

        return View(matches);
    }
}
