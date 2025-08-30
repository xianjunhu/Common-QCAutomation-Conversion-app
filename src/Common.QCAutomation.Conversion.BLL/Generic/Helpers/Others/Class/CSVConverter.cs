using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Interface;
using Common.QCAutomation.Conversion.BLL.Models;
using Microsoft.Extensions.Logging;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Class
{
    public class CSVConverter : ICSVConverter
    {
        private readonly ILogger<ICSVConverter> _logger;
        private readonly IS3Client _s3Client;
        public CSVConverter(ILogger<ICSVConverter> logger, IS3Client s3Client)
        {
            _logger = logger;
            _s3Client = s3Client;
        }
        public async Task ConvertToCsvAsync(ZipArchiveEntry entry, string s3UploadFolder, string s3BucketName)
        {
            try
            {
                _logger.LogInformation($"             >> Process the ZIP entry: {entry.FullName}");

                // Read entry content
                using (var entryStream = entry.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    string csvContent;
                    string content = await reader.ReadToEndAsync();
                    //_logger.LogInformation($"Content read from the file {Path.GetFileNameWithoutExtension(entry.Name)}: {content}");

                    // Assume content is delimited (e.g., JSON or pipe-delimited). Customize as needed.
                    string entryName = Path.GetFileName(entry.Name);
                    string entryExtension = Path.GetExtension(entry.Name);
                    if (entryExtension.Equals(".DAT", StringComparison.OrdinalIgnoreCase))
                    {
                        csvContent = ConvertToCsvFormat(content, FileParser.StColumns);
                    }
                    else
                        if (entryExtension.Equals(".DEM", StringComparison.OrdinalIgnoreCase))
                    {
                        csvContent = ConvertToCsvFormat(content, FileParser.DemColumns);
                    }
                    else
                    {
                        if (entryName.StartsWith("PB", StringComparison.OrdinalIgnoreCase))
                        {
                            csvContent = ConvertToCsvFormat(content, FileParser.PbSwdColumns);
                        }
                        else
                        {
                            csvContent = ConvertToCsvFormat(content, FileParser.AuSwdColumns);
                        }
                    }

                    string csvFileName = Path.GetFileNameWithoutExtension(entry.Name) + ".csv";
                    _logger.LogInformation($"                - Convert {entry.Name} to {csvFileName}");

                    // Upload CSV to S3
                    string csvS3Key = s3UploadFolder + Path.GetFileName(entry.Name) + ".csv";
                    await _s3Client.UploadToS3Async(s3BucketName, csvContent, csvS3Key);
                    _logger.LogInformation($"                - Upload {csvFileName} to {csvS3Key}x");
                    _logger.LogInformation($"               --------------------------------------------------------------------------");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"!!!ERROR!!! Error converting {entry.FullName}: {ex.Message}", ex);
            }
        }

        // Convert .txt to .csv (simple example: assumes space-separated values)
        public static string ConvertToCsvFormat(string txtContent, List<(int Position, int Length, string Header)> Columns)
        {
            var lines = txtContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var headers = string.Join(",", Columns.ConvertAll(col => $"\"{col.Header}\""));
            string csvLine;

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(headers);

            foreach (var line in lines)
            {
                List<string> values = new List<string>();
                foreach (var (position, length, _) in Columns)
                {
                    // Extract substring and trim
                    string value = position < line.Length
                        ? line.Substring(position, Math.Min(length, line.Length - position)).Trim()
                        : string.Empty;
                    values.Add(value);
                    //values.Add($"\"{value.Replace("\"", "\"\"")}\""); // to add double quote if needed.
                }
                csvLine = string.Join(",", values);
                csvBuilder.AppendLine(csvLine);
            }
            return csvBuilder.ToString();
        }
    }
}
