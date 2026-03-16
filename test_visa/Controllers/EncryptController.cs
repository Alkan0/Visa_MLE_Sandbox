using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using test_visa.Services;

namespace test_visa.Controllers;

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
            if (request.ValueKind == JsonValueKind.Undefined ||
                request.ValueKind == JsonValueKind.Null)
            {
                return BadRequest(new
                {
                    error = "Request body is empty."
                });
            }

            string plainJson = request.GetRawText();

            var encrypted = _service.Encrypt(plainJson);

            return Ok(encrypted);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = ex.Message
            });
        }
    }
}