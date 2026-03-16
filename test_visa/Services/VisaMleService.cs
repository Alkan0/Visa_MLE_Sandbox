using Jose;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using test_visa.Configurations;
using static test_visa.Models;

namespace test_visa.Services
{
    public class VisaMleService
    {
        private readonly MleOptions _options;

        public VisaMleService(Microsoft.Extensions.Options.IOptions<MleOptions> options)
        {
            _options = options.Value;
        }

        public EncryptResponse Encrypt(string plainJson)
        {
            var cert = LoadCertificate(_options.VisaServerPublicCertPath);
            using var rsa = cert.GetRSAPublicKey();

            long iat = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var extraHeaders = new Dictionary<string, object>
            {
                ["kid"] = _options.KeyId,
                ["iat"] = iat,
                ["cty"] = "application/json"
            };

            string jwe = JWT.EncodeBytes(
                Encoding.UTF8.GetBytes(plainJson),
                rsa,
                JweAlgorithm.RSA_OAEP_256,
                JweEncryption.A128GCM,
                extraHeaders: extraHeaders
            );

            return new EncryptResponse
            {
                EncData = jwe
            };
        }

        public DecryptResponse Decrypt(string? responseJson, string? encData)
        {
            string token = !string.IsNullOrWhiteSpace(encData)
                ? encData
                : ExtractEncData(responseJson ?? throw new ArgumentException("Either responseJson or encData is required."));

            if (!File.Exists(_options.ClientMlePrivateKeyPemPath))
                throw new FileNotFoundException("Client MLE private key PEM not found.", _options.ClientMlePrivateKeyPemPath);

            string pem = File.ReadAllText(_options.ClientMlePrivateKeyPemPath);
            using RSA rsa = LoadPrivateKey(pem);

            byte[] plaintextBytes = JWT.DecodeBytes(token, rsa);
            string plaintext = Encoding.UTF8.GetString(plaintextBytes);

            var json = JsonDocument.Parse(plaintext).RootElement;

            return new DecryptResponse
            {
                PlainJson = json
            };
        }

        private static string ExtractEncData(string responseJson)
        {
            using JsonDocument doc = JsonDocument.Parse(responseJson);

            if (!doc.RootElement.TryGetProperty("encData", out var encDataElement))
                throw new InvalidOperationException("Response JSON does not contain encData.");

            string? encData = encDataElement.GetString();

            if (string.IsNullOrWhiteSpace(encData))
                throw new InvalidOperationException("encData is empty.");

            return encData;
        }

        private static X509Certificate2 LoadCertificate(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();

            if (ext is ".cer" or ".crt" or ".der")
                return new X509Certificate2(path);

            string pem = File.ReadAllText(path);
            return X509Certificate2.CreateFromPem(pem);
        }

        private static RSA LoadPrivateKey(string pem)
        {
            RSA rsa = RSA.Create();
            rsa.ImportFromPem(pem.ToCharArray());
            return rsa;
        }
    }
}
