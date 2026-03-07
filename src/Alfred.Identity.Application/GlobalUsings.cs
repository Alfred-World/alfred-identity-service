// Disambiguate our ApplicationId from System.ApplicationId (macOS legacy API)

global using Alfred.Identity.Domain.Common.Ids;

global using ApplicationId = Alfred.Identity.Domain.Common.Ids.ApplicationId;
