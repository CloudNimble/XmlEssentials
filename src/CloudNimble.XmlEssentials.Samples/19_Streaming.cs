using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Shapes;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 19: serializes to and from streams and pipes, one document or one value at a time.
    /// </summary>
    public static class Streaming
    {

        #region Public Methods

        /// <summary>
        /// Writes one document to a stream and reads it back without blocking on the stream.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        /// <returns>A task that completes when the sample has run.</returns>
        public static async Task Run(TextWriter output)
        {
            var reading = new SensorReading { Sensor = "boiler", Value = 71.5 };

            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsync(stream, reading);
            output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));

            stream.Position = 0;
            SensorReading? copy = await XmlSerializer.DeserializeAsync<SensorReading>(stream);
            output.WriteLine($"{copy!.Sensor}: {copy.Value}");
        }

        /// <summary>
        /// Stops reading a sequence when a cancellation token is canceled.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        /// <returns>A task that completes when the sample has run.</returns>
        public static async Task Cancel(TextWriter output)
        {
            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(stream, ReadSensor());
            stream.Position = 0;

            using var cancellation = new CancellationTokenSource();
            try
            {
                await foreach (SensorReading? reading in XmlSerializer.DeserializeAsyncEnumerable<SensorReading>(stream, cancellationToken: cancellation.Token))
                {
                    output.WriteLine($"{reading!.Sensor}: {reading.Value}");
                    cancellation.Cancel();
                }
            }
            catch (OperationCanceledException)
            {
                output.WriteLine("Canceled");
            }
        }

        /// <summary>
        /// Writes a sequence to a pipe and reads it from the other end.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        /// <returns>A task that completes when the sample has run.</returns>
        public static async Task UsePipe(TextWriter output)
        {
            var pipe = new Pipe();

            Task writing = WriteAndComplete(pipe.Writer);
            await foreach (SensorReading? reading in XmlSerializer.DeserializeAsyncEnumerable<SensorReading>(pipe.Reader, topLevelValues: false))
            {
                output.WriteLine($"{reading!.Sensor}: {reading.Value}");
            }

            await writing;
            await pipe.Reader.CompleteAsync();
        }

        /// <summary>
        /// Writes a sequence as the children of one document element, and reads the values back one at a time.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        /// <returns>A task that completes when the sample has run.</returns>
        public static async Task WriteSequence(TextWriter output)
        {
            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(stream, ReadSensor());
            output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));

            stream.Position = 0;
            await foreach (SensorReading? reading in XmlSerializer.DeserializeAsyncEnumerable<SensorReading>(stream))
            {
                output.WriteLine($"{reading!.Sensor}: {reading.Value}");
            }
        }

        /// <summary>
        /// Writes a sequence as one document per value, and reads the documents back one at a time.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        /// <returns>A task that completes when the sample has run.</returns>
        public static async Task WriteTopLevelValues(TextWriter output)
        {
            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(stream, ReadSensor(), topLevelValues: true);
            output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));

            stream.Position = 0;
            await foreach (SensorReading? reading in XmlSerializer.DeserializeAsyncEnumerable<SensorReading>(stream, topLevelValues: true))
            {
                output.WriteLine($"{reading!.Sensor}: {reading.Value}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Produces three readings, yielding to the caller between them as a real sensor feed does.
        /// </summary>
        /// <param name="cancellationToken">The token that stops the feed.</param>
        /// <returns>The readings.</returns>
        private static async IAsyncEnumerable<SensorReading> ReadSensor([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            double[] values = [71.5, 72.25, 73];
            foreach (double value in values)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return new SensorReading { Sensor = "boiler", Value = value };
            }
        }

        /// <summary>
        /// Writes the readings to a pipe and completes the writer so that the reader sees the end of the data.
        /// </summary>
        /// <param name="writer">The writing end of the pipe.</param>
        /// <returns>A task that completes when the pipe has been flushed and completed.</returns>
        private static async Task WriteAndComplete(PipeWriter writer)
        {
            await XmlSerializer.SerializeAsyncEnumerable(writer, ReadSensor(), topLevelValues: false);
            await writer.CompleteAsync();
        }

        #endregion

    }

}
