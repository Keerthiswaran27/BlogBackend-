using BlogApp1.Shared;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using static Supabase.Postgrest.Constants;

namespace BlogApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogsController : ControllerBase
    {
        private readonly Client _supabase;

        public BlogsController(IConfiguration config)
        {
            var supabaseUrl = config["Supabase:Url"];
            var supabaseKey = config["Supabase:Key"];

            _supabase = new Client(supabaseUrl, supabaseKey);
        }

        // GET api/blogs
        [HttpGet]
        public async Task<IActionResult> GetBlogs()
        {
            var result = await _supabase.From<BlogData>().Get();

            var dtoList = result.Models.Select(b => new BlogDto
            {
                Id = b.Id,
                Title = b.Title,
                Slug = b.Slug,
                Content = b.Content,
                CoverImageUrl = b.CoverImageUrl,
                AuthorName = b.AuthorName,
                AuthorUid = b.AuthorUid,
                Domain = b.Domain,
                Tags = b.Tags,
                Status=b.Status,
                ViewCount = b.ViewCount,
                LikesCount = b.LikesCount,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).ToList();

            return Ok(dtoList);
        }

        // GET api/blogs/slug/{slug}
        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetBlogBySlug(string slug)
        {
            var result = await _supabase
                .From<BlogData>()
                .Filter("slug", Operator.Equals, slug)
                .Single();

            if (result == null)
                return NotFound();

            var dto = new BlogDto
            {
                Id = result.Id,
                Title = result.Title,
                Slug = result.Slug,
                Content = result.Content,
                CoverImageUrl = result.CoverImageUrl,
                AuthorName = result.AuthorName,
                AuthorUid = result.AuthorUid,
                Domain = result.Domain,
                Tags = result.Tags,
                ViewCount = result.ViewCount,
                LikesCount = result.LikesCount,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt
            };

            return Ok(dto);
        }
        [HttpGet("likedid/{uid}")]
        public async Task<IActionResult> GetLikedIdAsync(string uid)
        {
            if (string.IsNullOrEmpty(uid))
                return BadRequest("User ID is required");

            try
            {
                // ✅ Parse first
                if (!Guid.TryParse(uid, out var guid))
                    return BadRequest("Invalid User ID format");

                var response = await _supabase
                    .From<BlogUser>()
                    .Where(p => p.UserId == guid)   // 👈 now it's a direct comparison
                    .Get();

                var result = response.Models.FirstOrDefault();

                if (result == null || result.LikeId == null)
                    return Ok(new List<int>());

                return Ok(result.LikeId.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLikedIdAsync: {ex}");
                return StatusCode(500, "Server error fetching liked IDs");
            }
        }
        [HttpGet("historyid/{uid}")]
        public async Task<IActionResult> GetHistoryAsync(Guid uid)
        {
            if (uid == Guid.Empty)
                return BadRequest("User ID is required");

            try
            {
                

                var response = await _supabase
                    .From<HistoryDto>()
                    .Where(p => p.UserId == uid)   // 👈 now it's a direct comparison
                    .Get();

                var result = response.Models.FirstOrDefault();

                if (result == null || result.BlogId == null)
                    return Ok(new List<int>());

                return Ok(result.BlogId.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLikedIdAsync: {ex}");
                return StatusCode(500, "Server error fetching liked IDs");
            }
        }


        [HttpGet("savedid/{uid}")]
        public async Task<IActionResult> GetSavedIdAsync(Guid uid)
        {
            try
            {
                var response = await _supabase
                    .From<BlogUser>()
                    .Where(p => p.UserId == uid)
                    .Get();

                var result = response.Models.FirstOrDefault();

                if (result == null)
                {
                    return Ok(new List<int>());
                }

                // if SavedId is null, return empty list
                var savedIds = result.SavedId ?? Array.Empty<int>();
                return Ok(savedIds.ToList());
            }
            catch (Exception ex)
            {
                // Log error
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }
        [HttpGet("following/{uid}")]
        public async Task<IActionResult> GetFollowingAsync(Guid uid)
        {
            try
            {
                var response = await _supabase
                    .From<BlogUser>()
                    .Where(p => p.UserId == uid)
                    .Get();

                var user = response.Models.FirstOrDefault();

                if (user == null || user.Following == null)
                    return Ok(new List<string>());

                return Ok(user.Following.ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }
        [HttpGet("followers/{uid}")]
        public async Task<IActionResult> GetFollowers(Guid uid)
        {
            if (string.IsNullOrEmpty(uid.ToString()))
                return BadRequest("Invalid UID.");

            var response = await _supabase
                .From<BlogUser>()
                .Where(x => x.UserId == uid)
                .Single();

            if (response == null)
                return NotFound("User not found.");

            return Ok(response.Follower.ToList() ?? new List<string>()); // followers list
        }
        public class LikeIdRequest
        {
            public Guid Uid { get; set; }
        }
        public class LikeRequest
        {
            public int Id { get; set; }
            public bool State { get; set; }
            public Guid UserId { get; set; }
        }
        public class SaveIdRequest
        {
            public Guid Uid {  set; get; }
            public int BlogId { set; get; }
            public bool State {  set; get; }
        }
        [HttpPost("save")]
        public async Task<IActionResult> SaveId([FromBody] SaveIdRequest Model)
        {
            var result = await _supabase.From<BlogUser>().Where(p => p.UserId == Model.Uid).Single();
            if(result.SavedId==null)
            {
                if (Model.State)
                {
                    List<int> sid = new();
                    sid.Add(Model.BlogId);
                    result.SavedId = sid.ToArray();
                }
                else
                {
                    List<int> sid = result.SavedId.ToList();
                    sid.Remove(Model.BlogId);
                    result.SavedId = sid.ToArray();
                }
            }
            else
            {
                if (!result.SavedId.Contains(Model.BlogId) && Model.State)
                {
                    List<int> sid = result.SavedId.ToList();
                    sid.Add(Model.BlogId);
                    result.SavedId = sid.ToArray();
                }
                else
                {
                    List<int> sid = result.SavedId.ToList();
                    sid.Remove(Model.BlogId);
                    result.SavedId = sid.ToArray();
                }
            }

                await _supabase.From<BlogUser>().Update(result);
            return Ok(new {success = true,savedid = result.SavedId});
        }
        
        [HttpPost("like")]
        public async Task<IActionResult> LikeCount([FromBody] LikeRequest request)
        {
            var result = await _supabase.From<BlogData>()
                                        .Where(p => p.Id == request.Id)
                                        .Single();

            if (result == null)
                return NotFound("Post not found");
            var result1 = await _supabase.From<BlogUser>()
                                        .Where(p => p.UserId == request.UserId)
                                        .Single();
            // 2. Increment or Decrement like count
            
            if (request.State)
            {
                result.LikesCount += 1;
                List<int> likeid = result1.LikeId != null? result1.LikeId.ToList(): new List<int>();
                likeid.Add(request.Id);
                int[] likeid1 = likeid.ToArray();
                result1.LikeId = likeid1;
                await _supabase.From<BlogUser>().Update(result1);
            }
            else
            {
                result.LikesCount = Math.Max(0, result.LikesCount - 1);
                List<int> likeid = result1.LikeId != null ? result1.LikeId.ToList() : new List<int>();
                if(likeid.Contains(request.Id))
                {
                    likeid.Remove(request.Id);
                    int[] likeid1 = likeid.ToArray();
                    result1.LikeId = likeid1;
                    await _supabase.From<BlogUser>().Update(result1);
                }
            }
                

            // 3. Update in DB
            await _supabase.From<BlogData>().Update(result);

            return Ok(new { success = true, likes = result.LikesCount,likeid =result1.LikeId });
        }
        [HttpPost("newblog")]
        public async Task<IActionResult> CreateNewBlog([FromBody] NewBlog newBlog)
        {
            if (newBlog == null)
            {
                return BadRequest("Invalid blog data");
            }

            var blogData = new BlogData
            {
                Title = newBlog.Title,
                Slug = newBlog.Slug,
                Content = newBlog.Content,
                CoverImageUrl = newBlog.Image,
                AuthorName = newBlog.AuhtorName,
                AuthorUid = newBlog.AuhtorUID,
                Tags = newBlog.Tags ?? new List<string>(),
                Domain = string.IsNullOrEmpty(newBlog.Domain) ? "General" : newBlog.Domain,
                Status = string.IsNullOrEmpty(newBlog.Status) ? "draft" : newBlog.Status,
                MetaDescription = newBlog.MetaDescription,

                // DB managed or defaults
                ViewCount = 0,
                LikesCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ReadingTime = CalculateReadingTime(newBlog.Content), // optional helper
                PublishedAt = newBlog.Status == "published" ? DateTime.UtcNow : null
            };
            var response = await _supabase.From<BlogData>().Insert(blogData);

            return Ok(new { message = "Blog received successfully", blog = newBlog });
        }
        [HttpPost("follow")]
        public async Task<IActionResult> FollowUserAsync([FromBody] FollowRequest request)
        {
            try
            {
                // 1. Get Reader (who is following)
                var readerResponse = await _supabase
                    .From<BlogUser>()
                    .Where(p => p.UserId == request.ReaderUid)
                    .Get();
                var reader = readerResponse.Models.FirstOrDefault();

                // 2. Get Author (who is being followed)
                var authorResponse = await _supabase
                    .From<BlogUser>()
                    .Where(p => p.UserId == request.AuthorUid)
                    .Get();
                var author = authorResponse.Models.FirstOrDefault();

                if (reader == null || author == null)
                    return BadRequest("Invalid user IDs.");

                // 3. Update Following list of Reader
                var updatedFollowing = (reader.Following ?? new string[0]).ToList();
                if (!updatedFollowing.Contains(request.AuthorUid.ToString()))
                    updatedFollowing.Add(request.AuthorUid.ToString());
                reader.Following = updatedFollowing.ToArray();

                await _supabase.From<BlogUser>().Upsert(reader);

                // 4. Update Followers list of Author
                var updatedFollowers = (author.Follower ?? new string[0]).ToList();
                if (!updatedFollowers.Contains(request.ReaderUid.ToString()))
                    updatedFollowers.Add(request.ReaderUid.ToString());
                author.Follower = updatedFollowers.ToArray();

                await _supabase.From<BlogUser>().Upsert(author);

                return Ok(new { Message = "Followed successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }


        public class FollowRequest
        {
            public Guid ReaderUid { get; set; }  // the person who follows
            public Guid AuthorUid { get; set; }  // the person being followed
        }

        private static long CalculateReadingTime(string content)
        {
            // simple reading time estimate: 200 words per minute
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return (long)Math.Ceiling(wordCount / 150.0);
        }   
        public class UploadResponse
        {
            public Guid Url { get; set; }
        }

    }
}
