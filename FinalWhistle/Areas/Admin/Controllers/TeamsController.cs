using FinalWhistle.Domain.Entities;
using FinalWhistle.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TeamsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TeamsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var teams = await _context.Teams.Include(t => t.Group).OrderBy(t => t.Name).ToListAsync();
        return View(teams);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Groups = new SelectList(await _context.Groups.OrderBy(g => g.DisplayOrder).ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Team team)
    {
        if (ModelState.IsValid)
        {
            team.Slug = team.Name.ToLower().Replace(" ", "-");
            team.CreatedAt = DateTime.UtcNow;
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Groups = new SelectList(await _context.Groups.OrderBy(g => g.DisplayOrder).ToListAsync(), "Id", "Name", team.GroupId);
        return View(team);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team == null) return NotFound();
        ViewBag.Groups = new SelectList(await _context.Groups.OrderBy(g => g.DisplayOrder).ToListAsync(), "Id", "Name", team.GroupId);
        return View(team);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Team team)
    {
        if (id != team.Id) return NotFound();
        if (ModelState.IsValid)
        {
            team.Slug = team.Name.ToLower().Replace(" ", "-");
            _context.Update(team);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Groups = new SelectList(await _context.Groups.OrderBy(g => g.DisplayOrder).ToListAsync(), "Id", "Name", team.GroupId);
        return View(team);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var team = await _context.Teams.Include(t => t.Group).FirstOrDefaultAsync(t => t.Id == id);
        if (team == null) return NotFound();
        return View(team);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team != null)
        {
            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
