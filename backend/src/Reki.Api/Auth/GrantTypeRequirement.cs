using Microsoft.AspNetCore.Authorization;

namespace Reki.Api.Auth;

internal sealed record GrantTypeRequirement(string GrantType) : IAuthorizationRequirement;
