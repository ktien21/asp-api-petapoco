using PetaPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace WEB.SERVICE.PersonService
{
    public class PersonSV
    {
        public Person GetByID(int id)
        {
            Console.WriteLine(ConfigurationManager.AppSettings["hello"]);
            Console.WriteLine("connection string: "+ ConfigurationManager.AppSettings["MYSQL"]);
            var sql = Sql.Builder.Select("*")
            .From("`dossier_person`").Where("ID = @0", id);
            using DBM dbm = new DBM();
            dbm.OpenConnection();
            using var db = new Database(dbm.conn);

            var person = db.FirstOrDefault<Person>(sql);

            return person;
        }
    }
}
