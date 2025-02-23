namespace RdtClient.Service.Helpers;

public interface IDelayProvider
{
    public Task Delay(Int32 milliseconds);
}
