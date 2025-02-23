using System.Diagnostics;

namespace RdtClient.Service.Wrappers;

public class Process : IProcess
{
    private readonly System.Diagnostics.Process _process = new();

    public ProcessStartInfo StartInfo
    {
        get => _process.StartInfo;
        set => _process.StartInfo = value;
    }

    public event EventHandler<String?>? OutputDataReceived;
    public event EventHandler<String?>? ErrorDataReceived;

    public void Dispose()
    {
        _process.Dispose();
    }

    public void BeginOutputReadLine()
    {
        _process.OutputDataReceived += (sender, args) => OutputDataReceived?.Invoke(sender, args.Data);
        _process.BeginOutputReadLine();
    }

    public void BeginErrorReadLine()
    {
        _process.ErrorDataReceived += (sender, args) => ErrorDataReceived?.Invoke(sender, args.Data);
        _process.BeginErrorReadLine();
    }

    public Boolean WaitForExit(Int32 milliseconds)
    {
        return _process.WaitForExit(milliseconds);
    }

    public void Start()
    {
        _process.Start();
    }
}
