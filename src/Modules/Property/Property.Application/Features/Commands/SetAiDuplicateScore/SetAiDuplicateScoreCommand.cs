using MediatR;

namespace Property.Application.Features.Commands.SetAiDuplicateScore;

public record SetAiDuplicateScoreCommand(Guid PropertyId, decimal Confidence) : IRequest;
