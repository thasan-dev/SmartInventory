using TLS._Framework.Model.QueryModel;

namespace SmartInventory.Inventories.QueryModel;

public class PlantQueryModel : IQueryModel
{
    public Guid Id { get; set; }

    public string PlantName { get; set; } = null!;
}
