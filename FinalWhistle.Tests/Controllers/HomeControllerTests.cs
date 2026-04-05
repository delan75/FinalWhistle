using FinalWhistle.Controllers;
using FinalWhistle.Models;
using FinalWhistle.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinalWhistle.Tests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void Error_ReturnsViewModelWithTraceIdentifier()
    {
        var controller = new HomeController(new NullLogger<HomeController>())
        {
            ControllerContext = ControllerTestHelpers.CreateControllerContext(traceIdentifier: "trace-123")
        };

        var result = controller.Error();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(view.Model);
        Assert.Equal("trace-123", model.RequestId);
    }
}
