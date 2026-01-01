namespace Mootable.Application.Features.Auth.Constants;

public static class AuthMessages
{
    public const string InvalidCredentials = "Invalid email or password.";
    public const string EmailAlreadyExists = "A user with this email already exists.";
    public const string UsernameAlreadyExists = "A user with this username already exists.";
    public const string UserNotFound = "User not found.";
    public const string InvalidRefreshToken = "Invalid or expired refresh token.";
    public const string RefreshTokenRevoked = "Refresh token has been revoked.";
    public const string AccountLocked = "Account is locked. Please try again later.";
    
    public static class Validation
    {
        public const string EmailRequired = "Email is required.";
        public const string EmailInvalid = "Invalid email format.";
        public const string PasswordRequired = "Password is required.";
        public const string PasswordMinLength = "Password must be at least 8 characters.";
        public const string PasswordRequiresUppercase = "Password must contain at least one uppercase letter.";
        public const string PasswordRequiresLowercase = "Password must contain at least one lowercase letter.";
        public const string PasswordRequiresDigit = "Password must contain at least one digit.";
        public const string PasswordRequiresSpecialChar = "Password must contain at least one special character.";
        public const string UsernameRequired = "Username is required.";
        public const string UsernameMinLength = "Username must be at least 3 characters.";
        public const string UsernameMaxLength = "Username cannot exceed 32 characters.";
        public const string UsernameInvalidChars = "Username can only contain letters, numbers, and underscores.";
    }
}
