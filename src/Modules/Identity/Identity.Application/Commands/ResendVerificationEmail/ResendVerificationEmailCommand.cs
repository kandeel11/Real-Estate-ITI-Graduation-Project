using MediatR;

namespace Identity.Application.Commands.ResendVerificationEmail;

public record ResendVerificationEmailCommand(string Email) : IRequest;
