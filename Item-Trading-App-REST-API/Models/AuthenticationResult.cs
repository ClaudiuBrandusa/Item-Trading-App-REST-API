using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Models
{
    public class AuthenticationResult
    {
        public bool Success { get; set; }

        public IEnumerable<string> Errors { get; set; }
    }
}
