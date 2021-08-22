﻿using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Models
{
    public class AuthenticationResult
    {
        public string Token { get; set; }

        public bool Success { get; set; }

        public IEnumerable<string> Errors { get; set; }
    }
}
