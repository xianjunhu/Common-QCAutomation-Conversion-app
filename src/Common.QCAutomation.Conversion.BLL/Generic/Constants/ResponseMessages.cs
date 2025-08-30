using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Constants
{
    internal class ResponseMessages
    {
    }
}

public static class ResponseMessages
{
    private static readonly Dictionary<string, ResponseMessage> _messages = new()
    {
        { "AllFilesConverted", new ResponseMessage("All files converted for the selected report and no Glue workflow specified", 200) },
        { "GlueFlowStarted", new ResponseMessage("All files converted for the selected report and the Glue workflow is started", 200) },
        { "AuthHeaderMissing", new ResponseMessage("Authorization header missing", 401) },
        { "InvalidToken", new ResponseMessage("Invalid Token", 401) },
        { "FileNotFound", new ResponseMessage("File not found", 404) },
        { "S3FileMigrated", new ResponseMessage("S3 File(Station Code) Migrated to Glacier", 404) },
        { "InvalidReportName", new ResponseMessage("Report not defined", 400) },
        { "InvalidDataPhase", new ResponseMessage("Invalid DataPhase", 400) },
        { "InvalidMethod", new ResponseMessage("HTTP Method Not Allowed", 405) },
        { "EmptyRequest", new ResponseMessage("Request body is empty or null", 400) },
        { "InvalidRequest", new ResponseMessage("Failed to retrieve the request payload", 400) },
        { "InvalidDateRange", new ResponseMessage("StartDate can not be greater than EndDate", 400) }
    };

    public static ResponseMessage Get(string key)
    {
        return _messages.TryGetValue(key, out var msg)
            ? msg
            : new ResponseMessage("Unknown error", 520);
    }
}

public class ResponseMessage
{
    public string Message { get; }
    public int StatusCode { get; }

    public ResponseMessage(string message, int statusCode)
    {
        Message = message;
        StatusCode = statusCode;
    }
}