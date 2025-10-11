using Microsoft.AspNetCore.Mvc;
using Supabase;
using BlogApp1.Shared;

namespace BlogApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectionsController : ControllerBase
    {
        private readonly Client _supabase;

        public CollectionsController(IConfiguration config)
        {
            var supabaseUrl = config["Supabase:Url"];
            var supabaseKey = config["Supabase:Key"];
            _supabase = new Client(supabaseUrl, supabaseKey);
        }

        // GET: api/collections/{uid}
        [HttpGet("{uid}")]
        public async Task<IActionResult> GetCollections(Guid uid)
        {
            var result = await _supabase.From<CollectionDto>()
                                        .Where(c => c.UserId == uid)
                                        .Get();
            var DtoResponse = result.Models.Select(b => new CollectionResponse
            {
                Id = b.Id,
                UserId = b.UserId,
                CollectionName = b.CollectionName,
                BlogIds = b.BlogIds,
                Description = b.Description,
                CreatedAt = b.CreatedAt,
            }).ToList();
            return Ok(DtoResponse);
        }

        // POST: api/collections
        [HttpPost]
        public async Task<IActionResult> CreateCollection([FromBody] CollectionDto collection)
        {
            collection.Id = Guid.NewGuid();
            collection.CreatedAt = DateTime.UtcNow;

            var result = await _supabase.From<CollectionDto>().Insert(collection);
            return Ok(result.Models.FirstOrDefault());
        }

        // PUT: api/collections/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCollection(Guid id, [FromBody] CollectionDto updated)
        {
            var result = await _supabase.From<CollectionDto>().Where(c => c.Id == id).Update(updated);
            return Ok(result.Models.FirstOrDefault());
        }

        // DELETE: api/collections/{id}
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteCollection(Guid id)
        //{
        //    var result = await _supabase.From<CollectionDto>().Where(c => c.Id == id).Delete();
        //    return Ok(new { deleted = result.Models.Count });
        //}

        // POST: api/collections/addBlog/{id}/{blogId}
        [HttpPost("addBlog/{id}/{blogId}")]
        public async Task<IActionResult> AddBlog(Guid id, int blogId)
        {
            var response = await _supabase.From<CollectionDto>()
                                          .Where(c => c.Id == id)
                                          .Get();

            var collection = response.Models.FirstOrDefault();
            if (collection == null) return NotFound();

            var blogs = collection.BlogIds.ToList();
            if (!blogs.Contains(blogId))
                blogs.Add(blogId);

            collection.BlogIds = blogs.ToArray();

            var updated = await _supabase.From<CollectionDto>().Update(collection);
            return Ok(updated.Models.FirstOrDefault());
        }

        // DELETE: api/collections/removeBlog/{id}/{blogId}
        [HttpDelete("removeBlog/{id}/{blogId}")]
        public async Task<IActionResult> RemoveBlog(Guid id, int blogId)
        {
            var response = await _supabase.From<CollectionDto>()
                                          .Where(c => c.Id == id)
                                          .Get();

            var collection = response.Models.FirstOrDefault();
            if (collection == null) return NotFound();

            collection.BlogIds = collection.BlogIds.Where(b => b != blogId).ToArray();

            var updated = await _supabase.From<CollectionDto>().Update(collection);
            return Ok(updated.Models.FirstOrDefault());
        }
    }
}
