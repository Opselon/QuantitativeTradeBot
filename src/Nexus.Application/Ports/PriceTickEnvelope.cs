namespace Nexus.Application.Ports
{
    public record PriceTickEnvelope(string SymbolName, DateTime Timestamp, double Bid, double Ask, long SequenceNumber);
}
