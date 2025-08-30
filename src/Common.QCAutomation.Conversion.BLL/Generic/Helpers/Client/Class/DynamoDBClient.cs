using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Glue;
using Common.QCAutomation.Conversion.BLL.Generic.Enums;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface;
using Common.QCAutomation.Conversion.BLL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Class
{
    public class DynamoDBClient : IDynamoDBClient
    {
        AmazonDynamoDBClient? _amazonDynamoDBClient = null;
        private readonly ILogger<IDynamoDBClient> _logger;
        public readonly IConfiguration _configuration;
        private string _qcReportConfigTable;
        private string _qcReportDataSourceTable;
        private string _qcReportFileScanStateTable;
        private static string _currentDate = DateTime.Now.ToString("yyyyMMdd");
        public DynamoDBClient(ILogger<IDynamoDBClient> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _qcReportConfigTable = configuration["DynamoDB:QcReportConfigTable"] + (string)Environment.GetEnvironmentVariable("LAMBDA_ENV").ToLower() ?? throw new Exception();
            _qcReportDataSourceTable = configuration["DynamoDB:QcReportDataSourceTable"] + (string)Environment.GetEnvironmentVariable("LAMBDA_ENV").ToLower() ?? throw new Exception();
        }

        public AmazonDynamoDBClient GetDynamoDBClient()
        {
            var accessKey = _configuration["AWS:AccessKeyDynamoDB"];
            var secretKey = _configuration["AWS:SecretKeyDynamoDB"];
            if (_amazonDynamoDBClient == null)
            {
                if (accessKey == null || secretKey == null)
                {
                    _amazonDynamoDBClient = new AmazonDynamoDBClient();
                }
                else
                {
                    _amazonDynamoDBClient = new AmazonDynamoDBClient(accessKey, secretKey, Amazon.RegionEndpoint.CACentral1);
                }

            }
            return _amazonDynamoDBClient;
        }

        public async Task<Dictionary<string, AttributeValue>> GetReportConfiguration(string reportName)
        {
            var getItemRequest = new GetItemRequest
            {
                TableName = _qcReportConfigTable,
                Key = new Dictionary<string, AttributeValue>
                    {
                        { "ReportName", new AttributeValue { S = reportName.ToUpper() } }
                    }
            };
            var response = await GetDynamoDBClient().GetItemAsync(getItemRequest);
            //_logger.LogInformation($"01_Scan DynamoDB table {tableName} successfully and found total #of records: {response.Items.Count()}");

            return response.Item;
        }
        // Query the DynamoDB Data Source table
        public async Task<Dictionary<string, AttributeValue>> ReadDataSourceTable(string dataSourceName, int dataSourceSeqNo)
        {
            try
            {
                var getItemRequest = new GetItemRequest
                {
                    TableName = _qcReportDataSourceTable,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "DataSourceName", new AttributeValue { S = dataSourceName.ToUpper() } },
                        { "DataSourceSeqNo", new AttributeValue { N = dataSourceSeqNo.ToString() } }
                    }
                };
                var response = await GetDynamoDBClient().GetItemAsync(getItemRequest);

                return response.Item;
            }
            catch (AmazonDynamoDBException ex)
            {
                throw new ApplicationException($"!!!ERROR!!! Failed querying the DynamoDB table: {_qcReportDataSourceTable}", ex);
            }
        }

        // Parse List of Maps from List<AttributeValue>
        public List<Dictionary<string, object>> ParseListOfMaps(List<AttributeValue> list)
        {
            var result = new List<Dictionary<string, object>>();

            foreach (var item in list)
            {
                if (item.M != null)
                {
                    var map = new Dictionary<string, object>();
                    foreach (var kvp in item.M)
                    {
                        map[kvp.Key] = FormatAttributeValue(kvp.Value);
                    }
                    result.Add(map);
                }
            }
            return result;
        }

        // Convert AttributeValue to a simple type
        public object FormatAttributeValue(AttributeValue value)
        {
            if (value.S != null) return value.S;
            if (value.N != null) return int.Parse(value.N);
            if (value.IsBOOLSet) return value.BOOL;
            if (value.L != null) return value.L.Select(FormatAttributeValue).ToList();
            if (value.M != null)
            {
                var map = new Dictionary<string, object>();
                foreach (var kvp in value.M)
                {
                    map[kvp.Key] = FormatAttributeValue(kvp.Value);
                }
                return map;
            }
            throw new ApplicationException($"!!!ERROR!!! Value not recoganized: {value} when parsing the DataSourceFilePathList");
        }
    }
}
