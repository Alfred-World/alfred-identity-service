// Disambiguate our ApplicationId from System.ApplicationId (macOS legacy API)

global using Alfred.Identity.Application.Common;
global using Alfred.Identity.Application.Querying.Core;
global using Alfred.Identity.Application.Querying.Fields;
global using Alfred.Identity.Application.Querying.Projection;
global using Alfred.Identity.Domain.Abstractions;
global using Alfred.Identity.Domain.Abstractions.Repositories;
global using Alfred.Identity.Domain.Common.Ids;

global using ApplicationId = Alfred.Identity.Domain.Common.Ids.ApplicationId;
