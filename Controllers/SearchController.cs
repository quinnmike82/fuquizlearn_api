using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers
{
    [Route("api/SearchGlobal")]
    [ApiController]
    public class SearchController : BaseController
    {
        private ISearchTextService _searchTextService;
        public SearchController(ISearchTextService searchService)
        {
            _searchTextService = searchService;
        }
        [HttpGet("SearchGlobal")]
        public async Task<ActionResult> GetAllPosts([FromQuery] PagedRequest options)
        {
            var result = await _searchTextService.Search(options);
            return Ok(result);
        }
    }
}
