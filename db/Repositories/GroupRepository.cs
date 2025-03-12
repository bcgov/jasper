using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Repositories;
public interface IGroupRepository : IRepositoryBase<Group>
{
}

public class GroupRepository(JasperDbContext context) : RepositoryBase<Group>(context), IGroupRepository
{
}
