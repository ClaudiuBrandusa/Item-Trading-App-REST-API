﻿using System;

namespace Item_Trading_App_REST_API.Models.Trade
{
    public class ReceivedRespondedTradeOffer : ReceivedTradeOffer
    {
        public bool Response { get; set; }

        public DateTime ResponseDate { get; set; }
    }
}
