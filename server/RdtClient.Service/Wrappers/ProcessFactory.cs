namespace RdtClient.Service.Wrappers;

public class ProcessFactory: IProcessFactory
{
    public IProcess NewProcess()
    {
        return new Process();
    }
}
