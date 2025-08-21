using Ensek.Controllers;
using Ensek.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ensek.Tests.Controllers;

public class MeterReadingsControllerTests
{
    [Fact]
    public async Task Upload_NoFile_ReturnsBadRequest()
    {
        var svc = new Mock<IMeterReadingService>();
        var controller = new MeterReadingsController(svc.Object);
    var result = await controller.Upload(null!, false, CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_Valid_ReturnsOk()
    {
        var svc = new Mock<IMeterReadingService>();
    svc.Setup(s => s.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MeterReadingUploadResult(1,0));
        var controller = new MeterReadingsController(svc.Object);

        var file = new FormFile(new System.IO.MemoryStream([1]), 0, 1, "file", "f.csv");
    var result = await controller.Upload(file, false, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ((MeterReadingUploadResult)ok!.Value!).SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task Upload_NonCsv_ReturnsBadRequest()
    {
        var svc = new Mock<IMeterReadingService>();
        var controller = new MeterReadingsController(svc.Object);

        var file = new FormFile(new System.IO.MemoryStream([1]), 0, 1, "file", "not-csv.txt");
    var result = await controller.Upload(file, false, CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
