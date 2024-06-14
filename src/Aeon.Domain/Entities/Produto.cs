namespace Aeon.Domain.Entities;
public class Produto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public decimal Price { get; set; }
    public bool isActive { get; set; }
}
