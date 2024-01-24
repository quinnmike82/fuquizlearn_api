namespace fuquizlearn_api.Services
{
    public interface IHelperDateService
    {
        long ConvertToUnixTimestamp(DateTime date);
    }
    public class HelperDateService : IHelperDateService
    {
        public long ConvertToUnixTimestamp(DateTime date)
        {
            var unixTimestamp = new DateTimeOffset(date).ToUnixTimeSeconds();

            return unixTimestamp * 1000;
        }
    }
}
