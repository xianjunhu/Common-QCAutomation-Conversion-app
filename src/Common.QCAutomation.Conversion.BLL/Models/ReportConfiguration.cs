using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Models
{
    public class ReportConfiguration
    {
        public string ReportName { get; set; } = string.Empty;
        //public string DataSourceName { get; set; } = string.Empty;
        public string GlueWorkFlow { get; set; } = string.Empty;
        public string S3Bucket { get; set; } = string.Empty;
        public string S3LandingZone { get; set; } = string.Empty;
        public string S3UploadFolder { get; set; } = string.Empty;
        public List<DataSourceFilePath> DataSourceFilePathList { get; set; } = new List<DataSourceFilePath>();
    }

    public class DataSourceFilePath
    {
        public string DataSourceName { get; set; } = string.Empty;
        public int DataSourceSeqNo { get; set; }
        public string ExcludeFiles { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string PhaseEnabled { get; set; } = string.Empty;
        public string IncludeFiles { get; set; } = string.Empty;
        public string DataSourcePhase { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

    }
}
