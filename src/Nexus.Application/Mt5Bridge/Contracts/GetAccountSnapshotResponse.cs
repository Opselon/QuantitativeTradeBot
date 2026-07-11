using System.Text.Json.Serialization;

namespace Nexus.Application.Mt5Bridge.Contracts
{
    public class GetAccountSnapshotResponse
    {
        [JsonPropertyName("accountId")]
        public long AccountId { get; }

        [JsonPropertyName("broker")]
        public string Broker { get; }

        [JsonPropertyName("currency")]
        public string Currency { get; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; }

        [JsonPropertyName("equity")]
        public decimal Equity { get; }

        [JsonPropertyName("margin")]
        public decimal Margin { get; }

        [JsonPropertyName("freeMargin")]
        public decimal FreeMargin { get; }

        [JsonPropertyName("leverage")]
        public int Leverage { get; }

        [JsonPropertyName("connectionHealth")]
        public string ConnectionHealth { get; }

        [JsonConstructor]
        public GetAccountSnapshotResponse(
            long accountId,
            string broker,
            string currency,
            decimal balance,
            decimal equity,
            decimal margin,
            decimal freeMargin,
            int leverage,
            string connectionHealth)
        {
            AccountId = accountId;
            Broker = broker;
            Currency = currency;
            Balance = balance;
            Equity = equity;
            Margin = margin;
            FreeMargin = freeMargin;
            Leverage = leverage;
            ConnectionHealth = connectionHealth;
        }
    }
}
