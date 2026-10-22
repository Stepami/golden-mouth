using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed class GetWorldTemplatesHandler(IWorldRepository worldRepository) : IRequestHandler<GetWorldTemplatesQuery, IReadOnlyList<World>>
{
    public async ValueTask<IReadOnlyList<World>> Handle(GetWorldTemplatesQuery request, CancellationToken cancellationToken) =>
        await worldRepository.GetWorldTemplatesAsync(cancellationToken);
}
