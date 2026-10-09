using System.Security.Cryptography;

namespace Server.Tests;

/// <summary>
/// A real P-256 VAPID pair generated once per test run: the private key
/// never leaves the test host (in-memory config only).
/// </summary>
internal static class TestVapidKeys
{
    private static readonly Lazy<(string PublicKey, string PrivateKey)> Pair = new(Build);

    public static string PublicKey => Pair.Value.PublicKey;

    public static string PrivateKey => Pair.Value.PrivateKey;

    private static (string PublicKey, string PrivateKey) Build()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(true);

        var uncompressed = new byte[65];
        uncompressed[0] = 0x04;
        Buffer.BlockCopy(parameters.Q.X!, 0, uncompressed, 1, 32);
        Buffer.BlockCopy(parameters.Q.Y!, 0, uncompressed, 33, 32);

        return (Base64Url(uncompressed), Base64Url(parameters.D!));
    }

    private static string Base64Url(byte[] bytes)
        => Convert
            .ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
