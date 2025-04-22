using System;

namespace FishCareSystem.API.AI
{
    public class AIPredictionResponse
    {
        public bool IsAbnormal { get; set; }
        public AIAction? Action { get; set; }
    }
}
