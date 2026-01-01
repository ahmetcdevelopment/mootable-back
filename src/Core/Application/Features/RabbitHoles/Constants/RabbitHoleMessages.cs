namespace Mootable.Application.Features.RabbitHoles.Constants;

public static class RabbitHoleMessages
{
    public const string RabbitHoleNotFound = "RabbitHole not found.";
    public const string RabbitHoleLocked = "This RabbitHole is locked.";
    public const string RabbitHoleResolved = "This RabbitHole has been marked as resolved.";
    public const string MessageNotFound = "Starter message not found.";
    public const string MessageAlreadyHasRabbitHole = "This message already has a RabbitHole.";
    
    public static class Validation
    {
        public const string TitleRequired = "RabbitHole title is required.";
        public const string TitleMinLength = "Title must be at least 3 characters.";
        public const string TitleMaxLength = "Title cannot exceed 200 characters.";
        public const string MessageIdRequired = "Starter message ID is required.";
    }
}
