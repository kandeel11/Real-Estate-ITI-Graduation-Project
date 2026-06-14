using AI.Application.DTOs;
using MediatR;

namespace AI.Application.Features.PropertyDescription.Queries.GetLatestDescriptionByProperty;

/// <summary>
/// Returns the most recent AI description generation for a property (or null if none yet).
/// Used after POST /properties/{id}/request-ai-description to fetch the draft for review/accept.
/// </summary>
public record GetLatestDescriptionByPropertyQuery(Guid PropertyId) : IRequest<DescriptionDraftDto?>;
