// See https://aka.ms/new-console-template for more information

using Demo.Decorator.ConsoleApp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Debug);
    
    // Ensure console provider is configured properly
    builder.AddSimpleConsole(options =>
    {
        options.IncludeScopes = false;
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
});

 try {
    // Example 1: User Service with a bunch of decorators and fluid API usage
    await UserServiceDemo.FluidApiUsageDemo(loggerFactory);

    // Example 2: Email Service with one base class and fluid API usage
    await EmailServiceDemo.FluidApiUsageDemo(loggerFactory);
}
finally
{
    loggerFactory.Dispose();
}