// Identity.Application/Features/Auth/Otp/VerifyOtp/VerifyOtpCommand.cs

using MediatR;

namespace Identity.Application.Features.Auth.Otp.VerifyOtp;

public sealed record VerifyOtpCommand(string Email, string Code)
    : IRequest<VerifyOtpResponse>;

public sealed record VerifyOtpResponse(
    bool Success,
    string Message);