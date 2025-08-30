using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.QCAutomation.Conversion.BLL.Generic.Enums;
using Common.QCAutomation.Conversion.BLL.Models;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface
{
    public interface IS3Client
    {
        public Task UploadToS3Async(string bucketName, string csvContent, string key);
        public Task<DataSourceProcessedResult> ProcessCurrentFileOneDateS3Async(DataSourceFilePath dataSourceFilePath, ReportConfiguration reportConfiguration, string s3Bucket, string filePrefix, DateTime startDate, DateTime endDate);
        public Task<bool> CheckFileInS3(string bucketName, string fileName);

        public Task ProcessZipStream(Stream zipStream, DataSourceFilePath dataSourceFilePath, ReportConfiguration reportConfiguration, DateTime startDate, DateTime? endDate = null);

        public Task ConvertToCsvAsync(ZipArchiveEntry entry, string s3UploadFolder, string s3BucketName, DateTime? startDate = null, DateTime? endDate = null);

    }
}
