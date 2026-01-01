namespace Mootable.Application.Features.MootTables.Constants;

public static class MootTableMessages
{
    public const string MootTableNotFound = "MootTable not found.";
    public const string MootTableArchived = "This MootTable is archived.";
    public const string CannotDeleteDefaultMootTable = "Cannot delete the default MootTable.";
    
    public static class Validation
    {
        public const string NameRequired = "MootTable name is required.";
        public const string NameMinLength = "MootTable name must be at least 2 characters.";
        public const string NameMaxLength = "MootTable name cannot exceed 100 characters.";
        public const string TopicMaxLength = "Topic cannot exceed 1024 characters.";
    }
}
