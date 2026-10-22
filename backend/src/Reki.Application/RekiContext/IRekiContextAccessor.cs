namespace Reki.Application.RekiContext;

public interface IRekiContextAccessor
{
    IRekiContext Context { get; }
}