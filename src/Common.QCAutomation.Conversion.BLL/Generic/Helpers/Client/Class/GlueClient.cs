using Amazon.Glue;
using Amazon.Glue.Model;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface;
using Common.QCAutomation.Conversion.BLL.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Class
{
    public class GlueClient : IGlueClient
    {
        AmazonGlueClient? amazonGlueClient = null;
        private readonly ILogger<IGlueClient> _logger;
        public readonly IConfiguration _configuration;
        public GlueClient(ILogger<IGlueClient> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public AmazonGlueClient GetAmazonGlueClient()
        {
            var accessKey = _configuration["AWS:AccessKeyGlue"];
            var secretKey = _configuration["AWS:SecretKeyGlue"];
            if (amazonGlueClient == null)
            {
                if (accessKey == null || secretKey == null)
                {
                    amazonGlueClient = new AmazonGlueClient();
                }
                else
                {
                    amazonGlueClient = new AmazonGlueClient(accessKey, secretKey, Amazon.RegionEndpoint.CACentral1);
                }

            }
            return amazonGlueClient;

        }

        public async Task StartWorkflow(string workFlowName, DateTime startDate)
        {
            try
            {
                var request = new StartWorkflowRunRequest
                {
                    Name = workFlowName,
                    RunProperties = new Dictionary<string, string>
                    {
                        { "focus_week", startDate.ToString("yyyyMMdd").Substring(2) }
                    }
                };
                var response = await GetAmazonGlueClient().StartWorkflowRunAsync(request);
                _logger.LogInformation($"          - Started the Glue Workflow {workFlowName}, RunId: {response.RunId}");
            }
            catch (AmazonGlueException ex)
            {
                throw new ApplicationException($"!!!ERROR!!! Failed starting the Glue Workflow {workFlowName}", ex);
            }
        }
    }
}
