using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace MinimalApi.Services;

public interface IHasher
{
    string Hash(string value);
}

public class Hasher : IHasher
{
    private static Lazy<byte[]> _hashSaltLazy;
    private static byte[] _hashSalt => _hashSaltLazy.Value;

    private readonly CryptoConfig _config;

    public Hasher(IOptions<CryptoConfig> cryptoConfigOptions)
    {
        _config = cryptoConfigOptions.Value;
        _hashSaltLazy = new Lazy<byte[]>(() => Encoding.UTF8.GetBytes(_config.HashSalt));
    }

    public string Hash(string value)
    {
        using (var algorithm = new HMACSHA256(_hashSalt))
        {
            return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }
    }
}
