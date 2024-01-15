using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Deestone.Models
{
    public class EmployeeModel
    {
        public string CODEMPID { get; set; }
        public string EMPNAME { get; set; }
        public string EMPLASTNAME { get; set; }
        public string COMPANYNAME { get; set; }
        public int POSITIONCODE { get; set; }
        public int DIVISIONCODE { get; set; }
        public int DEPARTMENTCODE { get; set; }
        public string EMAIL { get; set; }
        public string DIVISIONNAME { get; set; }
        public string DEPARTMENTNAME { get; set; }
        public string POSITIONNAME { get; set; }
    }
}
