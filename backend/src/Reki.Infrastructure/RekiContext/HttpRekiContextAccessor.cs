using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.RekiContext;

namespace Reki.Infrastructure.RekiContext;

internal sealed class HttpRekiContextAccessor(
    IHttpContextAccessor httpContextAccessor) : IRekiContextAccessor
{
    public IRekiContext Context =>
        httpContextAccessor
            .HttpContext?.RequestServices
            .GetService<IRekiContext>() ??
        new NoPlayerRekiContext();
}