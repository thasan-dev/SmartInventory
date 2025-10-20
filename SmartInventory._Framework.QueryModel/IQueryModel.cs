namespace TLS._Framework.Model.QueryModel;

/// <summary>
/// A QueryModel owned by a Provider.
/// </summary>
/// <remarks>By using this interface, the DefaultQueriesModelRepository could be used.</remarks>
public interface IQueryModel
{
    /// <summary>
    /// The id of the query model.
    /// </summary>
    Guid Id { get; }
}