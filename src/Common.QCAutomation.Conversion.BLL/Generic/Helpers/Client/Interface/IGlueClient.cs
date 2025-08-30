using Amazon.Glue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface
{
    public interface IGlueClient
    {
        public AmazonGlueClient GetAmazonGlueClient();
        public Task StartWorkflow(string workFlowName, DateTime startDate);
    }
}
