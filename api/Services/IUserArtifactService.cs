using System.Collections.Generic;
using System.Threading.Tasks;
using Scv.Core.Infrastructure;
using Scv.Models.UserArtifacts;

namespace Scv.Api.Services;

public interface IUserArtifactService : ICrudService<UserArtifactDto>
{
    Task<UserArtifactDto> GetByIdAsync(string id, bool includeDeleted = false);
    Task<OperationResult<IEnumerable<UserArtifactDto>>> SearchAsync(SearchArtifactsCriteria criteria = null);
}
