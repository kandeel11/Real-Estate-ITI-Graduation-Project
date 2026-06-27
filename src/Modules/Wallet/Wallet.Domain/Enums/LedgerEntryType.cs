using System.Text.Json.Serialization;

namespace Wallet.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LedgerEntryType
{
    Credit = 0,
    Debit = 1
}
