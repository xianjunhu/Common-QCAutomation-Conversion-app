using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Service
{
    public interface IQCAutomationService
    {
        public Task<string> ProcessQCAutomationFiles(ReportRequest reportName, CancellationToken cancellationToken);
    }
}
