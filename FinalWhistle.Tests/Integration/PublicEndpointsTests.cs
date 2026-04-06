using FinalWhistle.Domain.Entities;
using FinalWhistle.Domain.Enums;
using FinalWhistle.Infrastructure.Data;
using FinalWhistle.Models;
using FinalWhistle.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Net;
using Xunit;

namespace FinalWhistle.Tests.Integration;

public class PublicEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly InMemoryDatabaseRoot DatabaseRoot = new();
    private readonly WebApplicationFactory<Program> _factory;

    public PublicEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging => logging.ClearProviders());

                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb", DatabaseRoot);
                });
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.RemoveAll<ICountryMetadataService>();
                services.AddSingleton<ICountryMetadataService>(new StubCountryMetadataService());

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
                SeedTestData(db);
            });
        });
    }

    private void SeedTestData(ApplicationDbContext context)
    {
        if (context.Tournaments.Any()) return;

        var tournament = new Tournament { Id = 1, Name = "Test Cup", Season = 2026, Status = TournamentStatus.Live, CreatedAt = DateTime.UtcNow };
        var group = new Group { Id = 1, TournamentId = 1, Name = "Group A", DisplayOrder = 1 };
        var team1 = new Team { Id = 1, Name = "Team A", Slug = "team-a", GroupId = 1, CreatedAt = DateTime.UtcNow };
        var team2 = new Team { Id = 2, Name = "Team B", Slug = "team-b", GroupId = 1, CreatedAt = DateTime.UtcNow };
        var completedMatch = new Match
        {
            Id = 1,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            GroupId = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            KickoffTime = DateTime.UtcNow.AddDays(-1),
            Status = MatchStatus.Completed,
            IsLockedForPredictions = true
        };
        var scheduledMatch = new Match
        {
            Id = 2,
            TournamentId = 1,
            Stage = MatchStage.GroupStage,
            GroupId = 1,
            HomeTeamId = 2,
            AwayTeamId = 1,
            KickoffTime = DateTime.UtcNow.AddDays(1),
            Status = MatchStatus.Scheduled,
            IsLockedForPredictions = false
        };
        var result = new MatchResult
        {
            Id = 1,
            MatchId = 1,
            HomeGoals = 2,
            AwayGoals = 1,
            EnteredAt = DateTime.UtcNow,
            RevisionNumber = 1
        };

        context.Tournaments.Add(tournament);
        context.Groups.Add(group);
        context.Teams.AddRange(team1, team2);
        context.Matches.AddRange(completedMatch, scheduledMatch);
        context.MatchResults.Add(result);
        context.SaveChanges();
    }

    [Fact]
    public async Task GetHome_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPrivacy_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Home/Privacy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLogin_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRegister_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Account/Register");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTeams_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Teams");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Team A", content);
        Assert.Contains("Mock Capital", content);
    }

    [Fact]
    public async Task GetGroups_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Groups");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Group A", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetMatches_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Matches");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMatches_WithGroupFilter_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Matches?groupId=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Team A", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetBracket_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Bracket");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Leaderboard");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTeamDetails_WithValidSlug_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Teams/team-a");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Country Snapshot", content);
        Assert.Contains("Mock Capital", content);
    }

    [Fact]
    public async Task GetTeamDetails_WithInvalidSlug_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Teams/nonexistent-team");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPredictions_WithoutAuth_ReturnsRedirect()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Predictions");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task GetDashboard_WithoutAuth_ReturnsRedirect()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Dashboard");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task GetAdminGroups_WithoutAuth_ReturnsRedirect()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Admin/Groups");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task GetAdminBracket_WithoutAuth_ReturnsRedirect()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Admin/Bracket");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
    }

    private sealed class StubCountryMetadataService : ICountryMetadataService
    {
        public Task<CountryProfile?> GetCountryProfileAsync(string teamName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CountryProfile?>(new CountryProfile
            {
                CommonName = teamName,
                Capital = "Mock Capital",
                Region = "Mock Region",
                FlagSvgUrl = "https://flagcdn.com/mock.svg",
                FlagPngUrl = "https://flagcdn.com/mock.png",
                GoogleMapsUrl = "https://maps.example/mock",
                OpenStreetMapsUrl = "https://osm.example/mock",
                Timezones = new List<string> { "UTC+00:00" }
            });
        }
    }
}
