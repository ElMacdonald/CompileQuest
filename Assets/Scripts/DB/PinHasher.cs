using System.Security.Cryptography;
using System.Text;

// ================================================================
//  PinHasher.cs  —  NEW FILE
//  Hashes PINs using SHA-256 with a per-user salt.
//  The raw PIN is never stored anywhere.
// ================================================================

public static class PinHasher
{
    /// <summary>Hash a PIN with the userId as salt.</summary>
    public static string Hash(string pin, string salt)
    {
        string combined = pin + "_" + salt;
        using SHA256 sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
        StringBuilder sb = new StringBuilder();
        foreach (byte b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    /// <summary>Check if a PIN matches a stored hash.</summary>
    public static bool Verify(string pin, string salt, string storedHash)
    {
        return Hash(pin, salt) == storedHash;
    }
}
