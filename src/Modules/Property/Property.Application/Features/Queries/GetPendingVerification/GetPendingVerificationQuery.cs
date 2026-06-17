using MediatR;
using Property.Application.DTOs;

namespace Property.Application.Features.Queries.GetPendingVerification;

public record GetPendingVerificationQuery : IRequest<List<MyPropertySummaryDto>>;
