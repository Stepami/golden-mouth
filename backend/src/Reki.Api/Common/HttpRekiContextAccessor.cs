using Reki.Application.Common;

namespace Reki.Api.Common;

internal sealed class HttpRekiContextAccessor(
    IHttpContextAccessor httpContextAccessor) : IRekiContextAccessor
{
    public IRekiContext Context =>
        httpContextAccessor
            .HttpContext?.RequestServices
            .GetService<IRekiContext>() ??
        new NoPlayerRekiContext();
}