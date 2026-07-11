namespace Nexus.Application.Ports
{
    public interface IGatewaySessionFactory
    {
        IGatewaySession CreateSession(string connectionString, string credentials);
    }
}
