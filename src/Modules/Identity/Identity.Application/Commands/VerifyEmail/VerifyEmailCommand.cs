using MediatR;

namespace Identity.Application.Commands.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest<bool>;
