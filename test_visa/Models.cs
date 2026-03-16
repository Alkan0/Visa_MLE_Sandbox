using System.Text.Json;
using System.Text.Json.Nodes;

namespace test_visa;

public class Models
{
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
}
