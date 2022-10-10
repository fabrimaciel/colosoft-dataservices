using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace System.Net.Http
{
    [ExcludeFromCodeCoverage]
    internal class PushStreamContent : HttpContent
    {
        private readonly Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable;

        public PushStreamContent(Action<Stream, HttpContent, TransportContext?> onStreamAvailable)
            : this(Taskify(onStreamAvailable), (MediaTypeHeaderValue?)null)
        {
        }

        public PushStreamContent(Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable)
            : this(onStreamAvailable, (MediaTypeHeaderValue?)null)
        {
        }

        public PushStreamContent(Action<Stream, HttpContent, TransportContext?> onStreamAvailable, string mediaType)
            : this(Taskify(onStreamAvailable), new MediaTypeHeaderValue(mediaType))
        {
        }

        public PushStreamContent(Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable, string mediaType)
            : this(onStreamAvailable, new MediaTypeHeaderValue(mediaType))
        {
        }

        public PushStreamContent(Action<Stream, HttpContent, TransportContext?> onStreamAvailable, MediaTypeHeaderValue? mediaType)
            : this(Taskify(onStreamAvailable), mediaType)
        {
        }

        public PushStreamContent(Func<Stream, HttpContent, TransportContext?, Task> onStreamAvailable, MediaTypeHeaderValue? mediaType)
        {
            this.onStreamAvailable = onStreamAvailable ?? throw new ArgumentNullException(nameof(onStreamAvailable));
            this.Headers.ContentType = mediaType ?? new MediaTypeHeaderValue("application/octet-stream");
        }

        private static Func<Stream, HttpContent, TransportContext?, Task> Taskify(
            Action<Stream, HttpContent, TransportContext?> onStreamAvailable)
        {
            if (onStreamAvailable == null)
            {
                throw new ArgumentNullException(nameof(onStreamAvailable));
            }

            return (Stream stream, HttpContent content, TransportContext? transportContext) =>
            {
                onStreamAvailable(stream, content, transportContext);
                return Task.FromResult<AsyncVoid>(default);
            };
        }

        private struct AsyncVoid
        {
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var serializeToStreamTask = new TaskCompletionSource<bool>();

            var wrappedStream = new CompleteTaskOnCloseStream(stream, serializeToStreamTask);
            await this.onStreamAvailable(wrappedStream, this, context);

            // wait for wrappedStream.Close/Dispose to get called.
            await serializeToStreamTask.Task;
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        private sealed class CompleteTaskOnCloseStream : DelegatingStream
        {
            private readonly TaskCompletionSource<bool> serializeToStreamTask;

            public CompleteTaskOnCloseStream(Stream innerStream, TaskCompletionSource<bool> serializeToStreamTask)
                : base(innerStream)
            {
                Contract.Assert(serializeToStreamTask != null);
                this.serializeToStreamTask = serializeToStreamTask ?? throw new ArgumentNullException(nameof(serializeToStreamTask));
            }

            protected override void Dispose(bool disposing)
            {
                this.serializeToStreamTask.TrySetResult(true);
            }
        }
    }
}
