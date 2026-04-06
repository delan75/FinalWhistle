using FinalWhistle.Infrastructure.Data;
using FinalWhistle.Models;
using FinalWhistle.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalWhistle.Controllers;

public class TeamsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICountryMetadataService _countryMetadataService;

    public TeamsController(ApplicationDbContext context, ICountryMetadataService countryMetadataService)
    {
        _context = context;
        _countryMetadataService = countryMetadataService;
    }

    public async Task<IActionResult> Index()
    {
        var teams = await _context.Teams
            .Include(t => t.Group)
            .OrderBy(t => t.Group!.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();

        var teamItems = new List<TeamListItemViewModel>(teams.Count);
        foreach (var team in teams)
        {
            var countryProfile = await _countryMetadataService.GetCountryProfileAsync(team.Name, HttpContext.RequestAborted);
            teamItems.Add(new TeamListItemViewModel
            {
                Team = team,
                CountryProfile = countryProfile,
                DisplayFlagUrl = ResolveDisplayFlagUrl(team.FlagUrl, countryProfile)
            });
        }

        return View(teamItems);
    }

    [HttpGet("/Teams/{slug}")]
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

        var countryProfile = await _countryMetadataService.GetCountryProfileAsync(team.Name, HttpContext.RequestAborted);
        var viewModel = new TeamDetailsViewModel
        {
            Team = team,
            CountryProfile = countryProfile,
            DisplayFlagUrl = ResolveDisplayFlagUrl(team.FlagUrl, countryProfile)
        };

        return View(viewModel);
    }

    private static string ResolveDisplayFlagUrl(string? storedFlagUrl, CountryProfile? countryProfile)
    {
        if (!string.IsNullOrWhiteSpace(storedFlagUrl))
        {
            return storedFlagUrl;
        }

        if (!string.IsNullOrWhiteSpace(countryProfile?.FlagSvgUrl))
        {
            return countryProfile.FlagSvgUrl;
        }

        if (!string.IsNullOrWhiteSpace(countryProfile?.FlagPngUrl))
        {
            return countryProfile.FlagPngUrl;
        }

        return string.Empty;
    }
}
