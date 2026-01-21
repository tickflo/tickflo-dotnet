namespace Tickflo.Core.Utils;

using System.Security.Cryptography;
using System.Text;

public static class TokenGenerator
{
    public static string GenerateToken(int byteLength = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        var sb = new StringBuilder(byteLength * 2);
        foreach (var b in bytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }
        return sb.ToString();
    }
}
