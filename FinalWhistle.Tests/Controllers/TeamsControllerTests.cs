using FinalWhistle.Controllers;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Models;
using FinalWhistle.Services;
using FinalWhistle.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;
using DomainMatch = FinalWhistle.Domain.Entities.Match;

namespace FinalWhistle.Tests.Controllers;

public class TeamsControllerTests
{
    [Fact]
    public async Task Index_UsesCountryServiceFlagFallback_WhenStoredFlagIsMissing()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Groups.Add(new Group { Id = 1, TournamentId = 1, Name = "A", DisplayOrder = 1 });
        context.Teams.Add(new Team
        {
            Id = 1,
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

        var controller = new TeamsController(context, countryService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext()
        };

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<TeamListItemViewModel>>(view.Model);
        var item = Assert.Single(model);
        Assert.Equal("https://flagcdn.com/br.svg", item.DisplayFlagUrl);
        Assert.Equal("Brasilia", item.CountryProfile?.Capital);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenTeamDoesNotExist()
    {
        using var context = TestDbContextFactory.CreateContext();
        var countryService = new Mock<ICountryMetadataService>();
        var controller = new TeamsController(context, countryService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext()
        };

        var result = await controller.Details("missing-team");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsViewModel_WithCountryMetadata()
    {
        using var context = TestDbContextFactory.CreateContext();
        context.Groups.Add(new Group { Id = 1, TournamentId = 1, Name = "A", DisplayOrder = 1 });
        context.Teams.Add(new Team
        {
            Id = 1,
            Name = "Brazil",
            Slug = "brazil",
            GroupId = 1,
            FlagUrl = string.Empty,
            CreatedAt = DateTime.UtcNow
        });
        context.Matches.Add(new DomainMatch
        {
            Id = 10,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            HomeTeamId = 1,
            AwayTeamId = 2,
            KickoffTime = DateTime.UtcNow.AddDays(-1),
            Status = MatchStatus.Completed,
            IsLockedForPredictions = true
        });
        context.MatchResults.Add(new MatchResult
        {
            MatchId = 10,
            HomeGoals = 2,
            AwayGoals = 1,
            EnteredAt = DateTime.UtcNow,
            RevisionNumber = 1
        });
        await context.SaveChangesAsync();

        var countryService = new Mock<ICountryMetadataService>();
        countryService
            .Setup(service => service.GetCountryProfileAsync("Brazil", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryProfile
            {
                CommonName = "Brazil",
                Capital = "Brasilia",
                Region = "Americas",
                FlagSvgUrl = "https://flagcdn.com/br.svg"
            });

        var controller = new TeamsController(context, countryService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext()
        };

        var result = await controller.Details("brazil");

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TeamDetailsViewModel>(view.Model);
        Assert.Equal("Brazil", model.Team.Name);
        Assert.Equal("Brasilia", model.CountryProfile?.Capital);
        Assert.Equal("https://flagcdn.com/br.svg", model.DisplayFlagUrl);
    }
}
