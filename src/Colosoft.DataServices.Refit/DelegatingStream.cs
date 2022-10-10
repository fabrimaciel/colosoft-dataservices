using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    [ExcludeFromCodeCoverage]
    internal abstract class DelegatingStream : Stream
    {
        protected DelegatingStream(Stream innerStream)
        {
            this.InnerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        }

        protected Stream InnerStream { get; private set; }

        public override bool CanRead
        {
            get { return this.InnerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return this.InnerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return this.InnerStream.CanWrite; }
        }

        public override long Length
        {
            get { return this.InnerStream.Length; }
        }

        public override long Position
        {
            get { return this.InnerStream.Position; }
            set { this.InnerStream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return this.InnerStream.ReadTimeout; }
            set { this.InnerStream.ReadTimeout = value; }
        }

        public override bool CanTimeout
        {
            get { return this.InnerStream.CanTimeout; }
        }

        public override int WriteTimeout
        {
            get { return this.InnerStream.WriteTimeout; }
            set { this.InnerStream.WriteTimeout = value; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.InnerStream.Dispose();
            }

            base.Dispose(disposing);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.InnerStream.Seek(offset, origin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.InnerStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.InnerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            return this.InnerStream.ReadByte();
        }

        public override void Flush()
        {
            this.InnerStream.Flush();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return this.InnerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return this.InnerStream.FlushAsync(cancellationToken);
        }

        public override void SetLength(long value)
        {
            this.InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.InnerStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            this.InnerStream.WriteByte(value);
        }
    }
}
