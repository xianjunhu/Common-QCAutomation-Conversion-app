using Amazon.Glue;
using Amazon.Glue.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Common.QCAutomation.Conversion.BLL.Service;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Class;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Interface;
using Common.QCAutomation.Conversion.BLL.Generic.Constants;
using Common.QCAutomation.Conversion.BLL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.QCAutomation.Conversion.BLL.Generic.Enums;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Class
{
    public class S3Client : IS3Client
    {
        AmazonS3Client? amazonS3Client = null;
        private readonly ILogger<IS3Client> _logger;
        public readonly IConfiguration _configuration;
        //private readonly IZipProcessor _zipProcessor;

        public S3Client(ILogger<IS3Client> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            // _zipProcessor = zipProcessor;
        }

        public AmazonS3Client GetAmazonS3Client()
        {
            var accessKey = _configuration["AWS:AccessKeyS3"];
            var secretKey = _configuration["AWS:SecretKeyS3"];
            if (amazonS3Client == null)
            {
                if (accessKey == null || secretKey == null)
                {
                    amazonS3Client = new AmazonS3Client();
                }
                else
                {
                    amazonS3Client = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.CACentral1);
                }
            }
            return amazonS3Client;

        }

        public async Task<bool> CheckFileInS3(string bucketName, string fileName)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = fileName
                };
                await GetAmazonS3Client().GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }
                throw; // Re-throw other exceptions
            }
        }
        public async Task<DataSourceProcessedResult> ProcessCurrentFileOneDateS3Async(DataSourceFilePath dataSourceFilePath, ReportConfiguration reportConfiguration, string s3Bucket, string filePrefix, DateTime startDate, DateTime endDate)
        {
            // Find the most recently modified file with the given file name prefix from the Client Site S3 Bucket                               
            string fileKey = await FindLatestS3File(s3Bucket, filePrefix);
            if (string.IsNullOrEmpty(fileKey))
            {
                _logger.LogInformation($"             >> No file found with the name prefix: '{filePrefix}' from the Client Site S3 bucket '{s3Bucket}'");
                return DataSourceProcessedResult.FileNotFound;
            }

            var metadata = await GetAmazonS3Client().GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = s3Bucket,
                Key = fileKey
            });
            _logger.LogInformation($"             >> Get the Object Metadata successfully for: {fileKey}");


            if (metadata.StorageClass == S3StorageClass.Glacier ||
                metadata.StorageClass == S3StorageClass.DeepArchive)
            {
                _logger.LogInformation($"             >> The last modified file has been moved in Glacier: {fileKey}");
                return DataSourceProcessedResult.FileMigrated;
            }

            var zipObject = await GetAmazonS3Client().GetObjectAsync(s3Bucket, fileKey);
            _logger.LogInformation($"             >> Read the S3 file successfully: {fileKey}");

            using (var zipStream = zipObject.ResponseStream)
            {
                if (zipStream == Stream.Null)
                {
                    _logger.LogInformation("             >> Failed to read ZIP file...zipSteam is null!");
                    return DataSourceProcessedResult.FileNotFound;
                }
                await ProcessZipStream(zipStream, dataSourceFilePath, reportConfiguration, startDate, endDate);
            }
            return DataSourceProcessedResult.FilesConverted;
        }

        // Upload the converted .csv files to the S3 landing zone
        public async Task UploadToS3Async(string bucketName, string csvContent, string key)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent)))
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = "text/csv"
                };
                await GetAmazonS3Client().PutObjectAsync(request);
            }
        }

        public async Task<string> FindLatestS3File(string bucketName, string filePrefix)
        {
            var fileList = new List<S3Object>();
            string continuationToken = null;

            int filesCount = 0;
            do
            {
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    ContinuationToken = continuationToken
                };

                var response = await GetAmazonS3Client().ListObjectsV2Async(listRequest);
                _logger.LogInformation($"             >> Retrieved {response.S3Objects.Count} objects from the Client Site S3 Bucket");

                fileList.AddRange(response.S3Objects
                    .Where(obj => obj.Key.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    .Where(obj => Path.GetFileName(obj.Key).StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase)));

                continuationToken = response.NextContinuationToken;
                _logger.LogInformation(continuationToken != null
                    ? $"             >> More objects to retrieve..."
                    : "             >> No more objects to retrieve from the Client Site S3 Bucket");

                filesCount += response.S3Objects.Count;
            } while (continuationToken != null);

            if (!fileList.Any()) return string.Empty;

            var selectedFile = fileList.OrderByDescending(obj => obj.LastModified).First();
            _logger.LogInformation($"             >> The most recent modified file being identified: {selectedFile.Key}, LastModified: {selectedFile.LastModified}");
            return selectedFile.Key;
        }

        public async Task ProcessZipStream(Stream zipStream, DataSourceFilePath dataSourceFilePath, ReportConfiguration reportConfiguration, DateTime startDate, DateTime? endDate = null)
        {
            string[]? includeFilter = null, excludeFilter = null;
            string includeFileName = string.Empty;

            if (!string.IsNullOrEmpty(dataSourceFilePath.IncludeFiles))
            {
                includeFilter = dataSourceFilePath.IncludeFiles.Split('*');
                includeFileName = includeFilter[0] +
                         startDate.ToString("yyyyMMdd").Substring(2) + includeFilter[1];
            }
            if (!string.IsNullOrEmpty(dataSourceFilePath.ExcludeFiles))
            {
                excludeFilter = dataSourceFilePath.ExcludeFiles.Split('*');
            }

            // Process ZIP contents
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                if (dataSourceFilePath.DataSourceName.Equals("S3"))
                {
                    var stTxtFiles = zipArchive.Entries.Where(e => e.Name.StartsWith("st", StringComparison.OrdinalIgnoreCase) &&
                                                             e.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
                    if (stTxtFiles.Any())
                    {
                        await ConvertToCsvAsync(stTxtFiles.Last(), reportConfiguration.S3UploadFolder, reportConfiguration.S3Bucket, startDate, endDate);
                    }
                }
                else
                {
                    foreach (var entry in zipArchive.Entries)
                    {
                        // Apply any filtering logic here to process specific files only
                        // E.g. if (entry.Name.StartsWith("AU"))

                        if (dataSourceFilePath.ExcludeFiles != null && excludeFilter != null)
                        {
                            if (Path.GetExtension(entry.Name).Contains(excludeFilter[1]))
                            {
                                _logger.LogInformation($"             >> Skip the file: {entry.Name}");
                            }
                            else
                            {
                                await ConvertToCsvAsync(entry, reportConfiguration.S3UploadFolder, reportConfiguration.S3Bucket);
                            }
                        }
                        if (dataSourceFilePath.IncludeFiles != null && includeFilter != null)
                        {
                            if (Path.GetFileName(entry.Name).Contains(includeFileName))
                            {
                                await ConvertToCsvAsync(entry, reportConfiguration.S3UploadFolder, reportConfiguration.S3Bucket);
                            }
                            else
                            {
                                _logger.LogInformation($"             >> Skip the file: {entry.Name}");
                            }
                        }
                    }
                }
            }
        }

        public async Task ConvertToCsvAsync(ZipArchiveEntry entry, string s3UploadFolder, string s3BucketName, DateTime? startDate = null, DateTime? endDate = null)
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
                    if (entryName.StartsWith("st") &&
                        entryExtension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
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

                    string csvFileName;
                    if (startDate != null && endDate != null)
                    {
                        foreach (DateTime currentDate in QCAutomationService.EachCalendarDay((DateTime)startDate, (DateTime)endDate))
                        {
                            csvFileName = "ST" + currentDate.ToString("yyyyMMdd").Substring(2) + ".DAT" + ".csv";
                            _logger.LogInformation($"                - Convert {entry.Name} to {csvFileName}");

                            string csvS3Key = s3UploadFolder + csvFileName;
                            await UploadToS3Async(s3BucketName, csvContent, csvS3Key);
                            _logger.LogInformation($"                - Upload {csvFileName} to {csvS3Key}");
                            _logger.LogInformation($"               --------------------------------------------------------------------------");
                        }
                    }
                    else
                    {
                        csvFileName = Path.GetFileNameWithoutExtension(entry.Name) + ".csv";
                        _logger.LogInformation($"                - Convert {entry.Name} to {csvFileName}");

                        // Upload CSV to S3
                        string csvS3Key = s3UploadFolder + Path.GetFileName(entry.Name) + ".csv";
                        await UploadToS3Async(s3BucketName, csvContent, csvS3Key);
                        _logger.LogInformation($"                - Upload {csvFileName} to {csvS3Key}");
                        _logger.LogInformation($"               --------------------------------------------------------------------------");
                    }
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