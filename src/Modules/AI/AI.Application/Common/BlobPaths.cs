// AI.Application/Common/BlobPaths.cs
// Centralized path builder — all modules use this

namespace AI.Application.Common;

public static class BlobPaths
{
    // Leases module
    public static string LeasePdf(Guid leaseId)
        => $"leases/{leaseId}";

    // OwnershipVerification module
    public static string NationalId(Guid requestId)
        => $"ownership-verification/{requestId}/national-id";

    public static string OwnershipProof(Guid requestId)
        => $"ownership-verification/{requestId}/ownership-proof";

    // ContractRag module
    public static string ContractRag(Guid contractId)
        => $"contracts/{contractId}";
}