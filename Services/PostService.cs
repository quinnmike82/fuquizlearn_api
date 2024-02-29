using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Posts;
using fuquizlearn_api.Models.Quiz;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace fuquizlearn_api.Services
{
    public interface IPostService
    {
        Task<PostResponse> CreatePost(PostCreate post, Account account);
        Task<PostResponse> GetPostById(int id);
        Task<List<PostResponse>> GetAllPosts(int classroomId);
        Task<PostResponse> UpdatePost(PostUpdate post, Account currentUser);
        Task DeletePost(int id);
        Task<CommentResponse> CreateComment(int postId, CommentCreate comment, Account account);
        Task<CommentResponse> GetCommentById(int id);
        Task<List<CommentResponse>> GetAllComments(int postId);
        Task DeleteComment(int id);
    }
    public class PostService : IPostService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public PostService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PostResponse> CreatePost(PostCreate post, Account account)
        {
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(i => i.Id == post.ClassroomId);
            if (classroom == null)
                throw new KeyNotFoundException("Cound not find Classroom");
            var newPost = _mapper.Map<Post>(post);
            newPost.Author = account;
            newPost.Classroom  = classroom; 
            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();
            return _mapper.Map<PostResponse>(newPost);
        }

        public async Task<PostResponse> GetPostById(int id)
        {
            var post = await GetPost(id);
             if (post == null)
                throw new KeyNotFoundException("Could not find Post");
            return _mapper.Map<PostResponse>(post);
        }

        public async Task<List<PostResponse>> GetAllPosts(int classroomId)
        {
            var posts = await _context.Posts.Where(p => p.Classroom.Id == classroomId).Include(i => i.Comments).ToListAsync();
            var postResponse = _mapper.Map<List<PostResponse>>(posts);
            for (int i = 0; i < postResponse.Count(); i++)
            {
                postResponse[i].Comments = _mapper.Map<List<CommentResponse>>(posts[i].Comments);
            }
            return postResponse;
        }

        public async Task<PostResponse> UpdatePost(PostUpdate post, Account currentUser)
        {
            var p = CheckPost(post.Id, currentUser);
            _context.Entry(post).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return _mapper.Map<PostResponse>(post);
        }

        public async Task DeletePost(int id)
        {
            var post = await GetPost(id);
            if (post == null)
            {
                throw new ArgumentException("Post not found");
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
        }

        private async Task<Post> GetPost(int postId)
        {
            var post = await _context.Posts.Include(i => i.Comments).FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
            {
                throw new KeyNotFoundException("Could not find the Post");
            }
            return post;
        }

        private async Task<PostResponse> CheckPost(int postId, Account currentUser)
        {
            var post = await GetPost(postId);
            if (post == null) throw new KeyNotFoundException("Could not find Post");
            if (post.Author.Id == currentUser.Id || currentUser.Role == Role.Admin) return _mapper.Map<PostResponse>(post);
            throw new UnauthorizedAccessException();
        }

        public async Task<CommentResponse> CreateComment(int postId, CommentCreate comment, Account account)
        {
            var newComment = _mapper.Map<Comment>(comment);
            var post = await GetPost(postId);
            if (post == null)
                throw new KeyNotFoundException("Could not find Post");
            newComment.Author = account;
            if(post.Comments == null)
                post.Comments = new List<Comment> { newComment };
            else post.Comments.Add(newComment);
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
            return _mapper.Map<CommentResponse>(newComment);
        }

        public async Task<CommentResponse> GetCommentById(int id)
        {
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null)
                throw new KeyNotFoundException("Comment not found");
            return _mapper.Map<CommentResponse>(comment);
        }

        public async Task<List<CommentResponse>> GetAllComments(int postId)
        {
            var comments = await _context.Comments.Where(c => c.Post.Id == postId).ToListAsync();
            return _mapper.Map<List<CommentResponse>>(comments);
        }

        public async Task DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                throw new KeyNotFoundException("Comment not found");

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }

    }
}
