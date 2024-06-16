using Aeon.Domain.Entities;
using Aeon.Domain.Repositories;
using Aeon.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aeon.Infrastructure.Repositories;

public class Repository : IRepository
{
    private readonly ILogger<Repository> _logger;
    private readonly AppDbContext _appDbContext;

    public Repository(ILogger<Repository> logger, AppDbContext appDbContext)
    {
        _logger = logger;
        _appDbContext = appDbContext;
    }

    public async Task<List<Produto>> Buscar(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"{nameof(Buscar)}");
        return await _appDbContext.Products.ToListAsync(cancellationToken);
    }

    public async Task<Produto> Inserir(Produto produto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"{nameof(Inserir)}: {produto.ToString()}");
        await _appDbContext.Products.AddAsync(produto, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        return produto;
    }
}
