using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Plan;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Principal;

namespace fuquizlearn_api.Controllers
{
    [Authorize]
    [ApiController]
    public class PlanController : BaseController
    {
        private readonly IPlanService _planService;
        public PlanController(IPlanService planService)
        {
            _planService = planService;
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePlan([FromBody] PlanCreate planCreate)
        {
                var newPlan = await _planService.CreatePlan(planCreate, Account);
                return Ok(newPlan);
        }
        [Authorize]
        [HttpPost("RegistPlan/{id}/{transactionId}")]
        public async Task<IActionResult> RegistPlan(int id, string transactionId)
        {
                var newPlan = await _planService.RegisterPlan(id, transactionId, Account);
                return Ok(newPlan);
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllPlans([FromQuery] PagedRequest options)
        {
                var plans = await _planService.GetAllPlan(options);
                return Ok(plans);
        }
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
                var plans = await _planService.CheckCurrent(Account);
                return Ok(plans.Plan);
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemovePlan(int id)
        {
                await _planService.RemovePlan(id, Account);
                return NoContent();
        }
        [Authorize]
        [HttpPut("unrelease/{id}")]
        public async Task<IActionResult> UnReleasePlan(int id)
        {
                var unReleasedPlan = await _planService.UnReleasePlan(id, Account);
                return Ok(unReleasedPlan);
        }
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdatePlan(PlanUpdate planUpdate)
        {
                var updatedPlan = await _planService.UpdatePlan(planUpdate, Account);
                return Ok(updatedPlan);
        }
    }
}
