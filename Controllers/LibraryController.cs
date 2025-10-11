using BlogApp1.Shared;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using BlogApp1.Shared;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using static Supabase.Postgrest.Constants;
namespace BlogApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LibraryController : ControllerBase
    {
        private readonly Client _supabase;

        public LibraryController(IConfiguration config)
        {
            var supabaseUrl = config["Supabase:Url"];
            var supabaseKey = config["Supabase:Key"];
            _supabase = new Client(supabaseUrl, supabaseKey);
        }

        // ------------------------
        // 1. Get Reading History
        // ------------------------
        [HttpGet("historyid/{uid}")]
        public async Task<IActionResult> GetHistoryAsync(Guid uid)
        {
            if (uid == Guid.Empty)
                return BadRequest("User ID is required");

            try
            {
                var response = await _supabase
                    .From<HistoryDto>()
                    .Where(p => p.UserId == uid)
                    .Get();

                var result = response.Models.FirstOrDefault();

                if (result == null || result.BlogId == null)
                    return Ok(new List<int>());

                // Map to clean DTO
                var mapped = new HistoryResponse
                {
                    Id = result.Id,
                    UserId = result.UserId,
                    BlogId = result.BlogId,
                    ViewedAt = result.ViewedAt
                };

                return Ok(mapped);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetHistoryAsync: {ex}");
                return StatusCode(500, "Server error fetching history");
            }
        }


        // ------------------------
        // 2. Add Blog to History
        // ------------------------
        [HttpPost("history/add")]
        public async Task<IActionResult> AddToHistory([FromBody] HistoryDto item)
        {
            var inserted = await _supabase.From<HistoryDto>().Insert(item);
            return Ok(inserted.Models.FirstOrDefault());
        }

        // ------------------------
        // 3. Get Collections for User
        // ------------------------
        [HttpGet("collections/{userId}")]
        public async Task<IActionResult> GetCollections(Guid userId)
        {
            var collections = await _supabase.From<CollectionDto>()
                                             .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                                             .Get();

            return Ok(collections.Models);
        }

        // ------------------------
        // 4. Add Blog to Collection
        // ------------------------
        [HttpPost("collections/add")]
        public async Task<IActionResult> AddToCollection([FromBody] CollectionDto item)
        {
            var inserted = await _supabase.From<CollectionDto>().Insert(item);
            return Ok(inserted.Models.FirstOrDefault());
        }
        // 5. Remove Blog from Collection
        [HttpDelete("collections/remove/{collectionId}/{blogId}")]
        public async Task<IActionResult> RemoveFromCollection(Guid collectionId, int blogId)
        {
            // Delete the collection row matching collectionId and blogId
            await _supabase.From<CollectionDto>()
                           .Filter("collection_id", Supabase.Postgrest.Constants.Operator.Equals, collectionId)
                           .Filter("blog_id", Supabase.Postgrest.Constants.Operator.Equals, blogId)
                           .Delete();

            // Just return success message
            return Ok(new { message = "Blog removed from collection successfully." });
        }

    }

}
