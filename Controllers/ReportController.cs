using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Report;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers
{
    [Authorize]
    [ApiController]
    public class ReportController : BaseController
    {
        private readonly IReportService _reportService;
        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<PagedResponse<ReportResponse>>> GetAllReports([FromQuery] PagedRequest options)
        {
            var result = await _reportService.GetAllReport(options, Account);
            return Ok(result);
        }
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ReportResponse>> GetAllReports([FromBody] ReportCreate report)
        {
            var result = await _reportService.AddReport(report, Account);
            return Ok(result);
        }
        [Authorize]
        [HttpPost("verify/{reportId}")]
        public async Task<IActionResult> VerifyReport(int reportId)
        {
            await _reportService.VerifyReport(reportId, Account);
            return Ok("Report.Verify");
        }
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteReport([FromBody] ReportDelete reportDelete)
        {
            await _reportService.DeleteReport(reportDelete.ReportIds, Account);
            return Ok();
        }

    }
}
