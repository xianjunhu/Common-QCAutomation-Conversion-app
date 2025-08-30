using Common.QCAutomation.Conversion.BLL.Generic.Enums;
using SMBLibrary.Client;
using SMBLibrary;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Common.QCAutomation.Conversion.BLL.Models;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Interface;
using Microsoft.Extensions.Configuration;
using System.IO;
using Amazon.FSx;
using Amazon.S3.Model;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Class;
using Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Interface;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Client.Class
{
    public class FSXClient : IFSXClient
    {
        private readonly ILogger<IFSXClient> _logger;
        public readonly IConfiguration _configuration;
        private readonly ICSVConverter _csvConverter;
        private readonly IS3Client _s3Client;
        private readonly IDynamoDBClient _dynamoDBClient;
        private SMB2Client smbClient;
        bool _fileNotFound = false;
        //public FSXClient(ILogger<IFSXClient> logger, IConfiguration configuration, ICSVConverter cSVConverter, IDynamoDBClient dynamoDBClient)
        public FSXClient(ILogger<IFSXClient> logger, IConfiguration configuration,
               ICSVConverter cSVConverter, IDynamoDBClient dynamoDBClient, IS3Client s3Client)
        {
            _logger = logger;
            _configuration = configuration;
            _csvConverter = cSVConverter;
            _s3Client = s3Client;
            _dynamoDBClient = dynamoDBClient;
        }
        // Login the current Data Source to fetch the current files required by the current Report
        public async Task<bool> ProcessCurrentFileOneDateFSxAsync(ReportDataSource reportDataSource, DataSourceFilePath dataSourceFilePath, ReportConfiguration reportConfiguration, DateTime currentDate)
        {
            smbClient = new SMB2Client();

            //bool connected = smbClient.Connect(reportDataSource.DnsOrBucketName, SMBTransportType.DirectTCPTransport);
            bool connected = await TryConnectFSxWithDiagnosticsAsync(reportDataSource.DnsOrBucketName);
            
            if (!connected)
            {
                throw new ApplicationException("!!!ERROR!!! Failed to connect to FSx SMB share.");
            }

            try
            {
                //Retrieve the username/password from Configuration to log in FSx SMB
                var userName = _configuration[reportDataSource.AuthenticationSecretName + ":secret:service_account_username"] ?? throw new ApplicationException($"!!!ERROR!!! Can't find the username for the secret name: secret:service_account_username");
                var password = _configuration[reportDataSource.AuthenticationSecretName + ":secret:service_account_password"] ?? throw new ApplicationException($"!!!ERROR!!! Can't find the username for the secret name: secret:service_account_username");

                var status = smbClient.Login(reportDataSource.AuthenticationDomain, userName, password);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new ApplicationException($"!!!ERROR!!! SMB login failed with Domain/Username/Password: {status} + {reportDataSource.AuthenticationDomain} + {reportDataSource.UserName} + ****");
                }

                var fileStore = smbClient.TreeConnect(reportDataSource.ShareName, out status);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new ApplicationException($"!!!ERROR!!! Tree connect failed: {status}");
                }

                // Read ZIP file from FSx
                _fileNotFound = false;
                using (var zipStream = await ReadFileFromFSxAsync(fileStore, dataSourceFilePath.FileName, reportConfiguration))
                {
                    if (_fileNotFound)
                    {
                        _logger.LogInformation($"            >> The current file {dataSourceFilePath.FileName} NOT available yet for the report: {reportConfiguration.ReportName}");
                        return true;
                    }
                    if (zipStream == Stream.Null)
                    {
                        _logger.LogInformation("             >> Failed to read ZIP file...zipSteam is null!");
                        return true;
                    }
                    await _s3Client.ProcessZipStream(zipStream, dataSourceFilePath, reportConfiguration, currentDate);
                }
            }
            finally
            {
                smbClient.Logoff();
                smbClient.Disconnect();
            }
            return _fileNotFound;
        }

        // Implementing a robust connection method with diagnostics (for troubleshooting) and retries mechanism
        private async Task<bool> TryConnectFSxWithDiagnosticsAsync(string fsxDnsName)
        {
            int maxAttempts = int.Parse(_configuration["FSxConnect:maxAttempts"]);
            int delayMilliseconds = int.Parse(_configuration["FSxConnect:DelayMilliseconds"]);
            bool connected = false;
            IPAddress[] hostAddresses;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                _logger.LogInformation($"[FSx Diagnostic] Attempt {attempt} connecting to {fsxDnsName}...");

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    // Step 1: DNS Resolution Test
                    try
                    {
                        hostAddresses = await Dns.GetHostAddressesAsync(fsxDnsName);
                        _logger.LogInformation($"[FSx Diagnostic] DNS resolved to: {hostAddresses[0]}");
                    }
                    catch (Exception dnsEx)
                    {
                        _logger.LogInformation($"[FSx Diagnostic] DNS resolution failed: {dnsEx.GetType().Name} - {dnsEx.Message}");
                        throw; // No point retrying SMB without DNS
                    }

                    // Step 2: TCP Connectivity Test (port 445 for SMB)
                    try
                    {
                        using (var tcpClient = new TcpClient())
                        {
                            //var connectTask = tcpClient.ConnectAsync(hostAddresses[0], 445);
                            var connectTask = tcpClient.ConnectAsync(fsxDnsName, 445);
                            var timeoutTask = Task.Delay(5000); // 5s TCP connect timeout

                            var completed = await Task.WhenAny(connectTask, timeoutTask);
                            if (completed == timeoutTask)
                                throw new TimeoutException("TCP connect to port 445 timed out.");
                        }
                        _logger.LogInformation("[FSx Diagnostic] TCP port 445 reachable.");
                    }
                    catch (Exception tcpEx)
                    {
                        _logger.LogInformation($"[FSx Diagnostic] TCP connection failed: {tcpEx.GetType().Name} - {tcpEx.Message}");
                        throw;
                    }

                    // Step 3: SMB Connection Attempt
                    //connected = smbClient.Connect(hostAddresses[0], SMBTransportType.DirectTCPTransport);
                    connected = smbClient.Connect(fsxDnsName, SMBTransportType.DirectTCPTransport);
                    
                    stopwatch.Stop();

                    if (connected)
                    {
                        _logger.LogInformation($"[FSx Diagnostic] SMB connection established in {stopwatch.ElapsedMilliseconds} ms.");
                        break;
                    }
                    else
                    {
                        _logger.LogInformation($"[FSx Diagnostic] SMB connect returned false after {stopwatch.ElapsedMilliseconds} ms.");
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogInformation($"[FSx Diagnostic] Attempt {attempt} failed after {stopwatch.ElapsedMilliseconds} ms.");
                    _logger.LogInformation($"[FSx Diagnostic] Exception: {ex.GetType().Name} - {ex.Message}");
                }

                if (!connected && attempt < maxAttempts)
                {
                    _logger.LogInformation($"[FSx Diagnostic] Retrying in {delayMilliseconds / 1000} seconds...");
                    await Task.Delay(delayMilliseconds);
                }
            }

            if (!connected)
            {
                _logger.LogInformation($"[FSx Diagnostic] All attempts {maxAttempts} times failed to connect to FSx SMB share.");
            }

            return connected;
        }
        private async Task<Stream> ReadFileFromFSxAsync(ISMBFileStore share, string filePath, ReportConfiguration reportConfiguration)
        {
            try
            {
                object fileHandle;
                FileStatus fileStatus;
                NTStatus status = share.CreateFile(out fileHandle, out fileStatus, filePath,
                    AccessMask.GENERIC_READ, 0, ShareAccess.Read, CreateDisposition.FILE_OPEN,
                    CreateOptions.FILE_NON_DIRECTORY_FILE, null);

                if (status == NTStatus.STATUS_OBJECT_NAME_NOT_FOUND)
                {
                    _logger.LogInformation($"            >> The current file {filePath} NOT available yet for the report: {reportConfiguration.ReportName}");

                    _fileNotFound = true;
                    //_reportFileNotFoundCnt++;//PA : Move this flag QCAutomationService class after the fuction => ProcessZipFileAsync call

                    return Stream.Null;
                }
                else if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new ApplicationException($"!!!ERROR!!! Failed to open file {filePath}: {status}");
                }

                FileInformation fileInfo;
                status = share.GetFileInformation(out fileInfo, fileHandle, FileInformationClass.FileStandardInformation);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    share.CloseFile(fileHandle);
                    throw new ApplicationException($"!!!ERROR!!! Failed to get file info for {filePath}: {status}");
                }

                var standardInfo = (FileStandardInformation)fileInfo;
                long fileSize = standardInfo.EndOfFile;
                _logger.LogInformation($"             >> File size: {fileSize} bytes");

                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStoreStream(share, fileHandle))
                {
                    byte[] buffer = new byte[8192]; // 8KB buffer
                    long bytesReadTotal = 0;

                    while (bytesReadTotal < fileSize)
                    {
                        int bytesToRead = (int)Math.Min(buffer.Length, fileSize - bytesReadTotal);
                        byte[] data;
                        status = share.ReadFile(out data, fileHandle, bytesReadTotal, bytesToRead);

                        if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
                        {
                            memoryStream.Dispose();
                            throw new ApplicationException($"!!!ERROR!!! Failed to read file {filePath} at offset {bytesReadTotal}: {status}");
                        }

                        if (data == null || data.Length == 0)
                        {
                            _logger.LogInformation($"!!!WARNING!!! No data read at offset {bytesReadTotal}");
                            break;
                        }
                        await memoryStream.WriteAsync(data, 0, data.Length);
                        bytesReadTotal += data.Length;
                    }
                    memoryStream.Position = 0;
                    return memoryStream;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"!!!ERROR!!! Error reading file {filePath}: {ex.Message}", ex);
            }
        }
    }
}
