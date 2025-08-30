using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Class;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Class;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Interface;
using Common.QCAutomation.Conversion.BLL.Service;
using Microsoft.Extensions.DependencyInjection;

namespace Common.QCAutomation.Conversion.App.Utils.Setup
{
    /// <summary>
    /// Inject all the created services 
    /// </summary>
    public static class DependencyDefinition
    {
        /// <summary>
        /// Add services to inject 
        /// </summary>
        /// <param name="services"></param>
        public static void AddServiceDependencies(this IServiceCollection services)
        {
            services.AddScoped<IQCAutomationService, QCAutomationService>();
            services.AddScoped<IDynamoDBClient, DynamoDBClient>();
            services.AddScoped<IGlueClient, GlueClient>();
            services.AddScoped<IFSXClient, FSXClient>();
            services.AddScoped<IS3Client, S3Client>();
            services.AddScoped<ICSVConverter, CSVConverter>();
        }
    }
}
