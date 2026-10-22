using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.Common;

namespace Reki.Infrastructure.Context;

internal sealed class HttpRekiContextAccessor(
    IHttpContextAccessor httpContextAccessor) : IRekiContextAccessor
{
    public IRekiContext Context =>
        httpContextAccessor
            .HttpContext?.RequestServices
            .GetService<IRekiContext>() ??
        new NoPlayerRekiContext();
}