using Microsoft.AspNetCore.Mvc;
using test_visa.Services;
using static test_visa.Models;

namespace test_visa.Controllers;

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
    public IActionResult Post([FromBody] DecryptRequest request)
    {
        try
        {
            DecryptResponse decryptedJson = _service.Decrypt(request.ResponseJson, request.EncData);

            return Ok(decryptedJson);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = ex.Message
            });
        }
    }
}
