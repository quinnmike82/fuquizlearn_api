namespace fuquizlearn_api.Models.Plan
{
    public class PlanResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string[] Features { get; set; } = new string[] { };
        public int Duration { get; set; }
        public int Amount { get; set; }
        public int MaxStudent { get; set; }
        public int MaxClassroom { get; set; }
        public int useAICount { get; set; }
        public bool IsRelease { get; set; }
        public bool IsCurrent { get; set; }
    }
}
