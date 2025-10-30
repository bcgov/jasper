using System;
using System.IO;
using JCCommon.Clients.FileServices;
using Moq;

namespace tests.api.Fixtures;

/// <summary>
/// Extension methods for setting up common FilesService mock behaviors.
/// </summary>
public static class FilesServiceBuilder
{
    public static FilesServiceFixture SetupDocumentAsync(
        this FilesServiceFixture fixture,
        FileResponse response = null)
    {
        fixture.MockFilesService
            .Setup(f => f.DocumentAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync(response ?? new FileResponse(200, null, new MemoryStream(), null, null));

        return fixture;
    }

    public static FilesServiceFixture SetupDocumentAsyncWithException(
        this FilesServiceFixture fixture,
        Exception exception)
    {
        fixture.MockFilesService
            .Setup(f => f.DocumentAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ThrowsAsync(exception);

        return fixture;
    }

    public static FilesServiceFixture VerifyDocumentAsyncCalled(
        this FilesServiceFixture fixture,
        Times times)
    {
        fixture.MockFilesService.Verify(
            f => f.DocumentAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()),
            times);

        return fixture;
    }
}