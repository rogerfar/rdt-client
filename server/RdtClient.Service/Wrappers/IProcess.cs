using System.Diagnostics;

namespace RdtClient.Service.Wrappers;

public interface IProcess : IDisposable
{
    public ProcessStartInfo StartInfo { get; set; }
    event EventHandler<String?>? OutputDataReceived;
    event EventHandler<String?>? ErrorDataReceived;

    void BeginOutputReadLine();
    void BeginErrorReadLine();
    Boolean WaitForExit(Int32 milliseconds);
    void Start();
}
