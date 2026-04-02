using FinalWhistle.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Controllers;

public class TeamsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TeamsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var teams = await _context.Teams
            .Include(t => t.Group)
            .OrderBy(t => t.Group!.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return View(teams);
    }

    public async Task<IActionResult> Details(string slug)
    {
        var team = await _context.Teams
            .Include(t => t.Group)
            .Include(t => t.Players.OrderBy(p => p.JerseyNumber))
            .Include(t => t.HomeMatches.Where(m => m.Status == Domain.Enums.MatchStatus.Completed))
                .ThenInclude(m => m.AwayTeam)
            .Include(t => t.HomeMatches.Where(m => m.Status == Domain.Enums.MatchStatus.Completed))
                .ThenInclude(m => m.Result)
            .Include(t => t.AwayMatches.Where(m => m.Status == Domain.Enums.MatchStatus.Completed))
                .ThenInclude(m => m.HomeTeam)
            .Include(t => t.AwayMatches.Where(m => m.Status == Domain.Enums.MatchStatus.Completed))
                .ThenInclude(m => m.Result)
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (team == null) return NotFound();

        return View(team);
    }
}
