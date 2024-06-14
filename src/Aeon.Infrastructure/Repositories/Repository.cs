using Aeon.Domain.Entities;
using Aeon.Domain.Repositories;
using Aeon.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Aeon.Infrastructure.Repositories;

public class Repository : IRepository
{
    private readonly AppDbContext appDbContext;

    public Repository(AppDbContext appDbContext)
    {
        this.appDbContext = appDbContext;
    }

    public async Task<List<Produto>> Buscar(CancellationToken cancellationToken = default)
    {
        return await appDbContext.Products.ToListAsync(cancellationToken);
    }

    public async Task<Produto> Inserir(Produto produto, CancellationToken cancellationToken = default)
    {
        await appDbContext.Products.AddAsync(produto, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);
        return produto;
    }
}
