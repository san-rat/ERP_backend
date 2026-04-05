using System.Security.Cryptography;

namespace AdminService.Services;

public sealed class PasswordGenerator : IPasswordGenerator
{
    private const string AllowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$%&*?";

    public string Generate()
    {
        Span<char> password = stackalloc char[16];
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        for (var i = 0; i < password.Length; i++)
        {
            password[i] = AllowedChars[bytes[i] % AllowedChars.Length];
        }

        return new string(password);
    }
}
