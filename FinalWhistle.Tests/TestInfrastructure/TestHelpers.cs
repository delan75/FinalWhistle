using System.Security.Claims;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinalWhistle.Tests.TestInfrastructure;

public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}

public static class ControllerTestHelpers
{
    public static ControllerContext CreateControllerContext(string? userId = null, string traceIdentifier = "trace-id")
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = traceIdentifier
        };

        if (!string.IsNullOrWhiteSpace(userId))
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userId)
                },
                authenticationType: "Test"));
        }

        return new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    public static TempDataDictionary CreateTempData(HttpContext httpContext)
    {
        return new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }

    public static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        return new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}
