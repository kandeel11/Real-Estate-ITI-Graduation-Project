// Identity.Application/Features/Auth/Otp/SendOtp/SendOtpCommand.cs

using MediatR;

namespace Identity.Application.Features.Auth.Otp.SendOtp;

public sealed record SendOtpCommand(string Email) : IRequest<SendOtpResponse>;

public sealed record SendOtpResponse(
    bool Success,
    string Message,
    int ExpiresInSeconds);