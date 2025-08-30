using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Enums
{
    public enum ErrorNumbers
    {
        None = 0,
        RequestError = 10,
        AuthorizationError = 20,
        DbError = 90,
        GenericError = 100,
        GenericJobError = 101,
        JobCancelled = 102,
    }

    public enum Status
    {
        Started = 1,
        InProgress = 2,
        Success = 3,
        Failed = -3,
    }

    public enum FileScanUpdateOption
    {
        CleanForToday,
        FileNotAvailable,
        CurrentFileConverted,
        AllFilesConverted
    }
    public enum DataSourceProcessedResult
    {
        FileNotFound,
        FileMigrated,
        FilesConverted
    }
}
