namespace Aeon.Domain.Events;

public record ProdutoCompradoAtivadoEvent
{
    public int Id { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public decimal Price { get; init; }
    public bool isActive { get; init; }
}