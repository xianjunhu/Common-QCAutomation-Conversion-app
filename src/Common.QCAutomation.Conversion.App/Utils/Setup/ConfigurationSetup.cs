using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace Common.QCAutomation.Conversion.App.Utils.Setup
{
    /// <summary>
    /// This class setup the Secret Manager
    /// </summary>
    public static class ConfigurationSetup
    {
        /// <summary>
        /// This function add Secret Manager to the application, which help us to get the AWS secrets configured for our application
        /// </summary>
        /// <param name="configBuiler"></param>
        public static void AddSecrets(this IConfigurationBuilder configBuiler, string environment)
        {
            var config = configBuiler.Build();
            var accessKey = config["AWS:AccessKeySecretManager"];
            var secretKey = config["AWS:SecretKeySecretManager"];
            var acceptedARN = config["AWS:SecretARN"];

            if (!string.IsNullOrEmpty(acceptedARN))
            {
                acceptedARN = acceptedARN.Replace("env", environment.ToLower());
                BasicAWSCredentials? credentials = null;
                if (!string.IsNullOrEmpty(secretKey))
                {
                    credentials = new(accessKey, secretKey);
                    Console.WriteLine("Connecting Secret Manager with Access key and Secret.");
                }
                else
                {
                    Console.WriteLine("Connecting Secret Manager in AWS environment.");
                }

                configBuiler.AddSecretsManager(credentials, Amazon.RegionEndpoint.CACentral1, configurator: config =>
                {
                    config.AcceptedSecretArns = new List<string>() { acceptedARN };
                    //config.KeyGenerator = (secret, name) => name.Substring(acceptedARN.Length + 1).Replace("__", ":");
                });
            }
            else
            {
                Console.WriteLine("Skip Secret Manager - SM ARN was not specified.");
            }
        }
    }
}
