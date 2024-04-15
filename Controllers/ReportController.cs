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
            return await _reportService.GetAllReport(options, Account);
        }
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ReportResponse>> GetAllReports([FromBody] ReportCreate report)
        {
            return await _reportService.AddReport(report, Account);
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
        public async Task<IActionResult> DeleteReport(int reportId)
        {
            await _reportService.DeleteReport(reportId, Account);
            return Ok("Report.Delete");
        }

    }
}
