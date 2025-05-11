public static class RoleConstants
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string User = "User";
    public static IEnumerable<string> GetDefaultRoles()
    {
        yield return Administrator;
        yield return Manager;
        yield return User;
    }
}