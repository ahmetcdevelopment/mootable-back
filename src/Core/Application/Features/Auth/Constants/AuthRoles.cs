namespace Mootable.Application.Features.Auth.Constants;

public static class AuthRoles
{
    public const string User = "User";
    public const string Moderator = "Moderator";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";
    
    public static readonly string[] All = { User, Moderator, Admin, SuperAdmin };
    public static readonly string[] Elevated = { Moderator, Admin, SuperAdmin };
    public static readonly string[] Administrative = { Admin, SuperAdmin };
}
