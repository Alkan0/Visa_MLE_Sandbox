using Jose;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace test_visa;

public static class VisaMleEncryptor
{
    /// <summary>
    /// Encrypts plaintext JSON into Visa MLE encData wrapper.
    /// </summary>
    /// <param name="plainJson">The original business JSON you want to send.</param>
    /// <param name="visaServerPublicCertPath">
    /// Path to Visa server encryption certificate (.cer/.pem/.crt).
    /// This is the public cert used to encrypt request payloads.
    /// </param>
    /// <param name="keyId">Your Visa MLE Key-ID. Must match the HTTP keyId header you send.</param>
    /// <returns>JSON string like { "encData": "..." }</returns>
    public static string BuildEncDataBody(string plainJson, string visaServerPublicCertPath, string keyId)
    {
        if (string.IsNullOrWhiteSpace(plainJson))
            throw new ArgumentException("plainJson is empty.");

        if (string.IsNullOrWhiteSpace(visaServerPublicCertPath) || !File.Exists(visaServerPublicCertPath))
            throw new FileNotFoundException("Visa server public certificate file not found.", visaServerPublicCertPath);

        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("keyId is empty.");

        var cert = LoadCertificate(visaServerPublicCertPath);
        using var rsa = cert.GetRSAPublicKey();

        if (rsa == null)
            throw new InvalidOperationException("Could not extract RSA public key from Visa server certificate.");

        var payloadBytes = Encoding.UTF8.GetBytes(plainJson);

        var extraHeaders = new Dictionary<string, object>
        {
            ["kid"] = keyId,
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["cty"] = "application/json"
        };

        string jwe = JWT.EncodeBytes(
            payloadBytes,
            rsa,
            JweAlgorithm.RSA_OAEP_256,
            JweEncryption.A128GCM,
            extraHeaders: extraHeaders
        );

        var wrapper = new
        {
            encData = jwe
        };

        return JsonSerializer.Serialize(wrapper, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static X509Certificate2 LoadCertificate(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();

        if (ext is ".cer" or ".crt" or ".der")
        {
            return new X509Certificate2(path);
        }

        // PEM certificate support
        string pem = File.ReadAllText(path);
        return X509Certificate2.CreateFromPem(pem);
    }
}