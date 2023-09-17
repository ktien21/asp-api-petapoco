using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WEB.SERVICE.PersonService
{
    public class Person
    {
        public int ID { get; set; }
        public Guid DossierID { get; set; }
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int Type { get; set; }
        public bool IsMasterProfile { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
