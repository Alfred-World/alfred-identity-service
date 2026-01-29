namespace Alfred.Identity.Application.Applications.Common;

public record ApplicationMetadataDto(
    List<string> ApplicationTypes,
    List<string> ClientTypes,
    List<string> GrantTypes,
    List<string> Scopes,
    List<string> Endpoints
);
