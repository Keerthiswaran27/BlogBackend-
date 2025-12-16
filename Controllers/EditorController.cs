using BlogApp1.Shared;

using BlogApp1.Shared.EditorModels;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using static Supabase.Postgrest.Constants;

namespace BlogApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EditorController : ControllerBase
    {
        private readonly Client _supabase;

        public EditorController(IConfiguration config)
        {
            var supabaseUrl = config["Supabase:Url"];
            var supabaseKey = config["Supabase:Key"];
            _supabase = new Client(supabaseUrl, supabaseKey);
        }

        // ==============================
        // 1️⃣ GET: api/editor/blogs?status=pending
        // ==============================
        [HttpGet("blogs")]
        public async Task<IActionResult> GetBlogs([FromQuery] string status = "pending")
        {
            var result = await _supabase.From<BlogData>()
                                        .Where(b => b.Status == status)
                                        .Get();

            var response = result.Models.Select(b => new BlogSummaryModel
            {
                Id = b.Id,
                Title = b.Title,
                AuthorName = b.AuthorName,
                Domain = b.Domain,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                ReviewedAt = b.ReviewedAt,
                EditorUid = b.EditorUid
            }).ToList();

            return Ok(response);
        }

        // ==============================
        // 2️⃣ GET: api/editor/blog/{id}
        // ==============================
        [HttpGet("blog/{id}")]
        public async Task<IActionResult> GetBlog(int id)
        {
            var result = await _supabase.From<BlogData>()
                                        .Where(b => b.Id == id)
                                        .Single();

            if (result == null)
                return NotFound();

            var b = result;

            var detail = new BlogDetailModel
            {
                Id = b.Id,
                Title = b.Title,
                Content = b.Content,
                AuthorName = b.AuthorName,
                AuthorUid = b.AuthorUid,
                Domain = b.Domain,
                Tags = b.Tags,
                CoverImageUrl = b.CoverImageUrl,
                Status = b.Status,
                //ReviewComments = b.ReviewComments,
                //RejectionReason = b.RejectionReason,
                EditorUid = b.EditorUid,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                ReviewedAt = b.ReviewedAt,
                Slug = b.Slug,
                MetaDescription = b.MetaDescription,
                ReadingTime = b.ReadingTime
                
            };

            return Ok(detail);
        }

        // ==============================
        // 3️⃣ POST: api/editor/approve/{id}
        // ==============================
        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveBlog(int id, [FromBody] BlogDetailModel model)
        {
            var response = await _supabase.From<BlogData>()
                                          .Where(b => b.Id == id)
                                          .Get();

            var blog = response.Models.FirstOrDefault();
            if (blog == null) return NotFound();

            blog.Status = "approved";
            blog.EditorUid = model.EditorUid;
            //blog.ReviewComments = model.ReviewComments;
            blog.ReviewedAt = DateTime.UtcNow;

            var updated = await _supabase.From<BlogData>().Update(blog);
            return Ok(new { message = "Blog approved successfully." });
        }

        // ==============================
        // 4️⃣ POST: api/editor/reject/{id}
        // ==============================

        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectBlog(int id, [FromBody] RejectRequest model)
        {
            var response = await _supabase.From<BlogData>()
                                          .Where(b => b.Id == id)
                                          .Get();

            var blog = response.Models.FirstOrDefault();
            if (blog == null) return NotFound();

            // ✅ Apply rejection updates
            blog.Status = "rejected";
            blog.EditorUid = model.EditorUid;//add rejecction
            blog.ReviewedAt = DateTime.UtcNow;

            await _supabase.From<BlogData>().Update(blog);

            return Ok(new { message = "Blog rejected." });
        }

        public class RejectRequest
        {
            public string? EditorUid { get; set; }
            public string? RejectionReason { get; set; }
        }


        // ==============================
        // 5️⃣ POST: api/editor/revise/{id}
        // ==============================
        [HttpPost("revise/{id}")]
        public async Task<IActionResult> SendBackForRevision(int id, [FromBody] FeedbackModel feedback)
        {
            // Add feedback message
            feedback.CreatedAt = DateTime.UtcNow;
            var feedbackData = new EditorFeedback
            {
                BlogId = feedback.BlogId,
                AuthorUid = feedback.AuthorUid,
                EditorUid = feedback.EditorUid,
                FeedbackType = feedback.FeedbackType,
                Message = feedback.Message,
                CreatedAt = DateTime.UtcNow,
                IsAuthorVisible = feedback.IsAuthorVisible
            };

            await _supabase.From<EditorFeedback>().Insert(new List<EditorFeedback> { feedbackData });


            // Update blog status
            var result = await _supabase.From<BlogData>()
                                        .Where(b => b.Id == id)
                                        .Get();

            var blog = result.Models.FirstOrDefault();
            if (blog == null) return NotFound();

            blog.Status = "pending";
            await _supabase.From<BlogData>().Update(blog);

            return Ok(new { message = "Sent back for revision." });
        }

        // ==============================
        // 6️⃣ POST: api/editor/update-content/{id}
        // ==============================
        [HttpPost("update-content/{id}")]
        public async Task<IActionResult> UpdateBlogContent(int id, [FromBody] RevisionModel revision)
        {
            // Insert new revision
            revision.CreatedAt = DateTime.UtcNow;
            var revisionData = new BlogRevisions
            {
                BlogId = revision.BlogId,
                VersionNo = revision.VersionNo,
                Content = revision.Content,
                EditorUid = revision.EditorUid,
                AuthorUid = revision.AuthorUid,
                IsCurrent = revision.IsCurrent,
                CreatedAt = DateTime.UtcNow,
                
            };

            await _supabase.From<BlogRevisions>().Insert(new List<BlogRevisions> { revisionData });

            // Update main blog content
            var result = await _supabase.From<BlogData>()
                                        .Where(b => b.Id == id)
                                        .Get();

            var blog = result.Models.FirstOrDefault();
            if (blog == null) return NotFound();

            blog.Content = revision.Content;
            blog.UpdatedAt = DateTime.UtcNow;

            await _supabase.From<BlogData>().Update(blog);
            return Ok(new { message = "Content updated successfully." });
        }

        // ==============================
        // 7️⃣ GET: api/editor/revisions/{blog_id}
        // ==============================
        [HttpGet("revisions/{blog_id}")]
        public async Task<IActionResult> GetRevisions(int blog_id)
        {
            var result = await _supabase.From<BlogRevisions>()
                                        .Where(r => r.BlogId == blog_id)
                                        .Order("created_at", Ordering.Ascending)
                                        .Get();

            return Ok(result.Models);
        }

        // ==============================
        // 8️⃣ POST: api/editor/revisions/restore/{version_id}
        // ==============================
        [HttpPost("revisions/restore/{version_id}")]
        public async Task<IActionResult> RestoreRevision(int version_id)
        {
            var response = await _supabase.From<BlogRevisions>()
                                          .Where(r => r.Id == version_id)
                                          .Get();

            var revision = response.Models.FirstOrDefault();
            if (revision == null) return NotFound();

            // Update main blog content
            var blog = await _supabase.From<BlogData>()
                                      .Where(b => b.Id == revision.BlogId)
                                      .Get();

            var target = blog.Models.FirstOrDefault();
            if (target == null) return NotFound();

            target.Content = revision.Content;
            target.UpdatedAt = DateTime.UtcNow;

            await _supabase.From<BlogData>().Update(target);

            return Ok(new { message = "Version restored successfully." });
        }

        // ==============================
        // 9️⃣ GET: api/editor/feedback/{blog_id}
        // ==============================
        [HttpGet("feedback/{blog_id}")]
        public async Task<IActionResult> GetFeedback(int blog_id)
        {
            var result = await _supabase.From<EditorFeedback>()
                                        .Where(f => f.BlogId == blog_id)
                                        .Order("created_at", Ordering.Ascending)
                                        .Get();

            return Ok(result.Models);
        }

        // ==============================
        // 🔟 GET: api/editor/analytics/{editor_uid}
        // ==============================
        [HttpGet("analytics/{editor_uid}")]
        public async Task<IActionResult> GetAnalytics(string editor_uid)
        {
            // Get blogs reviewed by this editor
            var result = await _supabase.From<BlogData>()
                                        .Where(b => b.EditorUid == editor_uid)
                                        .Get();

            var blogs = result.Models;

            var totalReviewed = blogs.Count;
            var totalApproved = blogs.Count(b => b.Status == "approved");
            var totalRejected = blogs.Count(b => b.Status == "rejected");
            var totalPending = blogs.Count(b => b.Status == "pending");

            // Calculate average review time (if ReviewedAt and CreatedAt exist)
            var reviewTimes = blogs
                .Where(b => b.ReviewedAt.HasValue)
                .Select(b => (b.ReviewedAt.Value - b.CreatedAt).TotalHours)
                .ToList();

            double avgReviewTime = reviewTimes.Count > 0 ? reviewTimes.Average() : 0;

            var analytics = new AnalyticsModel
            {
                TotalReviewed = totalReviewed,
                TotalApproved = totalApproved,
                TotalRejected = totalRejected,
                TotalPending = totalPending,
                AverageReviewTimeHours = Math.Round(avgReviewTime, 2)
            };

            return Ok(analytics);
        }
        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateBlog(int id, [FromBody] BlogDetailModel model)
        {
            try
            {
                var response = await _supabase.From<BlogData>()
                                              .Where(b => b.Id == id)
                                              .Get();

                var blog = response.Models.FirstOrDefault();
                if (blog == null)
                    return NotFound(new { message = "Blog not found." });

                // --- Update basic fields (only if sent) ---
                if (!string.IsNullOrWhiteSpace(model.Title))
                    blog.Title = model.Title;

                if (!string.IsNullOrWhiteSpace(model.Slug))
                    blog.Slug = model.Slug;

                if (!string.IsNullOrWhiteSpace(model.MetaDescription))
                    blog.MetaDescription = model.MetaDescription;

                if (!string.IsNullOrWhiteSpace(model.Domain))
                    blog.Domain = model.Domain;

                if (model.Tags != null && model.Tags.Count > 0)
                    blog.Tags = model.Tags.ToList();

                if (!string.IsNullOrWhiteSpace(model.CoverImageUrl))
                    blog.CoverImageUrl = model.CoverImageUrl;

                if (!string.IsNullOrWhiteSpace(model.Content))
                    blog.Content = model.Content;

                // --- Update review/status info if applicable ---
                if (!string.IsNullOrWhiteSpace(model.Status))
                    blog.Status = model.Status;

                blog.EditorUid = model.EditorUid;
                blog.UpdatedAt = DateTime.UtcNow;

                await _supabase.From<BlogData>().Update(blog);

                return Ok(new { message = "Blog updated successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EditorController.UpdateBlog] Error: {ex.Message}");
                return StatusCode(500, new { message = "Error updating blog.", error = ex.Message });
            }
        }
        [HttpGet("dashboardstats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            // pending
            var pendingResult = await _supabase.From<BlogData>()
                .Where(b => b.Status == "pending")
                .Get();
            int pendingCount = pendingResult.Models.Count;

            // approved
            var approvedResult = await _supabase.From<BlogData>()
                .Where(b => b.Status == "approved")
                .Get();
            int approvedCount = approvedResult.Models.Count;

            // rejected
            var rejectedResult = await _supabase.From<BlogData>()
                .Where(b => b.Status == "rejected")
                .Get();
            int rejectedCount = rejectedResult.Models.Count;

            // revisions count (assuming you have table blog_revisions)
            var revisionsResult = await _supabase.From<BlogRevisions>()
                .Get();
            int revisionCount = revisionsResult.Models.Count;

            var stats = new DashboardStatsModel
            {
                PendingCount = pendingCount,
                ApprovedCount = approvedCount,
                RejectedCount = rejectedCount,
                RevisionCount = revisionCount,
            };

            return Ok(stats);
        }
    }
}
