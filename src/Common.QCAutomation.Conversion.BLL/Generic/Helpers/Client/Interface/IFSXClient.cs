using Common.QCAutomation.Conversion.BLL.Models;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface
{
    public interface IFSXClient
    {
        public Task<bool> ProcessCurrentFileOneDateFSxAsync(ReportDataSource reportDataSource, DataSourceFilePath dataSourceFilePath, ReportConfiguration reportConfiguration, DateTime currentDate);
    }
}