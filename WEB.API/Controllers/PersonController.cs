using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WEB.SERVICE.PersonService;

namespace WEB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        private readonly PersonSV _personSV;

        public PersonController(PersonSV personSV)
        {
            _personSV = personSV;
        }
        [HttpGet("getbyID")]
        public IActionResult actionResult(int id)
        {
            try
            {
                return Ok(new
                {
                    data = _personSV.GetByID(id)
                });
            }
            catch (Exception e)
            {

                return Ok(new
                {
                    msg = e.Message
                });
            }
        }
    }
}
