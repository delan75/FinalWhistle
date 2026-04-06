using FinalWhistle.Areas.Admin.Models;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Models;
using FinalWhistle.Services;
using FinalWhistle.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;
using AdminTeamsController = FinalWhistle.Areas.Admin.Controllers.TeamsController;

namespace FinalWhistle.Tests.Controllers;

public class AdminTeamsControllerTests
{
    [Fact]
    public async Task Edit_Get_ReturnsViewModelWithCountryPreview()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Groups.Add(new Group { Id = 1, TournamentId = 1, Name = "A", DisplayOrder = 1 });
        context.Teams.Add(new Team
        {
            Id = 10,
            Name = "Brazil",
            Slug = "brazil",
            GroupId = 1,
            FlagUrl = string.Empty,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var countryService = new Mock<ICountryMetadataService>();
        countryService
            .Setup(service => service.GetCountryProfileAsync("Brazil", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryProfile
            {
                CommonName = "Brazil",
                Capital = "Brasilia",
                FlagSvgUrl = "https://flagcdn.com/br.svg"
            });

        var controller = new AdminTeamsController(context, countryService.Object);

        var result = await controller.Edit(10);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AdminTeamEditorViewModel>(view.Model);
        Assert.Equal("Brazil", model.Name);
        Assert.NotNull(model.CountryPreview.CountryProfile);
        Assert.Equal("Brasilia", model.CountryPreview.CountryProfile!.Capital);
        Assert.Equal("https://flagcdn.com/br.svg", model.CountryPreview.SuggestedFlagUrl);
    }

    [Fact]
    public async Task Create_Post_AutoFillsSuggestedFlag_WhenFlagUrlIsBlank()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Groups.Add(new Group { Id = 1, TournamentId = 1, Name = "A", DisplayOrder = 1 });
        await context.SaveChangesAsync();

        var countryService = new Mock<ICountryMetadataService>();
        countryService
            .Setup(service => service.GetCountryProfileAsync("Brazil", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryProfile
            {
                CommonName = "Brazil",
                FlagSvgUrl = "https://flagcdn.com/br.svg"
            });

        var controller = new AdminTeamsController(context, countryService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext()
        };

        var result = await controller.Create(new AdminTeamEditorViewModel
        {
            Name = "Brazil",
            FlagUrl = string.Empty,
            GroupId = 1,
            IsVerified = true
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminTeamsController.Index), redirect.ActionName);
        var savedTeam = Assert.Single(context.Teams);
        Assert.Equal("https://flagcdn.com/br.svg", savedTeam.FlagUrl);
        Assert.Equal("brazil", savedTeam.Slug);
    }

    [Fact]
    public async Task Create_Post_PreservesManualFlagUrl_WhenProvided()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Groups.Add(new Group { Id = 1, TournamentId = 1, Name = "A", DisplayOrder = 1 });
        await context.SaveChangesAsync();

        var countryService = new Mock<ICountryMetadataService>(MockBehavior.Strict);
        var controller = new AdminTeamsController(context, countryService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext()
        };

        var result = await controller.Create(new AdminTeamEditorViewModel
        {
            Name = "Brazil",
            FlagUrl = "https://cdn.example/custom-brazil.png",
            GroupId = 1
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminTeamsController.Index), redirect.ActionName);
        var savedTeam = Assert.Single(context.Teams);
        Assert.Equal("https://cdn.example/custom-brazil.png", savedTeam.FlagUrl);
        countryService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Edit_Post_FillsSuggestedFlag_WhenExistingFlagIsCleared()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Groups.Add(new Group { Id = 1, TournamentId = 1, Name = "A", DisplayOrder = 1 });
        context.Teams.Add(new Team
        {
            Id = 20,
            Name = "Brazil",
            Slug = "brazil",
            GroupId = 1,
            FlagUrl = "https://cdn.example/old.png",
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var countryService = new Mock<ICountryMetadataService>();
        countryService
            .Setup(service => service.GetCountryProfileAsync("Brazil", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryProfile
            {
                CommonName = "Brazil",
                FlagPngUrl = "https://flagcdn.com/w320/br.png"
            });

        var controller = new AdminTeamsController(context, countryService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext()
        };

        var result = await controller.Edit(20, new AdminTeamEditorViewModel
        {
            Id = 20,
            Name = "Brazil",
            FlagUrl = string.Empty,
            GroupId = 1,
            CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminTeamsController.Index), redirect.ActionName);
        var savedTeam = await context.Teams.FindAsync(20);
        Assert.NotNull(savedTeam);
        Assert.Equal("https://flagcdn.com/w320/br.png", savedTeam!.FlagUrl);
    }

    [Fact]
    public async Task CountryPreview_ReturnsPreviewPartialWithSuggestedFlag()
    {
        using var context = TestDbContextFactory.CreateContext();
        var countryService = new Mock<ICountryMetadataService>();
        countryService
            .Setup(service => service.GetCountryProfileAsync("Brazil", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryProfile
            {
                CommonName = "Brazil",
                Capital = "Brasilia",
                FlagSvgUrl = "https://flagcdn.com/br.svg"
            });

        var controller = new AdminTeamsController(context, countryService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext()
        };

        var result = await controller.CountryPreview("Brazil", string.Empty);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_CountryMetadataPreview", partial.ViewName);
        var model = Assert.IsType<AdminTeamCountryMetadataViewModel>(partial.Model);
        Assert.Equal("Brazil", model.TeamName);
        Assert.Equal("https://flagcdn.com/br.svg", model.SuggestedFlagUrl);
        Assert.Equal("Brasilia", model.CountryProfile?.Capital);
    }
}
