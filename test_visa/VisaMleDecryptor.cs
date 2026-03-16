using System.Security.Cryptography;
using System.Text;
using Jose;

public static class VisaMleDecryptor
{
    public static string DecryptEncData(string encData, string privateKeyPemPath)
    {
        if (string.IsNullOrWhiteSpace(encData))
            throw new ArgumentException("encData is empty.");

        if (string.IsNullOrWhiteSpace(privateKeyPemPath) || !File.Exists(privateKeyPemPath))
            throw new FileNotFoundException("Private key PEM file not found.", privateKeyPemPath);

        string pem = File.ReadAllText(privateKeyPemPath);

        using RSA rsa = LoadRsaPrivateKeyFromPem(pem);

        byte[] plaintextBytes = JWT.DecodeBytes(encData, rsa);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private static RSA LoadRsaPrivateKeyFromPem(string pem)
    {
        RSA rsa = RSA.Create();

        try
        {
            rsa.ImportFromPem(pem.ToCharArray());
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw new InvalidOperationException(
                "Could not import the PEM private key. Make sure this is the CLIENT MLE private key " +
                "for the same Key-ID, not the mTLS/TLS key.");
        }
    }
}