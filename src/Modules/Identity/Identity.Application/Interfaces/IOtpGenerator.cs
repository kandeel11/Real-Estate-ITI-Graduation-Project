// Identity.Application/Interfaces/IOtpGenerator.cs

namespace Identity.Application.Interfaces;

public interface IOtpGenerator
{
    string Generate6Digit();
}