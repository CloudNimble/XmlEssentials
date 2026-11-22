using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A read-only stream that hands out at most a fixed number of bytes per read, the way a network stream does.
    /// </summary>
    public sealed class TrickleStream : Stream
    {

        #region Fields

        private readonly int _chunk;
        private readonly byte[] _data;
        private int _position;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the number of bytes handed out so far.
        /// </summary>
        public int BytesRead => _position;

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the stream.
        /// </summary>
        /// <param name="data">The content.</param>
        /// <param name="chunk">The most bytes one read returns.</param>
        public TrickleStream(byte[] data, int chunk)
        {
            _data = data;
            _chunk = chunk;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = Math.Min(Math.Min(count, _chunk), _data.Length - _position);
            _data.AsSpan(_position, read).CopyTo(buffer.AsSpan(offset));
            _position += read;
            return read;
        }

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var read = Math.Min(Math.Min(buffer.Length, _chunk), _data.Length - _position);
            _data.AsMemory(_position, read).CopyTo(buffer);
            _position += read;
            return read;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #endregion

    }

}
