using System.Net;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Microsoft.Extensions.Logging;
using Common.QCAutomation.Conversion.BLL.Service;
using Common.QCAutomation.Conversion.BLL.Generic.Constants;
using Serilog;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace App;

public class Functions
{
    private readonly ILogger<Functions> _logger;
    private readonly IQCAutomationService _iqcautomationService;
    private ReportRequest reportRequest;

    /// <summary>
    /// Default constructor that Lambda will invoke.
    /// </summary>
    public Functions(ILogger<Functions> logger, IQCAutomationService iqcautomationService)
    {
        _logger = logger;
        _iqcautomationService = iqcautomationService;
    }

    /// <summary>
    /// A Lambda function to respond to HTTP Get methods from API Gateway
    /// </summary>
    /// <remarks>
    /// This uses the <see href="https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.Annotations/README.md">Lambda Annotations</see> 
    /// programming model to bridge the gap between the Lambda programming model and a more idiomatic .NET model.
    /// 
    /// This automatically handles reading parameters from an APIGatewayHttpApiV2ProxyRequest
    /// as well as syncing the function definitions to serverless.template each time you build.
    /// 
    /// If you do not wish to use this model and need to manipulate the API Gateway 
    /// objects directly, see the accompanying Readme.md for instructions.
    /// </remarks>
    /// <param name="context">Information about the invocation, function, and execution environment</param>
    /// <returns>The response as an implicit <see cref="APIGatewayHttpApiV2ProxyResponse"/></returns>
    [LambdaFunction]
    //public async void QCFileProcessor(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    public async Task<APIGatewayHttpApiV2ProxyResponse> QCFileProcessor(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        string ResponseMessage = string.Empty;

        ResponseMessage = await RequestValidation(request);

        if (ResponseMessage.Equals(string.Empty))
        {
            ResponseMessage = "RequestAccepted";
            try
            {
                ResponseMessage = await _iqcautomationService.ProcessQCAutomationFiles(reportRequest, CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QCFileProcessor] Generic Lambda error.");
                throw;
            }
            finally
            {
                Console.WriteLine("QCFileProcessor execution completed.");
                Log.CloseAndFlush();
                //// wait for 1 sec to flush logs            
                Thread.Sleep(1000);
            }
        }
        Console.WriteLine(ResponseMessage);
        var result = ResponseMessages.Get(ResponseMessage);
        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = result.StatusCode,
            Body = JsonSerializer.Serialize(new { result.Message }),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
    public async Task<string> RequestValidation(APIGatewayHttpApiV2ProxyRequest request)
    {
        string AuthToken = string.Empty;
        string validationMessage = string.Empty;

        if (request.RequestContext is not null)
        {
            if (request.RequestContext.Http.Method != HttpMethods.Post)
            {
                _logger.LogInformation($"!!!ERROR!!!-[QCFileProcessor] Request method is not POST: {request.RequestContext.Http.Method}");
                return "InvalidMethod";
            }
        }

        // Check if Authorization header exists
        if (!request.Headers.TryGetValue("authorization", out AuthToken)) 
        {
            if (!request.Headers.TryGetValue("Authorization", out AuthToken))
            {
                _logger.LogInformation($"!!!ERROR!!!-[QCFileProcessor] Authorization header missing {AuthToken}!");
                return "AuthHeaderMissing";
            }
        }

        // Validate token by environments
        string auth_token = string.Empty;
        switch ((string)Environment.GetEnvironmentVariable("LAMBDA_ENV").ToLower())
        {
            case Constants.ENV_DEV:
                auth_token = Constants.AUTH_TOKEN_DEV;
                break;
            case Constants.ENV_PREPROD:
                auth_token = Constants.AUTH_TOKEN_PREPROD;
                break;
            case Constants.ENV_PROD:
                auth_token = Constants.AUTH_TOKEN_PROD;
                break;
            default:
                auth_token = Constants.AUTH_TOKEN_DEV;
                break;
        }
        if (AuthToken != $"Bearer {auth_token}")
        {
            _logger.LogInformation($"!!!ERROR!!!-[QCFileProcessor] Authentication Failed - Invalid Token!");
            return "InvalidToken";
        }

        if (request.Body == null || string.IsNullOrEmpty(request.Body)) 
        {
            if (Environment.GetEnvironmentVariable("LAMBDA_ENV").Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"!!!ERROR!!!-[QCFileProcessor] Request body is null or empty - To test with the request from the env. variables.");
                var reportName = Environment.GetEnvironmentVariable("ReportName");
                var dsPhase = Environment.GetEnvironmentVariable("DataPhase");
                var startDate = Environment.GetEnvironmentVariable("StartDate");
                var endDate = Environment.GetEnvironmentVariable("EndDate");

                // Convert startDate and endDate from string to DateTime
                DateTime parsedStartDate = DateTime.TryParse(startDate, out var tempStartDate) ? tempStartDate : default;
                DateTime parsedEndDate = DateTime.TryParse(endDate, out var tempEndDate) ? tempEndDate : default;

                reportRequest = new ReportRequest
                {
                    ReportName = reportName,
                    DataPhase = dsPhase,
                    StartDate = parsedStartDate,
                    EndDate = parsedEndDate
                };
            }
            else
            {
                _logger.LogInformation($"!!!ERROR!!!-[QCFileProcessor] Request body is null or empty!");
                validationMessage = "EmptyRequest";
            }
        }
        else
        {
            try
            {
                reportRequest = JsonSerializer.Deserialize<ReportRequest>(request.Body);
                _logger.LogInformation($"[QCFileProcessor] Handling the report request: {reportRequest}");

                if (reportRequest == null)
                {
                    _logger.LogInformation($"!!!ERROR!!!-[QCFileProcessor] The request payload can not be null");
                    validationMessage = "EmptyRequest";
                }
                else
                {
                    if (string.IsNullOrEmpty(reportRequest.ReportName))
                    {
                        _logger.LogInformation($"!!!ERROR!!!-[QCFileProcessor] Invalid report name: {reportRequest.ReportName}");
                        validationMessage = "InvalidReportName";
                    }
                    if (reportRequest.StartDate > reportRequest.EndDate)
                    {
                        _logger.LogInformation($"!!!ERROR!!!-[QCFileProcessor] Start date cannot be greater than end date: {reportRequest.StartDate} > {reportRequest.EndDate}");
                        validationMessage = "InvalidDateRange";
                    }
                    if (!reportRequest.DataPhase.ToLower().Equals(Constants.PhaseEnabled_Final) &&
                        !reportRequest.DataPhase.ToLower().Equals(Constants.PhaseEnabled_Overnight) &&
                        !reportRequest.DataPhase.ToLower().Equals(Constants.PhaseEnabled_Both))
                    {
                        _logger.LogInformation($"!!!ERROR!!!-[QCFileProcessor] Invalid DataPhase {reportRequest.DataPhase}");
                        validationMessage = "InvalidDataPhase";
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[QCFileProcessor] Error serializing the request payload!");
                validationMessage = "InvalidRequest";
            }
        }
        return validationMessage;
    }
}
