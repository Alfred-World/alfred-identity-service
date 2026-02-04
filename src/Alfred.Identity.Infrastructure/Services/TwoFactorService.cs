using System.Security.Cryptography;
using System.Text;
using Alfred.Identity.Domain.Abstractions.Services;

namespace Alfred.Identity.Infrastructure.Services;

public class TwoFactorService : ITwoFactorService
{
    private const int Period = 30; // 30 seconds
    private const int Digits = 6;

    public string GenerateSecret()
    {
        // Generate random bytes and base32 encode (simplified, or just use base64 for internal storage but OTP usually uses Base32)
        // For simplicity in this zero-dep implementation, let's use Base32-like char set or standard implementation if possible.
        // Or cleaner: Use a simple custom Base32 generator.
        
        var bytes = new byte[20]; // 160 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base32Encode(bytes);
    }

    public string GenerateQrCodeUri(string email, string secret)
    {
        // otpauth://totp/Alfred.Identity:{email}?secret={secret}&issuer=Alfred.Identity&digits=6&period=30
        var issuer = "Alfred.Identity";
        return $"otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}&digits={Digits}&period={Period}";
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code)) return false;

        // Try validation for current, previous, and next interval (drift)
        var currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / Period;
        
        for (long i = -1; i <= 1; i++)
        {
            if (CheckCode(secret, code, currentStep + i)) return true;
        }

        return false;
    }

    private bool CheckCode(string secret, string code, long step)
    {
        try
        {
            var secretBytes = Base32Decode(secret);
            var stepBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(step));

            using var hmac = new HMACSHA1(secretBytes);
            var hash = hmac.ComputeHash(stepBytes);

            var offset = hash[hash.Length - 1] & 0x0F;
            var binary =
                ((hash[offset] & 0x7f) << 24) |
                ((hash[offset + 1] & 0xff) << 16) |
                ((hash[offset + 2] & 0xff) << 8) |
                (hash[offset + 3] & 0xff);

            var otp = binary % (int)Math.Pow(10, Digits);
            var result = otp.ToString().PadLeft(Digits, '0');

            return result == code;
        }
        catch
        {
            return false;
        }
    }

    public string[] GenerateBackupCodes(int count = 10)
    {
        var codes = new string[count];
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];

        for (int i = 0; i < count; i++)
        {
            // Simple logic: 8 digit hex or similar complexity. User said "10 mã".
            // Let's use 8-char alphanumeric or hex. 
            // 8 hex chars = 4 bytes.
            rng.GetBytes(bytes);
            codes[i] = BitConverter.ToString(bytes).Replace("-", "").ToUpperInvariant();
        }
        return codes;
    }

    // Base32 Implementation (RFC 4648)

    private static readonly char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();
    private static readonly Dictionary<char, int> CharMap = Base32Chars
        .Select((c, i) => new { c, i })
        .ToDictionary(x => x.c, x => x.i);

    private static string Base32Encode(byte[] data)
    {
        var result = new StringBuilder();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (byte b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                result.Append(Base32Chars[(buffer >> (bitsLeft - 5)) & 0x1F]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            result.Append(Base32Chars[(buffer << (5 - bitsLeft)) & 0x1F]);
        }

        return result.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        input = input.Trim().ToUpperInvariant().Replace("=", "");
        var result = new List<byte>();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (char c in input)
        {
            if (!CharMap.TryGetValue(c, out int val)) continue;

            buffer = (buffer << 5) | val;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                result.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }
        
        return result.ToArray();
    }
}
