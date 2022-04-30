namespace RdtClient.Service.Services;

/// <summary>
///     Class for streaming data with throttling support.
///     Taken from Downloader: https://github.com/bezzad/Downloader
/// </summary>
public class ThrottledStream : Stream
{
    public Int64 Speed => (Int64)_bandwidth.AverageSpeed;

    private Bandwidth _bandwidth;
    private Int64 _bandwidthLimit;
    private readonly Stream _baseStream;

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:ThrottledStream" /> class.
    /// </summary>
    /// <param name="baseStream">The base stream.</param>
    /// <param name="bandwidthLimit">The maximum bytes per second that can be transferred through the base stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when <see cref="baseStream" /> is a null reference.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="BandwidthLimit" /> is a negative value.</exception>
    public ThrottledStream(Stream baseStream, Int64 bandwidthLimit)
    {
        if (bandwidthLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bandwidthLimit),
                                                  bandwidthLimit,
                                                  "The maximum number of bytes per second can't be negative.");
        }

        _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        BandwidthLimit = bandwidthLimit;
    }

    public static Int64 Infinite => Int64.MaxValue;

    /// <summary>
    ///     Bandwidth Limit (in B/s)
    /// </summary>
    /// <value>The maximum bytes per second.</value>
    public Int64 BandwidthLimit
    {
        get => _bandwidthLimit;
        set
        {
            _bandwidthLimit = value <= 0 ? Infinite : value;
            _bandwidth ??= new Bandwidth();
            _bandwidth.BandwidthLimit = _bandwidthLimit;
        }
    }

    /// <inheritdoc />
    public override Boolean CanRead => _baseStream.CanRead;

    /// <inheritdoc />
    public override Boolean CanSeek => _baseStream.CanSeek;

    /// <inheritdoc />
    public override Boolean CanWrite => _baseStream.CanWrite;

    /// <inheritdoc />
    public override Int64 Length => _baseStream.Length;

    /// <inheritdoc />
    public override Int64 Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }
        
    /// <inheritdoc />
    public override void Flush()
    {
        _baseStream.Flush();
    }

    /// <inheritdoc />
    public override Int64 Seek(Int64 offset, SeekOrigin origin)
    {
        return _baseStream.Seek(offset, origin);
    }

    /// <inheritdoc />
    public override void SetLength(Int64 value)
    {
        _baseStream.SetLength(value);
    }

    /// <inheritdoc />
    public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
    {
        Throttle(count).Wait();

        return _baseStream.Read(buffer, offset, count);
    }

    public override async Task<Int32> ReadAsync(Byte[] buffer,
                                                Int32 offset,
                                                Int32 count,
                                                CancellationToken cancellationToken)
    {
        await Throttle(count).ConfigureAwait(false);

        return await _baseStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Write(Byte[] buffer, Int32 offset, Int32 count)
    {
        Throttle(count).Wait();
        _baseStream.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    public override async Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
    {
        await Throttle(count).ConfigureAwait(false);
        await _baseStream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    }

    private async Task Throttle(Int32 transmissionVolume)
    {
        // Make sure the buffer isn't empty.
        if (BandwidthLimit > 0 && transmissionVolume > 0)
        {
            // Calculate the time to sleep.
            _bandwidth.CalculateSpeed(transmissionVolume);

            await Sleep(_bandwidth.PopSpeedRetrieveTime()).ConfigureAwait(false);
        }
    }

    private static async Task Sleep(Int32 time)
    {
        if (time > 0)
        {
            await Task.Delay(time).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override String ToString()
    {
        return _baseStream.ToString();
    }
}

internal class Bandwidth
{
    private const Double OneSecond = 1000; // millisecond
    private Int64 _count;
    private Int32 _lastSecondCheckpoint;
    private Int64 _lastTransferredBytesCount;
    private Int32 _speedRetrieveTime;

    public Bandwidth()
    {
        BandwidthLimit = Int64.MaxValue;
        Reset();
    }

    public Double Speed { get; private set; }
    public Double AverageSpeed { get; private set; }
    public Int64 BandwidthLimit { get; set; }

    public void CalculateSpeed(Int64 receivedBytesCount)
    {
        var elapsedTime = (Environment.TickCount - _lastSecondCheckpoint) + 1;
        receivedBytesCount = Interlocked.Add(ref _lastTransferredBytesCount, receivedBytesCount);
        var momentSpeed = (receivedBytesCount * OneSecond) / elapsedTime; // B/s

        if (OneSecond < elapsedTime)
        {
            Speed = momentSpeed;
            AverageSpeed = ((AverageSpeed * _count) + Speed) / (_count + 1);
            _count++;
            SecondCheckpoint();
        }

        if (momentSpeed >= BandwidthLimit)
        {
            var expectedTime = (receivedBytesCount * OneSecond) / BandwidthLimit;
            Interlocked.Add(ref _speedRetrieveTime, (Int32) expectedTime - elapsedTime);
        }
    }

    public Int32 PopSpeedRetrieveTime()
    {
        return Interlocked.Exchange(ref _speedRetrieveTime, 0);
    }

    public void Reset()
    {
        SecondCheckpoint();
        _count = 0;
        Speed = 0;
        AverageSpeed = 0;
    }

    private void SecondCheckpoint()
    {
        Interlocked.Exchange(ref _lastSecondCheckpoint, Environment.TickCount);
        Interlocked.Exchange(ref _lastTransferredBytesCount, 0);
    }
}