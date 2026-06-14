// Identity.Infrastructure/Email/OtpGenerator.cs

using Identity.Application.Interfaces;
using System.Security.Cryptography;

namespace Identity.Infrastructure.Email;

//public interface IOtpGenerator
//{
//    string Generate6Digit();
//}

public sealed class OtpGenerator : IOtpGenerator
{
    public string Generate6Digit()
    {
        // Cryptographically secure 6-digit OTP
        var bytes = RandomNumberGenerator.GetBytes(4);
        var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return value.ToString("D6");
    }
}