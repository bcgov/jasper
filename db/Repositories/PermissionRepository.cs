using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Repositories;

public interface IPermissionRepository : IRepositoryBase<Permission>
{
    Task<IEnumerable<Permission>> GetActivePermissionsAsync();
}

public class PermissionRepository(JasperDbContext context) : RepositoryBase<Permission>(context), IPermissionRepository
{
    public async Task<IEnumerable<Permission>> GetActivePermissionsAsync()
    {
        return await _dbSet.Where(p => p.IsActive).ToListAsync();
    }
}
