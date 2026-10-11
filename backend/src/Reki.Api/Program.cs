using Reki.Api.Bootstrapping;
using Serilog;

var bootstrapLogger = Bootstrap.CreateInitialLogger();
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddSerilog(bootstrapLogger, dispose: true));
var applicationLogger = loggerFactory
    .CreateLogger<ApplicationRunner>();
var applicationRunner = new ApplicationRunner(applicationLogger);

await applicationRunner.RunAsync(args);

namespace Reki.Api
{
    public partial class Program
    {
        protected Program()
        {
        }
    }
}
