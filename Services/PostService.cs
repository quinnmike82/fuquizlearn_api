using AutoMapper;
using fuquizlearn_api.Entities;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Extensions;
using fuquizlearn_api.Helpers;
using fuquizlearn_api.Models.Accounts;
using fuquizlearn_api.Models.Posts;
using fuquizlearn_api.Models.Quiz;
using fuquizlearn_api.Models.QuizBank;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Web;

namespace fuquizlearn_api.Services
{
    public interface IPostService
    {
        Task<PostResponse> CreatePost(PostCreate post, Account account);
        Task<PostResponse> GetPostById(int id);
        Task<PagedResponse<PostResponse>> GetAllPosts(int classroomId, PagedRequest options);
        Task<PostResponse> UpdatePost(int PostId,PostUpdate post, Account currentUser);
        Task DeletePost(int id);
        Task<CommentResponse> CreateComment(int postId, CommentCreate comment, Account account);
        Task<CommentResponse> GetCommentById(int id);
        Task<PagedResponse<CommentResponse>> GetAllComments(int postId, PagedRequest option);
        Task DeleteComment(int id);
        Task<bool> AddView(int postId, Account account);
        Task<PagedResponse<AccountResponse>> GetAccountView(int postId, PagedRequest options);
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

        public async Task<PagedResponse<PostResponse>> GetAllPosts(int classroomId, PagedRequest options)
        {
            var posts = await _context.Posts.Include(c => c.Classroom).ThenInclude(c => c.Account).Include(i => i.Comments).Where(p => p.Classroom.Id == classroomId).Include(i => i.Comments).ToPagedAsync(options,
            x => x.Title.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));

            var pages = new PagedResponse<PostResponse>
            {
                Data = _mapper.Map<IEnumerable<PostResponse>>(posts.Data),
                Metadata = posts.Metadata
            };
            var bank = new QuizBank();
            int id;
            foreach (var page in pages.Data)
            {
                if (int.TryParse(page.BankLink, out id))
                { bank = await _context.QuizBanks.FindAsync(id);
                if (bank != null)
                    page.QuizBank = _mapper.Map<QuizBankResponse>(bank); }
            }
            return pages;
        }

        public async Task<PostResponse> UpdatePost(int postId,PostUpdate post, Account currentUser)
        {
            var p = await CheckPost(postId, currentUser);
            _mapper.Map(post, p);

            _context.Posts.Update(p);
            _context.SaveChanges();

            return _mapper.Map<PostResponse>(p);
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
            var post = await _context.Posts.Include(c => c.Classroom).ThenInclude(c => c.Account).Include(i => i.Comments).FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null)
            {
                throw new KeyNotFoundException("Could not find the Post");
            }
            int id;
            if(int.TryParse(post.BankLink, out id))
                post.QuizBank = await _context.QuizBanks.FindAsync(id);
            return post;
        }

        private async Task<Post> CheckPost(int postId, Account currentUser)
        {
            var post = await GetPost(postId);
            if (post == null) throw new KeyNotFoundException("Could not find Post");
            if (post.Author.Id == currentUser.Id || currentUser.Role == Role.Admin) return _mapper.Map<Post>(post);
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

        public async Task<PagedResponse<CommentResponse>> GetAllComments(int postId, PagedRequest options)
        {
            var comments = await _context.Comments.Include(c => c.Author).Where(c => c.Post.Id == postId).ToPagedAsync(options,
            x => x.Content.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
            return new PagedResponse<CommentResponse>
            {
                Data = _mapper.Map<IEnumerable<CommentResponse>>(comments.Data),
                Metadata = comments.Metadata
            };

        }

        public async Task DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                throw new KeyNotFoundException("Comment not found");

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AddView(int postId, Account account)
        {
            var post = await GetPost(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");
            if(post.ViewIds == null)
            {
                post.ViewIds = new int[] {account.Id};
                _context.Posts.Update(post);
                await _context.SaveChangesAsync();
                return true;
            }
            if(Array.IndexOf(post.ViewIds, account.Id) == -1)
            {
                post.ViewIds = post.ViewIds.Append(account.Id).ToArray();
                _context.Posts.Update(post);
                await _context.SaveChangesAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<PagedResponse<AccountResponse>> GetAccountView(int postId, PagedRequest options)
        {
            var post = await GetPost(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");
            if (post.ViewIds == null)
            {
                post.ViewIds = new int[] { };
            }
            int[] ids = post.ViewIds;
            var accounts = await _context.Accounts.Where(c => ids.Contains(c.Id)).ToPagedAsync(options,
            x => x.FullName.ToLower().Contains(HttpUtility.UrlDecode(options.Search, Encoding.ASCII).ToLower()));
            return new PagedResponse<AccountResponse>
            {
                Data = _mapper.Map<IEnumerable<AccountResponse>>(accounts.Data),
                Metadata = accounts.Metadata
            };
        }
    }
}
