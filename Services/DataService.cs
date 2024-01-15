using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Deestone.Services
{
    public class DataService
    {
        public object filterDatatables(HttpRequest req)
        {
            var draw = req.Form["draw"].FirstOrDefault();
            var start = req.Form["start"].FirstOrDefault();
            var length = req.Form["length"].FirstOrDefault();

            return new { };
        }

        public object formatDatatables(List<object> _data)
        {
            return new {
                draw = 1,
                recordsTotal = _data.Count,
                recordsFiltered = _data.Count,
                data = _data
            };
        }
    }
}
