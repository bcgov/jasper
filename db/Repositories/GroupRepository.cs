using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Repositories;

public class GroupRepository(JasperDbContext context) : RepositoryBase<Group>(context)
{
}
