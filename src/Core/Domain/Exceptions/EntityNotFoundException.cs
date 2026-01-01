namespace Mootable.Domain.Exceptions;

public sealed class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object Key { get; }
    
    public EntityNotFoundException(string entityName, object key) 
        : base($"Entity '{entityName}' with key '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }
}
