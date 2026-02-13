using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RdtClient.Data.Models.Data;
using RdtClient.Service.Services;
using RdtClient.Web.Controllers;

namespace RdtClient.Web.Test.Controllers;

public class TorrentsControllerNzbTest
{
    private readonly Mock<Torrents> _torrentsMock;
    private readonly Mock<ILogger<TorrentsController>> _loggerMock;
    private readonly TorrentsController _controller;

    public TorrentsControllerNzbTest()
    {
        _torrentsMock = new Mock<Torrents>(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
        _loggerMock = new Mock<ILogger<TorrentsController>>();
        _controller = new TorrentsController(_loggerMock.Object, _torrentsMock.Object, null!);
    }

    [Fact]
    public async Task UploadNzbLink_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new TorrentControllerUploadNzbLinkRequest
        {
            NzbLink = "http://example.com/test.nzb",
            Torrent = new Torrent()
        };

        // Act
        var result = await _controller.UploadNzbLink(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _torrentsMock.Verify(t => t.AddNzbLinkToDebridQueue(request.NzbLink, request.Torrent), Times.Once);
    }

    [Fact]
    public async Task UploadNzbLink_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadNzbLink(null);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task UploadNzbLink_EmptyLink_ReturnsBadRequest()
    {
        // Arrange
        var request = new TorrentControllerUploadNzbLinkRequest
        {
            NzbLink = "",
            Torrent = new Torrent()
        };

        // Act
        var result = await _controller.UploadNzbLink(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid nzb link", badRequest.Value);
    }

    [Fact]
    public async Task UploadNzbFile_ValidRequest_ReturnsOk()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "nzb content";
        var fileName = "test.nzb";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);

        var formData = new TorrentControllerUploadFileRequest
        {
            Torrent = new Torrent()
        };

        // Act
        var result = await _controller.UploadNzbFile(fileMock.Object, formData);

        // Assert
        Assert.IsType<OkResult>(result);
        _torrentsMock.Verify(t => t.AddNzbFileToDebridQueue(It.IsAny<Byte[]>(), fileName, formData.Torrent), Times.Once);
        Assert.Equal(fileName, formData.Torrent.RdName);
    }

    [Fact]
    public async Task UploadNzbFile_NoFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadNzbFile(null, new TorrentControllerUploadFileRequest());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid nzb file", badRequest.Value);
    }
}
