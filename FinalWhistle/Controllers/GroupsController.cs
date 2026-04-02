using FinalWhistle.Application.Services;
using FinalWhistle.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Controllers;

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
        return View(standings);
    }
}
