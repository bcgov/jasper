using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Repositories;
public interface IRoleRepository : IRepositoryBase<Role>
{
}

public class RoleRepository(JasperDbContext context) : RepositoryBase<Role>(context), IRoleRepository
{
}
