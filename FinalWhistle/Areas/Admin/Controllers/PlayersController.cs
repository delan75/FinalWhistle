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
public class PlayersController : Controller
{
    private readonly ApplicationDbContext _context;

    public PlayersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? teamId)
    {
        var query = _context.Players.Include(p => p.Team).AsQueryable();
        
        if (teamId.HasValue)
        {
            query = query.Where(p => p.TeamId == teamId.Value);
            ViewBag.SelectedTeam = await _context.Teams.FindAsync(teamId.Value);
        }

        var players = await query.OrderBy(p => p.Team.Name).ThenBy(p => p.JerseyNumber).ToListAsync();
        ViewBag.Teams = new SelectList(await _context.Teams.OrderBy(t => t.Name).ToListAsync(), "Id", "Name", teamId);
        
        return View(players);
    }

    public async Task<IActionResult> Create(int? teamId)
    {
        ViewBag.Teams = new SelectList(await _context.Teams.OrderBy(t => t.Name).ToListAsync(), "Id", "Name", teamId);
        ViewBag.Positions = new SelectList(Enum.GetValues(typeof(PlayerPosition)));
        
        var player = new Player();
        if (teamId.HasValue)
        {
            player.TeamId = teamId.Value;
        }
        
        return View(player);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Player player)
    {
        if (ModelState.IsValid)
        {
            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { teamId = player.TeamId });
        }
        
        ViewBag.Teams = new SelectList(await _context.Teams.OrderBy(t => t.Name).ToListAsync(), "Id", "Name", player.TeamId);
        ViewBag.Positions = new SelectList(Enum.GetValues(typeof(PlayerPosition)));
        return View(player);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var player = await _context.Players.FindAsync(id);
        if (player == null) return NotFound();
        
        ViewBag.Teams = new SelectList(await _context.Teams.OrderBy(t => t.Name).ToListAsync(), "Id", "Name", player.TeamId);
        ViewBag.Positions = new SelectList(Enum.GetValues(typeof(PlayerPosition)));
        return View(player);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Player player)
    {
        if (id != player.Id) return NotFound();
        
        if (ModelState.IsValid)
        {
            _context.Update(player);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { teamId = player.TeamId });
        }
        
        ViewBag.Teams = new SelectList(await _context.Teams.OrderBy(t => t.Name).ToListAsync(), "Id", "Name", player.TeamId);
        ViewBag.Positions = new SelectList(Enum.GetValues(typeof(PlayerPosition)));
        return View(player);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var player = await _context.Players.Include(p => p.Team).FirstOrDefaultAsync(p => p.Id == id);
        if (player == null) return NotFound();
        return View(player);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var player = await _context.Players.FindAsync(id);
        if (player != null)
        {
            var teamId = player.TeamId;
            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { teamId });
        }
        return RedirectToAction(nameof(Index));
    }
}
