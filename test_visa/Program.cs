using Jose;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
object value = builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Visa MLE Tool", Version = "v1" });
});

builder.Services.Configure<MleOptions>(builder.Configuration.GetSection("Mle"));
builder.Services.AddSingleton<VisaMleService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

public class MleOptions
{
    public string VisaServerPublicCertPath { get; set; } = "";
    public string ClientMlePrivateKeyPemPath { get; set; } = "";
    public string KeyId { get; set; } = "";
}

public class EncryptRequest
{
    public JsonNode PlainJson { get; set; } = default!;
}

public class EncryptResponse
{
    public string EncData { get; set; } = "";
}

public class DecryptRequest
{
    public string? ResponseJson { get; set; }
    public string? EncData { get; set; }
}

public class DecryptResponse
{
    public JsonElement PlainJson { get; set; }
}

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

[ApiController]
[Route("api/mle/encrypt")]
public class EncryptController : ControllerBase
{
    private readonly VisaMleService _service;

    public EncryptController(VisaMleService service)
    {
        _service = service;
    }

    [HttpPost]
    public IActionResult Post([FromBody] JsonElement request)
    {
        try
        {
            string plainJson = request.GetRawText();

            var result = _service.Encrypt(plainJson);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

[ApiController]
[Route("api/mle/decrypt")]
public class DecryptController : ControllerBase
{
    private readonly VisaMleService _service;

    public DecryptController(VisaMleService service)
    {
        _service = service;
    }

    [HttpPost]
    public ActionResult<DecryptResponse> Post([FromBody] DecryptRequest request)
    {
        try
        {
            return Ok(_service.Decrypt(request.ResponseJson, request.EncData));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}