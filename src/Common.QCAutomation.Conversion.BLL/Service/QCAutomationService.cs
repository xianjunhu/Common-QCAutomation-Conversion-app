using Amazon.DynamoDBv2.Model;
using Common.QCAutomation.Conversion.BLL.Generic.Constants;
using Common.QCAutomation.Conversion.BLL.Generic.Enums;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Class;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface;
using Common.QCAutomation.Conversion.BLL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Service
{
    public class QCAutomationService : IQCAutomationService
    {
        private readonly ILogger<IQCAutomationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IGlueClient _glueClient;
        private readonly IDynamoDBClient _dynamoDBClient;
        private readonly IFSXClient _fsXClient;
        private readonly IS3Client _s3Client;
        public DataSourceProcessedResult whatNext;
        private int processSteps;
        public QCAutomationService(ILogger<IQCAutomationService> logger, IConfiguration configuration, IGlueClient glueClient, IDynamoDBClient dynamoDBClient, IFSXClient fSXClient, IS3Client s3Client)
        {
            _logger = logger;
            _configuration = configuration;
            _glueClient = glueClient;
            _dynamoDBClient = dynamoDBClient;
            _fsXClient = fSXClient;
            _s3Client = s3Client;
        }

        public static IEnumerable<DateTime> EachCalendarDay(DateTime startDate, DateTime endDate)
        {
            for (var date = startDate.Date; date.Date <= endDate.Date; date = date.AddDays(1)) yield
            return date;
        }
        public async Task<string> ProcessQCAutomationFiles(ReportRequest reportRequest, CancellationToken cancellationToken)
        {
            processSteps = 1;

            try
            {
                _logger.LogInformation($"___Starting process the selected report: {reportRequest.ReportName} for date range: {reportRequest.StartDate.ToString("yyyyMMdd")} ~ {reportRequest.EndDate.ToString("yyyyMMdd")}");
                _logger.LogInformation($"00_Starting Lambda execution...");

                // 01__Get the Report Configuration for the specified report from DynamoDB table
                var currentRptConfig = await _dynamoDBClient.GetReportConfiguration(reportRequest.ReportName);
                if (!currentRptConfig.TryGetValue("DataSourceFilePathList", out var dataSourceFilePathList) ||
                    dataSourceFilePathList.L == null)
                {
                    _logger.LogInformation($"!!!ERROR!!!_DataSourceFilePathList attribute not found or not a list for report: {reportRequest.ReportName}");
                    return "InvalidReportName";
                }

                _logger.LogInformation($"___Starting process the selected report: {reportRequest.ReportName} for date range: {reportRequest.StartDate.ToString("yyyyMMdd")} ~ {reportRequest.EndDate.ToString("yyyyMMdd")}");

                // Convert DynamoDB item attributes to a readable format
                var reportConfiguration = new ReportConfiguration()
                {
                    ReportName = currentRptConfig["ReportName"].S,
                    //DataSourceName = currentRptConfig["DataSourceName"].S,
                    S3Bucket = currentRptConfig["S3Bucket"].S,
                    S3LandingZone = currentRptConfig["S3LandingZone"].S,
                    GlueWorkFlow = currentRptConfig["GlueWorkFlow"].S
                };
                _logger.LogInformation($"     - S3 Bucket Name: {reportConfiguration.S3Bucket}");
                _logger.LogInformation($"     - S3 Landing Zone: {reportConfiguration.S3LandingZone}");

                // Parse List of Maps for DataSourceFilePathList
                var currentReportDataSourcelist = _dynamoDBClient.ParseListOfMaps(dataSourceFilePathList.L);
                _logger.LogInformation($"     - Data Source File Path:");
                foreach (var map in currentReportDataSourcelist)
                {
                    map.TryGetValue("FilePath", out var currentFilePath);
                    _logger.LogInformation($"       >>>> {currentFilePath}");
                }
                _logger.LogInformation(string.Empty);

                //02__Process all input files required for the selected report
                var dsProcessedReult = await ProcessReportDataSource(reportConfiguration, currentReportDataSourcelist, reportRequest);
                if (dsProcessedReult == DataSourceProcessedResult.FileNotFound)
                {
                    _logger.LogInformation(string.Empty);
                    _logger.LogInformation($"_____{processSteps}_The required input file not found, exit the lambda without processing the selected report!");
                    return "FileNotFound";
                }

                if (dsProcessedReult == DataSourceProcessedResult.FileMigrated)
                {
                    _logger.LogInformation(string.Empty);
                    _logger.LogInformation($"_____{processSteps}_The required input file in S3 has been migrated to Glacier, exit the lambda without processing the selected report!");
                    return "S3FileMigrated";
                }

                _logger.LogInformation(string.Empty);
                _logger.LogInformation($"_____{processSteps}_All input files are converted for the selected report, ready to trigger the Glue workflow");
                processSteps++;

                string responseMessage = string.Empty;
                // Step 3: All files present in S3, trigger Glue Workflow
                _logger.LogInformation(string.Empty);
                if (string.IsNullOrEmpty(reportConfiguration.GlueWorkFlow))// _workflowName != null && _workflowName != string.Empty)
                {
                    _logger.LogInformation($"_____{processSteps}_Lambda testing only - NO Glue Workflow to trigger for the report: {reportConfiguration.ReportName}");
                    responseMessage = "AllFilesConverted";
                }
                else
                {
                    // Step 4: Start the Glue Workflow
                    _logger.LogInformation($"_____{processSteps}_Starting trigger the Glue Worflow... {reportConfiguration.GlueWorkFlow}");
                    await _glueClient.StartWorkflow(reportConfiguration.GlueWorkFlow, reportRequest.StartDate);
                    responseMessage = "GlueFlowStarted";
                }

                _logger.LogInformation(string.Empty);
                _logger.LogInformation("99_Lambda exeution ended.");
                _logger.LogInformation(string.Empty);
                return responseMessage;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"!!!ERROR!!! ProcessQCAutomationFiles failed: {ex.Message}",ex);
            }
        }

        // Process/Convert all files required to generate the selected report
        public async Task<DataSourceProcessedResult> ProcessReportDataSource(ReportConfiguration reportConfiguration, List<Dictionary<string, object>> reportDataSourceList, ReportRequest reportRequest)
        {
            _logger.LogInformation($"_____Loop process all input files required for the selected report: {reportConfiguration.ReportName}");
            foreach (var map in reportDataSourceList)
            {
                var currentFileSource = new DataSourceFilePath()
                {
                    FilePath = map.ContainsKey("FilePath") ? (string)map["FilePath"] : string.Empty,
                    DataSourceName = map.ContainsKey("DataSourceName") ? (string)map["DataSourceName"] : string.Empty,
                    DataSourceSeqNo = map.ContainsKey("DataSourceSeqNo") ? (int)map["DataSourceSeqNo"] : 0,
                    PhaseEnabled = map.ContainsKey("PhaseEnabled") ? (string)map["PhaseEnabled"] : string.Empty,
                    FileExtension = map.ContainsKey("FileExtension") ? (string)map["FileExtension"] : string.Empty,
                    IncludeFiles = map.ContainsKey("IncludeFiles") ? (string)map["IncludeFiles"] : string.Empty,
                    ExcludeFiles = map.ContainsKey("ExcludeFiles") ? (string)map["ExcludeFiles"] : string.Empty
                };

                _logger.LogInformation(string.Empty);
                if (currentFileSource.PhaseEnabled.Equals("None"))
                {
                    currentFileSource.DataSourcePhase = string.Empty;
                    reportConfiguration.S3UploadFolder = reportConfiguration.S3LandingZone;
                    _logger.LogInformation($"_____{processSteps}_Process the current input file pattern {currentFileSource.FilePath} without Data Source Phase enabled");
                    whatNext = await ProcessCurrentFileSourceByDateRange(reportConfiguration, currentFileSource, reportRequest.StartDate, reportRequest.EndDate);
                }
                else
                {
                    _logger.LogInformation($"_____{processSteps}_Process the current input file pattern {currentFileSource.FilePath} with Data_Source_Phase_Parameter = {currentFileSource.PhaseEnabled}");
                    //switch (currentFileSource.PhaseEnabled)
                    switch (reportRequest.DataPhase.ToLower())
                    {
                        case Constants.PhaseEnabled_Overnight:
                            currentFileSource.DataSourcePhase = Constants.Overnight;
                            reportConfiguration.S3UploadFolder = reportConfiguration.S3LandingZone + Constants.PhaseEnabled_Overnight + "/";
                            whatNext = await ProcessCurrentFileSourceByDateRange(reportConfiguration, currentFileSource, reportRequest.StartDate, reportRequest.EndDate);
                            break;
                        case Constants.PhaseEnabled_Final:
                            currentFileSource.DataSourcePhase = Constants.Final;
                            reportConfiguration.S3UploadFolder = reportConfiguration.S3LandingZone + Constants.PhaseEnabled_Final + "/";
                            whatNext = await ProcessCurrentFileSourceByDateRange(reportConfiguration, currentFileSource, reportRequest.StartDate, reportRequest.EndDate);
                            break;
                        case Constants.PhaseEnabled_Both:
                            _logger.LogInformation($"       >> First, process the current input file pattern {currentFileSource.FilePath} for Overnight");
                            currentFileSource.DataSourcePhase = Constants.Overnight;
                            reportConfiguration.S3UploadFolder = reportConfiguration.S3LandingZone + Constants.PhaseEnabled_Overnight + "/";
                            whatNext = await ProcessCurrentFileSourceByDateRange(reportConfiguration, currentFileSource, reportRequest.StartDate, reportRequest.EndDate);

                            if (whatNext != DataSourceProcessedResult.FileNotFound)
                            {
                                _logger.LogInformation(string.Empty);
                                _logger.LogInformation($"       >> Second, process the current input file pattern {currentFileSource.FilePath} for Final");
                                currentFileSource.DataSourcePhase = Constants.Final;
                                reportConfiguration.S3UploadFolder = reportConfiguration.S3LandingZone + Constants.PhaseEnabled_Final + "/";
                                whatNext = await ProcessCurrentFileSourceByDateRange(reportConfiguration, currentFileSource, reportRequest.StartDate, reportRequest.EndDate);
                            }
                            break;
                        default:
                            currentFileSource.DataSourcePhase = string.Empty;
                            _logger.LogInformation($"_____{processSteps}_Process the current input file {currentFileSource.FilePath} without Data Source Phase enabled");
                            whatNext = await ProcessCurrentFileSourceByDateRange(reportConfiguration, currentFileSource, reportRequest.StartDate, reportRequest.EndDate);
                            break;
                    }
                }
                processSteps++;
                if (whatNext != DataSourceProcessedResult.FilesConverted) return whatNext;

            } // End the Loop of foreach map
            return DataSourceProcessedResult.FilesConverted;
        }
        public async Task<DataSourceProcessedResult> ProcessCurrentFileSourceByDateRange(ReportConfiguration reportConfiguration, DataSourceFilePath dataSourceFilePath, DateTime startDate, DateTime endDate)
        {
            //bool fileConverted;
            //fileConverted = await _s3Client.CheckFileInS3(reportConfiguration.S3Bucket, dataSourceFilePath.FileName);
            //if (fileConverted) continue;

            //04_Get the Data Source Configuration from DynamoDB based on the report configuration
            var dataSourceItem = await _dynamoDBClient.ReadDataSourceTable(dataSourceFilePath.DataSourceName, dataSourceFilePath.DataSourceSeqNo);
            var reportDataSource = new ReportDataSource()
            {
                DataSourceName = dataSourceItem["DataSourceName"].S,
                AuthenticationDomain = dataSourceItem["AuthenticationDomain"].S,
                DnsOrBucketName = dataSourceItem["DnsOrBucketName"].S,
                ShareName = dataSourceItem["ShareName"].S,
                AuthenticationSecretName = dataSourceItem["AuthenticationSecretName"].S
            };

            _logger.LogInformation(string.Empty);
            _logger.LogInformation($"          (1)_Read the Data Source DynamoDB table successfully for Data Source SeqNo#: {dataSourceFilePath.DataSourceSeqNo}");
            _logger.LogInformation($"             - Data Source: {reportDataSource.DataSourceName}");

            //
            // Adjust the date range for Overnight & Final data files upon selected reports
            //
            if (reportConfiguration.ReportName.Contains("TV") ||
                reportConfiguration.ReportName.Contains("DFR") ) { 
                // Go back 1 day prior to the start date for all TV including DFR report
                startDate = startDate.AddDays(-1);
            }

            if (reportConfiguration.ReportName.Equals(Constants.Report_TV_Final.ToUpper()) &&
               (dataSourceFilePath.DataSourcePhase.Equals(Constants.Overnight)) ) { 
                // Get 1 more week of the data files for Overnight upon "TV Final" report only
                endDate = endDate.AddDays(+7);
            }

            var _fileNotFound = false;
            DataSourceProcessedResult s3FileResult;
            _logger.LogInformation($"          (2)_Start converting the input files for date range: {startDate.ToString("yyyyMMdd")} ~ {endDate.ToString("yyyyMMdd")}");
            if (dataSourceFilePath.DataSourceName.Equals(Constants.DataSource_FSx))
            {
                _logger.LogInformation($"             - Domain: {reportDataSource.AuthenticationDomain}");
                _logger.LogInformation($"             - FSx DNS Name: {reportDataSource.DnsOrBucketName}");
                _logger.LogInformation($"             - Share Name: {reportDataSource.ShareName}");
                _logger.LogInformation($"             - AD User Secret Name: {reportDataSource.AuthenticationSecretName}");

                foreach (DateTime currentDate in EachCalendarDay(startDate, endDate))
                {
                    //01_Assembly the full name of the current file
                    dataSourceFilePath.FileName = dataSourceFilePath.FilePath + dataSourceFilePath.DataSourcePhase +
                                                currentDate.ToString("yyyyMMdd").Substring(2) + dataSourceFilePath.FileExtension;

                    _fileNotFound = await _fsXClient.ProcessCurrentFileOneDateFSxAsync(reportDataSource, dataSourceFilePath, reportConfiguration, currentDate);

                    if (_fileNotFound) break;
                }
            }
            if (_fileNotFound)
                return DataSourceProcessedResult.FileNotFound;

            if (dataSourceFilePath.DataSourceName.Equals(Constants.DataSource_S3))
            {
                _logger.LogInformation($"             - S3 Bucket: {reportDataSource.DnsOrBucketName}");
                s3FileResult = await _s3Client.ProcessCurrentFileOneDateS3Async(dataSourceFilePath, reportConfiguration, reportDataSource.DnsOrBucketName, dataSourceFilePath.FilePath, startDate, endDate);
                return s3FileResult;
            }            
            return DataSourceProcessedResult.FilesConverted;
        }
    }

    public class ReportRequest
    {
        public string ReportName { get; set; } = "";
        public string DataPhase { get; set; } = "";
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now;
    }

}