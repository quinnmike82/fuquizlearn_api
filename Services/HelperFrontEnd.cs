using fuquizlearn_api.Helpers;
using Microsoft.Extensions.Options;

namespace fuquizlearn_api.Services;

public interface IHelperFrontEnd
{
    public string getBaseUrl();
    public string getUrl(string uri);
}

public class HelperFrontEnd : IHelperFrontEnd
{
    private readonly string _baseUrl;
    private readonly AppSettings _settings;

    public HelperFrontEnd(IOptions<AppSettings> settings)
    {
        _settings = settings.Value;
        _baseUrl = _settings.FrontEndUrl;
    }

    public string getBaseUrl()
    {
        return _baseUrl;
    }

    public string getUrl(string uri)
    {
        if (uri.StartsWith('/') && _baseUrl.EndsWith('/')) return _baseUrl + uri.PadLeft(1);

        return _baseUrl + uri;
    }
}