using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LazyCache;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure;
using Scv.Api.Models.AccessControlManagement;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public class UserService(
    IAppCache cache,
    IMapper mapper,
    ILogger<UserService> logger,
    IRepositoryBase<User> userRepo,
    IRepositoryBase<Group> groupRepo
) : AccessControlManagementServiceBase<IRepositoryBase<User>, User, UserDto>(
        cache,
        mapper,
        logger,
        userRepo)
{
    private readonly IRepositoryBase<Group> _groupRepo = groupRepo;

    public override string CacheName => "GetUsersAsync";

    public override async Task<OperationResult<UserDto>> ValidateAsync(UserDto dto, bool isEdit = false)
    {
        var errors = new List<string>();

        var userNotExist = await this.Repo.GetByIdAsync(dto.Id);
        if (isEdit && userNotExist == null)
        {
            errors.Add("User ID is not found.");
        }

        // Check if email is already used when editing
        var userEmailAlreadyUsed = await this.Repo.FindAsync(u => u.Email == dto.Email);
        if (isEdit && userEmailAlreadyUsed.Any())
        {
            errors.Add("Email address is already used.");
        }

        // Check if email is already used when adding
        if (!isEdit && userEmailAlreadyUsed.Any() && userEmailAlreadyUsed.Single().Id != dto.Id)
        {
            errors.Add("Email address is already used.");
        }

        // Check if group ids are all valid
        var existingGroupIds = (await _groupRepo.GetAllAsync()).Select(p => p.Id);
        if (!dto.GroupIds.All(id => existingGroupIds.Contains(id)))
        {
            errors.Add("Found one or more invalid group IDs.");
        }

        return errors.Count != 0
            ? OperationResult<UserDto>.Failure([.. errors])
            : OperationResult<UserDto>.Success(dto);
    }
}
