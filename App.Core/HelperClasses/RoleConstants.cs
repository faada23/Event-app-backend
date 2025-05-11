public static class RoleConstants
{
    public const string Administrator = "Admin";
    public const string User = "User";
    public static IEnumerable<string> GetDefaultRoles()
    {
        yield return Administrator;
        yield return User;
    }
}