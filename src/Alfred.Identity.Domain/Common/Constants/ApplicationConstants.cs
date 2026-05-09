namespace Alfred.Identity.Domain.Common.Constants;

/// <summary>
/// Constants for OAuth application metadata and permissions.
/// </summary>
public static class ApplicationConstants
{
    public static class ClientIds
    {
        public const string SsoWeb = "sso_web";
    }

    public static class ApplicationTypes
    {
        public const string Web = "web";
        public const string Native = "native";
        public const string Machine = "machine";
        public const string Spa = "spa";

        public static readonly IReadOnlyList<string> All = [Web, Native, Machine, Spa];
    }

    public static class ClientTypes
    {
        public const string Confidential = "confidential";
        public const string Public = "public";

        public static readonly IReadOnlyList<string> All = [Confidential, Public];
    }

    public static class AppScopePrefixes
    {
        public const string GrantType = "gt:";
        public const string Endpoint = "ept:";
        public const string Scope = "scp:";
    }

    public static class Endpoints
    {
        public const string Authorization = "authorization";
        public const string Token = "token";
        public const string UserInfo = "userinfo";
        public const string Introspection = "introspection";
        public const string Revocation = "revocation";
        public const string Logout = "logout";

        public static readonly IReadOnlyList<string> All =
            [Authorization, Token, UserInfo, Introspection, Revocation, Logout];
    }
}
