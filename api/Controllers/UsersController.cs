using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Validation;
using Scv.Api.Models;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Models.AccessControlManagement;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class UsersController(
    IUserService userService,
    IValidator<UserDto> validator,
    IValidator<ReleaseNotesViewedRequestDto> releaseNotesViewedRequestValidator,
    IValidator<FileUploadRequest> fileUploadRequestValidator,
    ILogger<UsersController> logger,
    IAntiVirusService antiVirusService
) : AccessControlManagementControllerBase<IUserService, UserDto>(userService, validator)
{
    private readonly IValidator<ReleaseNotesViewedRequestDto> _releaseNotesViewedRequestValidator = releaseNotesViewedRequestValidator;
    private readonly IValidator<FileUploadRequest> _fileUploadRequestValidator = fileUploadRequestValidator;
    private readonly ILogger<UsersController> _logger = logger;
    private readonly IAntiVirusService _antiVirusService = antiVirusService;

    /// <summary>
    /// Get all active users
    /// </summary>
    /// <returns>Active users</returns>
    [HttpGet]
    [RequiresPermission(permissions: Permission.LOCK_UNLOCK_USERS)]
    public override Task<IActionResult> GetAll()
    {
        return base.GetAll();
    }

    /// <summary>
    /// Get the user information for the currently logged-in user.
    /// </summary>
    /// <returns>Active users</returns>
    [HttpGet]
    [Route("me")]
    public async Task<IActionResult> GetMyUser()
    {
        var userId = User.UserId();
        _logger.LogInformation("User Id {UserId}, returning their own user information", userId);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("Invalid user. Please contact the JASPER admin.");
        }
        var user = await base.Service.GetByIdWithPermissionsAsync(User.UserId());

        if (user != null)
        {
            return Ok(user);
        }
        return NotFound("Unable to locate JASPER user. Please contact the JASPER admin.");
    }

    /// <summary>
    /// Marks release notes as viewed for the currently logged-in user using the server dt/tm.
    /// </summary>
    [HttpPost]
    [Route("me/release-notes")]
    public async Task<IActionResult> MarkReleaseNotesViewed([FromBody] ReleaseNotesViewedRequestDto request)
    {
        var userId = User.UserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("Invalid user. Please contact the JASPER admin.");
        }

        var validationResult = await _releaseNotesViewedRequestValidator.ValidateAsync(request ?? new ReleaseNotesViewedRequestDto());
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage).FirstOrDefault());
        }

        var result = await base.Service.MarkReleaseNotesViewedAsync(userId, request?.Version, DateTime.UtcNow);
        if (!result.Succeeded)
        {
            var error = result.Errors.Count > 0 ? result.Errors[0] : null;
            if (!string.IsNullOrEmpty(error) && string.Equals(error, "User not found.", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(error);
            }

            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    /// <summary>
    /// Allows a new user without authorization to JASPER to request access to the application.
    /// </summary>
    /// <returns>The user resulting from the access request.</returns>
    [HttpPut]
    [Route("request-access")]
    public async Task<IActionResult> RequestAccess()
    {
        var userId = User.UserId();
        _logger.LogInformation("User Id {UserId}, requested access", userId);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("Invalid user. Please contact the JASPER admin.");
        }

        var existingUserResponse = await base.GetById(User.UserId());

        var email = User.Email();
        if (existingUserResponse is OkObjectResult okResult)
        {
            var existingUser = (UserDto)okResult.Value;
            if (existingUser != null)
            {
                if (email != existingUser.Email)
                {
                    existingUser.Email = email;
                }
                existingUser.IsPendingRegistration = true;
                var result = await base.Update(User.UserId(), existingUser);

                return result;
            }
            else
            {
                return NotFound("Unable to locate JASPER user. Please contact the JASPER admin.");
            }
        }
        else
        {
            return existingUserResponse;
        }
    }

    /// <summary>
    /// Uploads a signature image for the user with the specified ID. The image is scanned for viruses before being stored.
    /// </summary>
    /// <param name="id">The ID of the user.</param>
    /// <param name="request">The file upload request containing the signature image.</param>
    /// <returns>The result of the upload operation.</returns>
    [HttpPost]
    [Route("{id}/signature")]
    [RequiresPermission(permissions: Permission.LOCK_UNLOCK_USERS)]
    public Task<IActionResult> UploadSignature([FromRoute, ObjectId] string id, [FromForm] FileUploadRequest request)
        => UploadImageAsync(request, file => base.Service.UploadSignatureAsync(id, file));

    /// <summary>
    /// Uploads initials image for the user with the specified ID. The image is scanned for viruses before being stored.
    /// </summary>
    /// <param name="id">The ID of the user.</param>
    /// <param name="request">The file upload request containing the initials image.</param>
    /// <returns>The result of the upload operation.</returns>
    [HttpPost]
    [Route("{id}/initials")]
    [RequiresPermission(permissions: Permission.LOCK_UNLOCK_USERS)]
    public Task<IActionResult> UploadInitials([FromRoute, ObjectId] string id, [FromForm] FileUploadRequest request)
        => UploadImageAsync(request, file => base.Service.UploadInitialsAsync(id, file));

    private async Task<IActionResult> UploadImageAsync(
        FileUploadRequest request,
        Func<byte[], Task<OperationResult>> uploadAsync)
    {
        if (request == null)
        {
            return BadRequest("No file was uploaded.");
        }

        var validationResult = await _fileUploadRequestValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage).FirstOrDefault());
        }

        var file = request.File;
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        memoryStream.Position = 0;
        var (isClean, message) = await _antiVirusService.ScanAsync(memoryStream);
        if (!isClean)
        {
            _logger.LogWarning("The uploaded file failed the antivirus scan: {Message}", message);
            return BadRequest("The uploaded file failed the antivirus scan.");
        }

        var result = await uploadAsync(memoryStream.ToArray());
        if (!result.Succeeded)
        {
            var error = result.Errors.Count > 0 ? result.Errors[0] : null;
            if (!string.IsNullOrEmpty(error) && string.Equals(error, "User not found.", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(error);
            }

            return BadRequest(result.Errors);
        }

        return Ok(result);
    }
}
