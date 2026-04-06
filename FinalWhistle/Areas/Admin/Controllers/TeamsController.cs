using FinalWhistle.Areas.Admin.Models;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Infrastructure.Data;
using FinalWhistle.Services;
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
    private readonly ICountryMetadataService _countryMetadataService;

    public TeamsController(ApplicationDbContext context, ICountryMetadataService countryMetadataService)
    {
        _context = context;
        _countryMetadataService = countryMetadataService;
    }

    public async Task<IActionResult> Index()
    {
        var teams = await _context.Teams.Include(t => t.Group).OrderBy(t => t.Name).ToListAsync();
        return View(teams);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateGroupsAsync();
        return View(new AdminTeamEditorViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminTeamEditorViewModel model)
    {
        if (ModelState.IsValid)
        {
            var team = model.ToTeam();
            team.Slug = BuildSlug(team.Name);
            team.CreatedAt = DateTime.UtcNow;
            await ApplySuggestedFlagAsync(team, GetRequestAborted());
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateGroupsAsync(model.GroupId);
        model.CountryPreview = await BuildCountryPreviewAsync(model.Name, model.FlagUrl, GetRequestAborted());
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team == null) return NotFound();

        await PopulateGroupsAsync(team.GroupId);
        return View(await BuildEditorViewModelAsync(team, GetRequestAborted()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminTeamEditorViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (ModelState.IsValid)
        {
            var existingTeam = await _context.Teams.FindAsync(id);
            if (existingTeam == null) return NotFound();

            existingTeam.Name = model.Name?.Trim() ?? string.Empty;
            existingTeam.Slug = BuildSlug(existingTeam.Name);
            existingTeam.FlagUrl = model.FlagUrl?.Trim() ?? string.Empty;
            existingTeam.GroupId = model.GroupId;
            existingTeam.IsVerified = model.IsVerified;
            existingTeam.CreatedAt = model.CreatedAt == default ? existingTeam.CreatedAt : model.CreatedAt;

            await ApplySuggestedFlagAsync(existingTeam, GetRequestAborted());
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        await PopulateGroupsAsync(model.GroupId);
        model.CountryPreview = await BuildCountryPreviewAsync(model.Name, model.FlagUrl, GetRequestAborted());
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CountryPreview(string teamName, string? currentFlagUrl)
    {
        var preview = await BuildCountryPreviewAsync(teamName, currentFlagUrl, GetRequestAborted());
        return PartialView("_CountryMetadataPreview", preview);
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

    private async Task PopulateGroupsAsync(int? selectedGroupId = null)
    {
        ViewBag.Groups = new SelectList(
            await _context.Groups.OrderBy(g => g.DisplayOrder).ToListAsync(),
            "Id",
            "Name",
            selectedGroupId);
    }

    private async Task<AdminTeamEditorViewModel> BuildEditorViewModelAsync(Team team, CancellationToken cancellationToken)
    {
        var countryProfile = await _countryMetadataService.GetCountryProfileAsync(team.Name, cancellationToken);
        return AdminTeamEditorViewModel.FromTeam(team, countryProfile);
    }

    private async Task<AdminTeamCountryMetadataViewModel> BuildCountryPreviewAsync(
        string? teamName,
        string? currentFlagUrl,
        CancellationToken cancellationToken)
    {
        var countryProfile = await _countryMetadataService.GetCountryProfileAsync(teamName ?? string.Empty, cancellationToken);
        return AdminTeamCountryMetadataViewModel.From(teamName, currentFlagUrl, countryProfile);
    }

    private async Task ApplySuggestedFlagAsync(Team team, CancellationToken cancellationToken)
    {
        team.FlagUrl = team.FlagUrl.Trim();
        if (!string.IsNullOrWhiteSpace(team.FlagUrl))
        {
            return;
        }

        var countryProfile = await _countryMetadataService.GetCountryProfileAsync(team.Name, cancellationToken);
        team.FlagUrl = AdminTeamCountryMetadataViewModel.From(team.Name, team.FlagUrl, countryProfile).SuggestedFlagUrl;
    }

    private static string BuildSlug(string teamName)
    {
        return string.Join(
            '-',
            (teamName ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private CancellationToken GetRequestAborted()
    {
        return HttpContext?.RequestAborted ?? CancellationToken.None;
    }
}
