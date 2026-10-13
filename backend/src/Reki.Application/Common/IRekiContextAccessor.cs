namespace Reki.Application.Common;

public interface IRekiContextAccessor
{
    IRekiContext Context { get; }
}