using Amazon.Lambda.Annotations;
using Common.QCAutomation.Conversion.App.Utils.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.QCAutomation.Conversion.App;

[LambdaStartup]
public class Startup
{
    /// <summary>
    /// Services for Lambda functions can be registered in the services dependency injection container in this method. 
    ///
    /// The services can be injected into the Lambda function through the containing type's constructor or as a
    /// parameter in the Lambda function using the FromService attribute. Services injected for the constructor have
    /// the lifetime of the Lambda compute container. Services injected as parameters are created within the scope
    /// of the function invocation.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        var environment = Environment.GetEnvironmentVariable("LAMBDA_ENV");
        Console.WriteLine("Common.QCAutomation.Conversion.app : Environment is: " + environment);

        var _environment = string.Empty;
        var appSettingsRegion = string.Empty;
        var appSettingsSmARN = string.Empty;
        var configurationBuilder = new ConfigurationBuilder();

        if (!string.IsNullOrEmpty(environment))
        {
            _environment = environment;
        }

        switch (_environment.ToLower())
        {
            case "dev":
            case "preprod":
            case "prod":
                Console.WriteLine("Common.QCAutomation.Conversion.app : Environment is " + _environment.ToLower());
                configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", true)
                 .AddEnvironmentVariables();
                break;
            case "local":
                Console.WriteLine("Common.QCAutomation.Conversion.app : Environment is local. ");
                configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables();
                break;
            default:
                break;

        }

        configurationBuilder.AddSecrets(_environment);
        var Configuration = configurationBuilder.Build();

        services.AddSingleton<IConfiguration>(Configuration);
        services.AddLogging(Configuration, _environment);   
        services.AddServiceDependencies();
    }
}
