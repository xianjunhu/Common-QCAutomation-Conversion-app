using Common.QCAutomation.Conversion.BLL.Models;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Interface
{
    public interface ICSVConverter
    {
        public Task ConvertToCsvAsync(ZipArchiveEntry entry, string s3UploadFolder, string s3BucketName);
    }
}
