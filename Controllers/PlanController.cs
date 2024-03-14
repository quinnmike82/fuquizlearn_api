using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Plan;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Principal;

namespace fuquizlearn_api.Controllers
{
    [ApiController]
    public class PlanController : BaseController
    {
        private readonly IPlanService _planService;
        public PlanController(IPlanService planService)
        {
            _planService = planService;
        }
        [HttpPost]
        public async Task<IActionResult> CreatePlan(PlanCreate planCreate)
        {
                var newPlan = await _planService.CreatePlan(planCreate, Account);
                return Ok(newPlan);
        }
        [HttpPost("RegistPlan/{id}")]
        public async Task<IActionResult> RegistPlan(int id)
        {
                var newPlan = await _planService.RegisterPlan(id, Account);
                return Ok(newPlan);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPlans()
        {
                var plans = await _planService.GetAllPlan();
                return Ok(plans);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemovePlan(int id)
        {
                await _planService.RemovePlan(id, Account);
                return NoContent();
        }

        [HttpPut("unrelease/{id}")]
        public async Task<IActionResult> UnReleasePlan(int id)
        {
                var unReleasedPlan = await _planService.UnReleasePlan(id, Account);
                return Ok(unReleasedPlan);
        }

        [HttpPut]
        public async Task<IActionResult> UpdatePlan(PlanUpdate planUpdate)
        {
                var updatedPlan = await _planService.UpdatePlan(planUpdate, Account);
                return Ok(updatedPlan);
        }
    }
}
