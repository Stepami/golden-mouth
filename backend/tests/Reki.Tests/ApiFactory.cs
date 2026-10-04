using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TUnit.AspNetCore;

namespace Reki.Tests;

public sealed class ApiFactory : TestWebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services => services
            .AddControllers()
            .AddApplicationPart(typeof(TestExceptionController).Assembly));
    }
}
