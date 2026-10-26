using System.Threading.Tasks;
using Scv.Models.UserArtifacts;

namespace Scv.Api.Services;

public interface IUserArtifactService : ICrudService<UserArtifactDto>
{
    Task<UserArtifactDto> GetByIdAsync(string id, bool includeDeleted = false);
}
