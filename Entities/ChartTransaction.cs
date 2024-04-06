namespace fuquizlearn_api.Entities
{
    public class ChartTransaction
    {
        public int Id { get; set; }
        public int Amount { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}
