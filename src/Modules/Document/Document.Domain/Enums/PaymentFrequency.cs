namespace Document.Domain.Enums;

public enum PaymentFrequency
{
    Monthly,
    Quarterly,
    SemiAnnually,
    Annually,
    // Appended (not reordered) so existing persisted int values stay stable.
    Bimonthly // every 2 months
}
