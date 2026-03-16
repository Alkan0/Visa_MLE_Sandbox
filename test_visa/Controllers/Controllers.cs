using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using test_visa.Services;
using static test_visa.Models;

namespace test_visa.Controllers
{
    public class Controllers
    {
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
    }
}
