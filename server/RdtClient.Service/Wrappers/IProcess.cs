using System.Diagnostics;

namespace RdtClient.Service.Wrappers;

public interface IProcess : IDisposable
{
    event EventHandler<String?>? OutputDataReceived;
    event EventHandler<String?>? ErrorDataReceived;

    public ProcessStartInfo StartInfo { get; set; }

    void BeginOutputReadLine();
    void BeginErrorReadLine();
    Boolean WaitForExit(Int32 milliseconds);
    void Start();
}
