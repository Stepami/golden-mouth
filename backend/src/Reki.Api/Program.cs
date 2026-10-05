using Reki.Api.Bootstrapping;
using Serilog;

Serilog.ILogger bootstrapLogger = Bootstrap.CreateInitialLogger();
using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    builder.AddSerilog(bootstrapLogger, dispose: true));
ILogger<ApplicationRunner> applicationLogger = loggerFactory
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
