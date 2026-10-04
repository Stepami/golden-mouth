using Reki.Application.Example.GetExample;
using Reki.Contracts.Example;
using Riok.Mapperly.Abstractions;

namespace Reki.Api.Example.GetExample;

[Mapper]
public sealed partial class ExampleMapper
{
    public partial ExampleResponse Map(GetExampleResult source);
}
