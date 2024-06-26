﻿namespace fuquizlearn_api.Models.Plan
{
    public class PlanUpdate
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public string[] Features { get; set; } = new string[] { };
        public int Amount { get; set; }
        public int MaxStudent { get; set; }
        public int useAICount { get; set; }
        public bool IsRelease { get; set; }
    }
}
