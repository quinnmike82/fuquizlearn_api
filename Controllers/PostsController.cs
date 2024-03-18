using fuquizlearn_api.Authorization;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Models.Posts;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace fuquizlearn_api.Controllers
{
    [ApiController]
    [Authorize]
    public class PostController : BaseController
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] PostCreate postCreate)
        {
            var createdPost = await _postService.CreatePost(postCreate, Account);
            return CreatedAtAction(nameof(GetPostById), new { id = createdPost.Id }, createdPost);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostById(int id)
        {
                var post = await _postService.GetPostById(id);
                return Ok(post);
        }

        [HttpGet("classroom/{classroomId}")]
        public async Task<IActionResult> GetAllPosts(int classroomId, [FromQuery] PagedRequest options)
        {
            var posts = await _postService.GetAllPosts(classroomId, options);
            return Ok(posts);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] PostUpdate postUpdate)
        {
                var updatedPost = await _postService.UpdatePost(id, postUpdate, Account);
                return Ok(updatedPost);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
                await _postService.DeletePost(id);
                return NoContent();
        }

        [HttpPost("{postId}/comments")]
        public async Task<IActionResult> CreateComment(int postId, [FromBody] CommentCreate comment)
        {
            var createdComment = await _postService.CreateComment(postId, comment, Account);
            return CreatedAtAction(nameof(GetCommentById), new { id = createdComment.Id }, createdComment);
        }

        [HttpGet("comments/{id}")]
        public async Task<IActionResult> GetCommentById(int id)
        {
                var comment = await _postService.GetCommentById(id);
                return Ok(comment);
        }

        [HttpGet("{postId}/comments")]
        public async Task<IActionResult> GetAllComments(int postId, [FromQuery] PagedRequest options)
        {
            var comments = await _postService.GetAllComments(postId, options);
            return Ok(comments);
        }

        [HttpDelete("comments/{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
                await _postService.DeleteComment(id);
                return NoContent();
        }
        [HttpPost("AddView/{id}")]
        public async Task<IActionResult> AddView(int id)
        {
                var comment = await _postService.AddView(id, Account);
                return Ok(comment);
        }
        [HttpGet("{id}/views")]
        public async Task<IActionResult> GetAccountView(int id, [FromQuery] PagedRequest options)
        {
                var comment = await _postService.GetAccountView(id, options);
                return Ok(comment);
        }

    }
}
