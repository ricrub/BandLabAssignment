namespace Common.Exceptions;

public class EntityNotFoundException : Exception
{
    public string EntityName { get; }
    public object Key { get; }

    public EntityNotFoundException(string entityName)
        : base($"Entity \"{entityName}\" was not found.")
    {
        EntityName = entityName;
    }

    public EntityNotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" ({key}) was not found.")
    {
        EntityName = entityName;
        Key = key;
    }
}