using System.Text.Json;

using Alfred.Identity.Domain.Common.Constants;

namespace Alfred.Identity.Application.Auth.Common;

internal static class OidcClientPermissions
{
    public static bool SupportsEndpoint(Domain.Entities.Application client, string endpoint)
    {
        return Parse(client.Permissions)
            .Contains($"{ApplicationConstants.AppScopePrefixes.Endpoint}{endpoint}");
    }

    public static bool SupportsGrantType(Domain.Entities.Application client, string grantType)
    {
        return Parse(client.Permissions)
            .Contains($"{ApplicationConstants.AppScopePrefixes.GrantType}{grantType}");
    }

    public static bool AreScopesAllowed(Domain.Entities.Application client, string? requestedScopes,
        out string[] unsupportedScopes)
    {
        var requested = ParseRequestedScopes(requestedScopes);
        if (requested.Length == 0)
        {
            unsupportedScopes = [];
            return true;
        }

        var allowedScopes = Parse(client.Permissions)
            .Where(scope =>
                scope.StartsWith(ApplicationConstants.AppScopePrefixes.Scope, StringComparison.OrdinalIgnoreCase))
            .Select(scope => scope[ApplicationConstants.AppScopePrefixes.Scope.Length..])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        unsupportedScopes = requested
            .Where(scope => !allowedScopes.Contains(scope))
            .ToArray();

        return unsupportedScopes.Length == 0;
    }

    public static bool ContainsScope(string? scopes, string requiredScope)
    {
        return ParseRequestedScopes(scopes)
            .Contains(requiredScope, StringComparer.OrdinalIgnoreCase);
    }

    public static string[] ParseRequestedScopes(string? scopes)
    {
        return string.IsNullOrWhiteSpace(scopes)
            ? []
            : scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    private static HashSet<string> Parse(string? permissions)
    {
        if (string.IsNullOrWhiteSpace(permissions))
        {
            return [];
        }

        if (permissions.TrimStart().StartsWith('['))
        {
            try
            {
                var items = JsonSerializer.Deserialize<string[]>(permissions);
                return items == null
                    ? []
                    : items
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(item => item.Trim())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
            }
        }

        return permissions
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
