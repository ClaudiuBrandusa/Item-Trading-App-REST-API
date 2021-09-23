using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Models.Base
{
    public class BaseResult
    {
        public bool Success { get; set; }

        public IEnumerable<string> Errors { get; set; }
    }
}
