using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Models
{
    public class ReportDataSource
    {
        public string DataSourceName { get; set; } = string.Empty;
        public int DataSourceSeqNo { get; set; }
        public string AuthenticationDomain { get; set; } = string.Empty;
        public string AuthenticationSecretName { get; set; } = string.Empty;
        public string DnsOrBucketName { get; set; } = string.Empty;
        public string ShareName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;


    }
}
