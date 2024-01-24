using System.Security.Cryptography;
using System.Text;

namespace fuquizlearn_api.Services;

public interface IHelperCryptoService
{
    string GeneratePIN(int length);
    byte[] GenerateSalt(int length);
    string HashSHA256(string data);
    bool CompareSHA256(string hash, string raw);
}

public class HelperCryptoService : IHelperCryptoService
{
    private static readonly RNGCryptoServiceProvider _cryptoServiceProvider = new();

    public string GeneratePIN(int length)
    {
        // Validate input
        if (length < 1) throw new ArgumentException("PIN length must be at least 1.");


        var buffer = new byte[length];
        _cryptoServiceProvider.GetBytes(buffer);

        var pin = new string(buffer.Select(b => (char)('0' + b % 10)).ToArray());
        return pin;
    }

    public byte[] GenerateSalt(int length)
    {
        if (length < 1) throw new ArgumentException("Salt length must be at lease 1");
        var salt = new byte[length];

        _cryptoServiceProvider.GetNonZeroBytes(salt);


        return salt;
    }

    public string HashSHA256(string data)
    {
        var crypt = new SHA256Managed();
        var hash = new StringBuilder();
        var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(data));
        foreach (var theByte in crypto) hash.Append(theByte.ToString("x2"));
        return hash.ToString();
    }

    public bool CompareSHA256(string hashed, string raw)
    {
        var rawHashed = HashSHA256(raw);
        return rawHashed == hashed;
    }
}