using Aeon.Domain.Entities;

namespace Aeon.Domain.Repositories;

public interface IRepository
{
    Task<Produto> Inserir(Produto produto, CancellationToken cancellationToken = default);
    Task<List<Produto>> Buscar(CancellationToken cancellationToken = default);
}
