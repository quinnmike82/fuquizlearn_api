using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;

namespace fuquizlearn_api.Entities
{
    public class Plan
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public int Amount { get; set; }
        public int MaxStudent { get; set; }
        public int useAICount { get; set; }
        public bool IsRelease { get; set; }
        public DateTime? Deleted { get; set; }

    }
}
