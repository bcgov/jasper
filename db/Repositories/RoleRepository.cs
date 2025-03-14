using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Repositories;

public class RoleRepository(JasperDbContext context) : RepositoryBase<Role>(context)
{
}
