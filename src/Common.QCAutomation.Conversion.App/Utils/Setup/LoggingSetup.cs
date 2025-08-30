using Serilog.Formatting.Compact;
using Serilog;
using AWS.Logger;
using System.Drawing;
using AWS.Logger.SeriLog;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Common.QCAutomation.Conversion.App.Utils.Setup
{
    /// <summary>
    /// Class to setup Serilog 
    /// </summary>
    public static class LoggingSetup
    {
        /// <summary>
        /// Set serilog configuration and inject it 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration configuration, string environment)
        {
            var awsS3sKey = configuration["AWS:AccessKeyCloudWatch"];
            var awsS3Secret = configuration["AWS:SecretKeyCloudWatch"];
            var logGroupName = configuration["Serilog:LogGroup"]?.Replace("env", environment.ToLower());
            var region = configuration["Serilog:Region"];
            var awsLogLevel = Serilog.Events.LogEventLevel.Debug;
            Enum.TryParse(configuration["Serilog:AWSLogLevel"], out awsLogLevel);

            // create a logger for AWS Cloudwatch
            var awsConfiguration = new AWSLoggerConfig
            {
                Region = region,
                LogGroup = logGroupName,
                LibraryLogErrors = false,
                DisableLogGroupCreation = true,
            };

            // create your AWS credential
            if (!string.IsNullOrWhiteSpace(awsS3Secret))
            {
                awsConfiguration.Credentials = new BasicAWSCredentials(awsS3sKey, awsS3Secret);
            }

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .WriteTo.Console()
                .WriteTo.AWSSeriLog(
                   configuration: awsConfiguration,
                    textFormatter: new RenderedCompactJsonFormatter(),
                    restrictedToMinimumLevel: awsLogLevel)
                .Enrich.WithProperty("App", "QCAutomationConversion")
                .CreateLogger();

            services.AddLogging(loggingBuilder =>// AddLogging() requires Microsoft.Extensions.Logging NuGet package
            {

                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(Log.Logger); // AddConsole() requires Microsoft.Extensions.Logging.Console NuGet package
            });

            //Console.WriteLine($"AWS CloudWatch Logging configured for {logGroupName} log group with {awsLogLevel} log level.");
            var connectionType = awsConfiguration.Credentials == null ? "AWS env" : "AccessKey/Secret";
            Log.Logger.Information($"AWS CloudWatch Logging configured for {logGroupName} log group with {awsLogLevel} log level. Connection with {connectionType}");

            return services;

        }
    }
}
