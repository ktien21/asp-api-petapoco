using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using WEB.SERVICE;
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

        [HttpPost("Insert")]
        public IActionResult Post()
        {
            try
            {
                List<Person> p = new List<Person>()
                {
                    new Person{FullName = "nguoi 1", Address = "Lai Xa", CreateDate = DateTime.Now}
                };
                BulkMySql.BulkInsert(new DBM(), p.ToDataTable(), "dossier_person");
                // BulkMySql.MySqlBulkCopy(new DBM(), p.ToDataTable(), "dossier_person");

                return Ok(new
                {
                    msg = "Ok"
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
