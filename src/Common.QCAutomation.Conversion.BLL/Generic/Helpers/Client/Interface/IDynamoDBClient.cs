using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Glue;
using Common.QCAutomation.Conversion.BLL.Generic.Enums;
using Common.QCAutomation.Conversion.BLL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface
{
    public interface IDynamoDBClient
    {
        public AmazonDynamoDBClient GetDynamoDBClient();

        public Task<Dictionary<string, AttributeValue>> GetReportConfiguration(string reportName);
        public Task<Dictionary<string, AttributeValue>> ReadDataSourceTable(string dataSourceName, int dataSourceSeqNo);
        public List<Dictionary<string, object>> ParseListOfMaps(List<AttributeValue> list);
        public object FormatAttributeValue(AttributeValue value);
    }
}
