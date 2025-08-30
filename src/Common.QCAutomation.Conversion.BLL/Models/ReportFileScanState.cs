using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace Common.QCAutomation.Conversion.BLL.Models
{
    public static class ReportFileScanState
    {
        //public string ReportName { get; set; } = string.Empty;
        //public bool AllFilesConverted { get; set; }
        //public List<string> ConvertedFilesList { get; set; } = new List<string>();
        //public string CurrentDate { get; set; } = string.Empty;
        //public string LastProcessedDate { get; set; } = string.Empty;
        //public int LastRetriedCount { get; set; }
        //public string LastScanTimestamp { get; set; } = string.Empty;
        //public int RetryCount { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public static Dictionary<string, AttributeValue> prevItem;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    }
}