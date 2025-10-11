using BlogApp1.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Gotrue;
using Supabase.Interfaces;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlogApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly Supabase.Client _supabase;

        public AuthController()
        {
            var options = new SupabaseOptions { AutoConnectRealtime = false };

            // ⛳ REPLACE with your actual Supabase URL and Anon Key
            _supabase = new Supabase.Client(
                "https://baynmesisxklgmqerbbo.supabase.co",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJheW5tZXNpc3hrbGdtcWVyYmJvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDczMDcxMjAsImV4cCI6MjA2Mjg4MzEyMH0.3AouI9q_9KzfiVxKu_zJzgyd_HEfL6RUaiJ6mE777v4",
                options);
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupModel model)
        {
            try
            {
                var response = await _supabase.Auth.SignUp(model.Email, model.Password, new SignUpOptions
                {
                    Data = new Dictionary<string, object>
                    {
                        { "name", model.Name },
                        { "role", model.Role }
                    }
                });
                if (response.User != null)
                {
                    var Profile = new BlogUser
                    {
                        UserId = Guid.Parse(    response.User.Id),
                        FullName = model.Name,
                        Email = model.Email,
                        Roles = model.Role?.ToArray()

                    };
                    var result = await _supabase.From<BlogUser>().Insert(Profile);
                    if (result.Models == null || !result.Models.Any())
                    {
                        return StatusCode(500, "Signup succeeded, but failed to insert user details.");
                    }
                }
                return Ok("Signup succeeded and user details stored.");


            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Signup failed: {ex.Message}");
            }
        }
        //[HttpPost("signin")]
        //public async Task<IActionResult> Signin([FromBody] SignInModel model)
        //{
        //    try
        //    {
        //        var session = await _supabase.Auth.SignInWithPassword(model.Email, model.Password);

        //        if (session?.User == null)
        //            return BadRequest("Invalid credentials");

        //        return Ok(new
        //        {
        //            AccessToken = session.AccessToken,
        //            RefreshToken = session.RefreshToken,
        //            UserId = session.User.Id,
        //            Email = session.User.Email
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Signin failed: {ex.Message}");
        //    }
        //}
        [HttpPost("signin")]
        public async Task<IActionResult> Signin([FromBody] SignInModel model)
        {

            try
            {
                var session = await _supabase.Auth.SignInWithPassword(model.Email, model.Password);

                if (session?.User != null)
                {
                    var user_details = new
                    {
                        AccessToken = session.AccessToken,
                        Uid = session.User.Id
                    };
                    return Ok(user_details);
                }
                else
                {
                    return BadRequest("not connected");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Signin failed: {ex.Message}");
            }
        }
        [HttpGet("user-info")]
        public async Task<IActionResult> GetUserInfo([FromQuery] Guid uid)
        {
            try
            {
                var response = await _supabase
                    .From<BlogUser>()
                    .Where(b => b.UserId == uid)
                    .Get();

                var user = response.Models.FirstOrDefault();

                if (user == null)
                    return NotFound("User record not found");

                // Return the user info
                return Ok(new
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Roles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving user info: {ex.Message}");
            }
        }
        //[HttpGet("get-following/{userId}")]
        //public async Task<IActionResult> GetFollowing(Guid userId)
        //{
        //    try
        //    {
        //        // Step 1: Get the list of following IDs
        //        //var followings = await _supabase
        //        //    .From<BlogUser>()
        //        //    .Where(f => f.UserId == userId)
        //        //    .Get();

        //        //var followingIds = followings.Model

        //        //if (!followingIds.Any())
        //        //    return Ok(new List<BlogUser>()); // return empty if none

        //        //// Step 2: Get the users matching those IDs
        //        //var users = await _supabase
        //        //    .From<BlogUser>()
        //        //    .Get(); // fetch all users (Supabase doesn’t support .Where(u => followingIds.Contains(u.Id)))

        //        //var filteredUsers = users.Models
        //        //    .Where(u => followingIds.Contains(u.Id)) // filter in memory
        //        //    .ToList();

        //        return Ok(filteredUsers);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { error = ex.Message });
        //    }
        //}

    }
}
