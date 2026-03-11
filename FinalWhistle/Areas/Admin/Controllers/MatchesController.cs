using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MatchesController : Controller
{
    private readonly ApplicationDbContext _context;

    public MatchesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(MatchStage? stage)
    {
        var query = _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Group)
            .Include(m => m.Result)
            .AsQueryable();

        if (stage.HasValue)
        {
            query = query.Where(m => m.Stage == stage.Value);
        }

        var matches = await query.OrderBy(m => m.KickoffTime).ToListAsync();
        ViewBag.Stages = new SelectList(Enum.GetValues(typeof(MatchStage)), stage);
        
        return View(matches);
    }

    public async Task<IActionResult> Create()
    {
        var tournament = await _context.Tournaments.FirstOrDefaultAsync();
        if (tournament == null)
        {
            TempData["Error"] = "No tournament found. Please create a tournament first.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.TournamentId = tournament.Id;
        ViewBag.Teams = new SelectList(await _context.Teams.OrderBy(t => t.Name).ToListAsync(), "Id", "Name");
        ViewBag.Groups = new SelectList(await _context.Groups.OrderBy(g => g.DisplayOrder).ToListAsync(), "Id", "Name");
        ViewBag.Stages = new SelectList(Enum.GetValues(typeof(MatchStage)));
        
        return View(new Match { TournamentId = tournament.Id, KickoffTime = DateTime.Now.AddDays(7) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Match match)
    {
        ModelState.Remove("Tournament");
        ModelState.Remove("HomeTeam");
        ModelState.Remove("AwayTeam");
        ModelState.Remove("Group");
        ModelState.Remove("Result");
        
        if (match.HomeTeamId == match.AwayTeamId)
        {
            ModelState.AddModelError("", "Home team and away team cannot be the same.");
        }

        if (ModelState.IsValid)
        {
            match.Status = MatchStatus.Scheduled;
            match.IsLockedForPredictions = false;
            _context.Matches.Add(match);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        var tournament = await _context.Tournaments.FirstOrDefaultAsync();
        ViewBag.TournamentId = tournament?.Id;
        ViewBag.Teams = new SelectList(await _context.Teams.OrderBy(t => t.Name).ToListAsync(), "Id", "Name");
        ViewBag.Groups = new SelectList(await _context.Groups.OrderBy(g => g.DisplayOrder).ToListAsync(), "Id", "Name");
        ViewBag.Stages = new SelectList(Enum.GetValues(typeof(MatchStage)));
        return View(match);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var match = await _context.Matches.FindAsync(id);
        if (match == null) return NotFound();

        ViewBag.Teams = new SelectList(await _context.Teams.OrderBy(t => t.Name).ToListAsync(), "Id", "Name");
        ViewBag.Groups = new SelectList(await _context.Groups.OrderBy(g => g.DisplayOrder).ToListAsync(), "Id", "Name");
        ViewBag.Stages = new SelectList(Enum.GetValues(typeof(MatchStage)));
        ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(MatchStatus)));
        return View(match);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Match match)
    {
        if (id != match.Id) return NotFound();

        ModelState.Remove("Tournament");
        ModelState.Remove("HomeTeam");
        ModelState.Remove("AwayTeam");
        ModelState.Remove("Group");
        ModelState.Remove("Result");

        if (match.HomeTeamId == match.AwayTeamId)
        {
            ModelState.AddModelError("", "Home team and away team cannot be the same.");
        }

        if (ModelState.IsValid)
        {
            _context.Update(match);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Teams = new SelectList(await _context.Teams.OrderBy(t => t.Name).ToListAsync(), "Id", "Name");
        ViewBag.Groups = new SelectList(await _context.Groups.OrderBy(g => g.DisplayOrder).ToListAsync(), "Id", "Name");
        ViewBag.Stages = new SelectList(Enum.GetValues(typeof(MatchStage)));
        ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(MatchStatus)));
        return View(match);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var match = await _context.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (match == null) return NotFound();
        return View(match);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var match = await _context.Matches.FindAsync(id);
        if (match != null)
        {
            _context.Matches.Remove(match);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
