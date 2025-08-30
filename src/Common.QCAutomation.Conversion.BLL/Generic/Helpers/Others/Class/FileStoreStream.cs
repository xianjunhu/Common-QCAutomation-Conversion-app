using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.QCAutomation.Conversion.BLL.Generic.Helpers.Others.Class
{
    public class FileStoreStream : Stream
    {
        private ISMBFileStore _fileStore;
        private object? _fileHandle;
        private long _position;

        public FileStoreStream(ISMBFileStore fileStore, object fileHandle)
        {
            _fileStore = fileStore;
            _fileHandle = fileHandle;
            _position = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _position;
            set => _position = value;
        }
        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            byte[] data;
            NTStatus status = _fileStore.ReadFile(out data, _fileHandle, _position, count);
            if (status != NTStatus.STATUS_SUCCESS)
                return 0;

            Array.Copy(data, 0, buffer, offset, data.Length);
            _position += data.Length;
            return data.Length;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
                _position = offset;
            else if (origin == SeekOrigin.Current)
                _position += offset;
            else
                throw new NotSupportedException();
            return _position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] data = new byte[count];
            Array.Copy(buffer, offset, data, 0, count);
            NTStatus status = _fileStore.WriteFile(out _, _fileHandle, _position, data);
            if (status == NTStatus.STATUS_SUCCESS)
                _position += count;
        }

        protected override void Dispose(bool disposing)
        {
            if (_fileHandle != null)
            {
                _fileStore.CloseFile(_fileHandle);
                _fileHandle = null;
            }
            base.Dispose(disposing);
        }
    }
}
