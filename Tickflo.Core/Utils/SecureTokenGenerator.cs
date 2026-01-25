namespace Tickflo.Core.Utils;

using System.Security.Cryptography;
using System.Text;

public static class SecureTokenGenerator
{
    public static string GenerateToken(int byteLength = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        var tokenBuilder = new StringBuilder(byteLength * 2);
        foreach (var value in bytes)
        {
            tokenBuilder.AppendFormat("{0:x2}", value);
        }
        return tokenBuilder.ToString();
    }
}
