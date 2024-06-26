﻿using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Models.Classroom;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers
{
    [Authorize]
    [ApiController]
    public class ClassroomsController : BaseController
    {
        private readonly IClassroomService _classroomService;

        public ClassroomsController(IClassroomService classroomService)
        {
            _classroomService = classroomService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ClassroomResponse>> CreateClassroom([FromBody] ClassroomCreate classroomCreate)
        {
            var result = await _classroomService.CreateClassroom(classroomCreate, Account);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ClassroomResponse>> GetClassroomById(int id)
        {
            var result = await _classroomService.GetClassroomById(id, Account);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet("get-all-member/{id}")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<AccountResponse>>> GetAllMember(int id,
            [FromQuery] PagedRequest options)
        {
            var result = await _classroomService.GetAllMember(id, Account, options);
            return Ok(result);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<ClassroomResponse>>> GetAllClassrooms([FromQuery] PagedRequest options)
        {
            var result = await _classroomService.GetAllClassrooms(options);
            return Ok(result);
        }

        [HttpPost("addmember")]
        [Authorize]
        public async Task<IActionResult> AddMember([FromBody] AddMember addMember)
        {
            await _classroomService.AddMember(addMember, Account);
            return Ok("Add member successfully");
        }

        [HttpPost("batchaddmember/{classroomId}")]
        [Authorize]
        public async Task<IActionResult> BatchAddMember(int classroomId,
            [FromBody] BatchMemberRequest batchMemberRequest)
        {
            await _classroomService.BatchAddMember(classroomId, Account, batchMemberRequest.MemberIds);
            return Ok("Add batch members successfully");
        }

        [HttpDelete("removemember/{memberId}/{classroomId}")]
        [Authorize]
        public async Task<IActionResult> RemoveMember(int memberId, int classroomId)
        {
            await _classroomService.RemoveMember(memberId, classroomId, Account);
            return Ok("Remove member successfully");
        }

        [HttpDelete("batchremovemember/{classroomId}")]
        [Authorize]
        public async Task<IActionResult> BatchRemoveMember(int classroomId,
            [FromBody] BatchMemberRequest batchMemberRequest)
        {
            await _classroomService.BatchRemoveMember(classroomId, Account, batchMemberRequest.MemberIds);
            return Ok("Remove batch members successfully");
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteClassroom(int id)
        {
            await _classroomService.DeleteClassroom(id, Account);
            return Ok( new { message= "Delete classroom successfully" });
        }

        [HttpPost("generatecode/{classroomId}")]
        [Authorize]
        public async Task<ActionResult<ClassroomCode>> GenerateClassroomCode(int classroomId)
        {
            var result = await _classroomService.GenerateClassroomCode(classroomId, Account);
            return Ok(result);
        }

        [HttpGet("codes/{classroomId}")]
        [Authorize]
        public async Task<ActionResult<List<ClassroomCode>>> GetAllClassroomCodes(int classroomId)
        {
            var result = await _classroomService.GetAllClassroomCodes(classroomId);
            return Ok(result);
        }

        [HttpPost("join/{classroomCode}")]
        [Authorize]
        public async Task<IActionResult> JoinClassroomWithCode(string classroomCode)
        {
            await _classroomService.JoinClassroomWithCode(classroomCode, Account);
            return Ok("Joined classroom successfully");
        }

        [HttpPost("addquizbank/{classroomId}")]
        [Authorize]
        public async Task<ActionResult<QuizBankResponse>> AddQuizBank([FromBody] QuizBankCreate model, int classroomId)
        {
            var result = await _classroomService.AddQuizBank(classroomId, model, Account);
            return Ok(result);
        }

        [HttpPost("copyquizbank/{quizbankId}/{classroomId}")]
        [Authorize]
        public async Task<IActionResult> CopyQuizBank(int quizbankId, int classroomId,
            [FromBody] QuizBankUpdate quizBankUpdate)
        {
            var newName = quizBankUpdate.BankName ?? "";
            await _classroomService.CopyQuizBank(quizbankId, classroomId, newName, Account);
            return Ok("Copy Quizbank successfully");
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<ActionResult<ClassroomResponse>> UpdateClassroom([FromBody] ClassroomUpdate classroomUpdate)
        {
            var result = await _classroomService.UpdateClassroom(classroomUpdate, Account);
            return Ok(result);
        }

        [HttpGet("getByAccountId/{id}")]
        [Authorize]
        public async Task<ActionResult<List<ClassroomResponse>>> GetCurrentAccountClassroom(int id)
        {
            var result = await _classroomService.GetAllClassroomsByUserId(id);
            return Ok(result);
        }

        [HttpGet("getCurrent")]
        [Authorize]
        public async Task<ActionResult<List<ClassroomResponse>>> GetCurrentClassroom([FromQuery] PagedRequest options)
        {
            var result = await _classroomService.GetAllClassroomsByAccountId(options, Account);
            return Ok(result);
        }

        [HttpGet("getCurrentJoined")]
        [Authorize]
        public async Task<ActionResult<List<ClassroomResponse>>> GetCurrentJoinedClassroom(
            [FromQuery] PagedRequest options)
        {
            var result = await _classroomService.GetCurrentJoinedClassroom(options, Account);
            return Ok(result);
        }

        [HttpPost("sent-invitation-email/{classroomId}")]
        [Authorize]
        public async Task<IActionResult> SentInvitationEmail(int classroomId,
            [FromBody] BatchMemberRequest batchMemberRequest)
        {
            await _classroomService.SentInvitationEmail(classroomId, batchMemberRequest, Account);
            return Ok("Send invitation email successfully");
        }

        [HttpPost("{classroomId}/users")]
        [Authorize]
        public async Task<IActionResult> BanMember(int classroomId, [FromBody] BatchMemberRequest members)
        {
            await _classroomService.BanMember(classroomId, members, Account);
            return Ok("Ban user successfully");
        }

        [HttpPut("{classroomId}/users")]
        [Authorize]
        public async Task<IActionResult> UnbanMember(int classroomId, [FromBody] BatchMemberRequest members)
        {
            await _classroomService.UnbanMember(classroomId, members, Account);
            return Ok("Unban user successfully");
        }

        [HttpGet("{classroomId}/users")]
        [Authorize]
        public async Task<ActionResult<List<AccountResponse>>> UnbanMember(int classroomId,
            [FromQuery] PagedRequest options)
        {
            var users = await _classroomService.GetBanAccounts(classroomId, options);
            return Ok(users);
        }

        [HttpPut("{classroomId}/leave")]
        [Authorize]
        public async Task<IActionResult> LeaveClassroom(int classroomId)
        {
            await _classroomService.LeaveClassroom(classroomId, Account);
            return Ok("Leave classroom successfully");
        }

        [HttpGet("classroom/{classroomId}/quizbank/")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<ClassroomResponse>>> GetAllBankFromClass(int classroomId,
            [FromQuery] PagedRequest options)
        {
            var result = await _classroomService.GetAllBankFromClass(classroomId, options, Account);
            return Ok(result);
        }
    }
}