using MediatR;

namespace AI.Application.Features.OwnershipVerification.Queries.GetPendingAdminReviews
{
    public record GetPendingAdminReviewsQuery(
        int Limit = 50
    ) : IRequest<List<AI.Application.Features.OwnershipVerification.DTOs.VerificationStatusDto>>;
}
