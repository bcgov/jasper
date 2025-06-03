using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Repositories;

public interface IJudicialBinderRepository : IRepositoryBase<JudicialBinder> { }

public class JudicialBinderRepository(JasperDbContext context) : RepositoryBase<JudicialBinder>(context), IJudicialBinderRepository
{
    public override async Task<IEnumerable<JudicialBinder>> FindAsync(Expression<Func<JudicialBinder, bool>> predicate)
    {
        var result = await base.FindAsync(predicate);

        foreach (var item in result)
        {
            item.Documents.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        return result;
    }
}
