using FinalWhistle.Application.Services;
using FinalWhistle.Controllers;
using FinalWhistle.Domain.Entities;
using FinalWhistle.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinalWhistle.Tests.Controllers;

public class PredictionsControllerTests
{
    [Fact]
    public async Task Submit_SetsSuccessMessage_WhenPredictionIsSaved()
    {
        using var context = TestDbContextFactory.CreateContext();
        var predictionsService = new Mock<IPredictionsService>();
        predictionsService
            .Setup(s => s.SubmitPredictionAsync(1, "user1", 2, 1))
            .ReturnsAsync(true);

        var controller = new PredictionsController(context, predictionsService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext("user1")
        };
        controller.TempData = ControllerTestHelpers.CreateTempData(controller.HttpContext);

        var result = await controller.Submit(1, 2, 1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(PredictionsController.Index), redirect.ActionName);
        Assert.Equal("Prediction saved successfully!", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Submit_SetsErrorMessage_WhenPredictionFails()
    {
        using var context = TestDbContextFactory.CreateContext();
        var predictionsService = new Mock<IPredictionsService>();
        predictionsService
            .Setup(s => s.SubmitPredictionAsync(1, "user1", 9, 9))
            .ReturnsAsync(false);

        var controller = new PredictionsController(context, predictionsService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext("user1")
        };
        controller.TempData = ControllerTestHelpers.CreateTempData(controller.HttpContext);

        var result = await controller.Submit(1, 9, 9);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(PredictionsController.Index), redirect.ActionName);
        Assert.Equal("Failed to save prediction. Match may have started or is locked.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task MyPredictions_ReturnsViewWithPredictionHistory()
    {
        using var context = TestDbContextFactory.CreateContext();
        var predictions = new List<Prediction>
        {
            new() { Id = 1, MatchId = 1, UserId = "user1", PredictedHomeGoals = 1, PredictedAwayGoals = 0 }
        };
        var predictionsService = new Mock<IPredictionsService>();
        predictionsService.Setup(s => s.GetUserPredictionsAsync("user1")).ReturnsAsync(predictions);

        var controller = new PredictionsController(context, predictionsService.Object)
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext("user1")
        };

        var result = await controller.MyPredictions();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(predictions, view.Model);
    }
}
