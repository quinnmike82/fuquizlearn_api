using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Posts;
using fuquizlearn_api.Models.Quiz;
using Microsoft.EntityFrameworkCore;

namespace fuquizlearn_api.Services
{
    public interface IPostService
    {
        Task<PostResponse> CreatePost(PostCreate post);
        Task<PostResponse> GetPostById(int id);
        Task<List<PostResponse>> GetAllPosts(int classroomId);
        Task<PostResponse> UpdatePost(PostUpdate post, Account currentUser);
        Task DeletePost(int id);
    }

    public interface ICommentService
    {
        Task<Comment> CreateComment(Comment comment);
        Task<Comment> GetCommentById(int id);
        Task<List<Comment>> GetAllComments(int postId);
        Task DeleteComment(int id);
    }
    public class PostService : IPostService, ICommentService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public PostService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PostResponse> CreatePost(PostCreate post)
        {
            var newPost = _mapper.Map<Post>(post);
            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();
            return _mapper.Map<PostResponse>(newPost);
        }

        public async Task<PostResponse> GetPostById(int id)
        {
            var post = GetPost(id);
             if (post == null)
                throw new KeyNotFoundException("Could not find Post");
            return _mapper.Map<PostResponse>(post);
        }

        public async Task<List<PostResponse>> GetAllPosts(int classroomId)
        {
            var posts = await _context.Posts.Where(p => p.Classroom.Id == classroomId).ToListAsync();
            return _mapper.Map<List<PostResponse>>(posts);
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
            var post = await _context.Posts.FirstOrDefaultAsync(x => x.Id == postId);
            if (post == null)
            {
                throw new KeyNotFoundException("Could not find the Post");
            }
            return post;
        }

        private Post CheckPost(int postId, Account currentUser)
        {
            var post = _context.Posts.Find(postId);
            if (post == null) throw new KeyNotFoundException("Could not find Post");
            if (post.Author.Id == currentUser.Id || currentUser.Role == Role.Admin) return post;
            throw new UnauthorizedAccessException();
        }

        public async Task<Comment> CreateComment(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<Comment> GetCommentById(int id)
        {
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null)
                throw new KeyNotFoundException("Comment not found");
            return comment;
        }

        public async Task<List<Comment>> GetAllComments(int postId)
        {
            var comments = await _context.Comments.Where(c => c.Post.Id == postId).ToListAsync();
            return comments;
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
