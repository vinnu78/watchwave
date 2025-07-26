using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using WatchWave.Models.GenericRepo;
using WatchWave.Models.Repo;
using System.IO;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace WatchWave.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepo _userRepo;
        private readonly IRecordsRepo _recordsRepo;

        public AccountController(IUserRepo userRepo, IRecordsRepo recordsRepo)
        {
            _userRepo = userRepo;
            _recordsRepo = recordsRepo;
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(UserSignUpModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userRepo.SignUpAsync(model);
                if (result.Succeeded)
                {
                    object data = "A Confirmation Email Has Been Sent To Your Email Address. Please Check Your Email And Click The Confirmation Link To Activate Your Account.";
                    return View("ThankYou", data);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LogIn(UserLogInModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var result = await _userRepo.LogInAsync(model);
                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "You need to confirm your email. Check your inbox.");
                }
                else if (result.Succeeded)
                {
                    return !string.IsNullOrEmpty(returnUrl)
                        ? LocalRedirect(returnUrl)
                        : RedirectToAction("GetMovie", "Collection");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid Username or Password");
                }
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Email is required.");
                return View();
            }

            var token = await _userRepo.GenerateResetPasswordTokenAsync(email);
            if (string.IsNullOrEmpty(token))
            {
                return View("ThankYou", "User with this email does not exist.");
            }

            var resetLink = Url.Action("ResetPassword", "Account", new { email, token }, Request.Scheme);

            // ✅ ADDED: Keeping reset link logic as requested
            return View("ThankYou", $"Reset Password Link: {resetLink}");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            var model = new ResetPasswordModel { Email = email, Token = token }; // ✅ ADDED
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _userRepo.ResetPasswordAsync(model);
            if (result.Succeeded)
            {
                return View("ThankYou", "Your password has been reset successfully.");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _userRepo.LogoutAsync();
            return RedirectToAction("GetMovie", "Collection");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult SignUp() => View();

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            ViewBag.EmailData = await _userRepo.GetUserEmailAsync(User.Identity.Name);

            // Fetching profile picture path from the database or file system
            var profilePicturePath = await _userRepo.GetUserProfilePictureAsync(User.Identity.Name);

            // If no profile picture exists, set the default image
            if (string.IsNullOrEmpty(profilePicturePath))
            {
                ViewBag.ProfilePicturePath = "~/images/profiles/default.png"; // Default image path
            }
            else
            {
                ViewBag.ProfilePicturePath = $"~/images/profiles/{User.Identity.Name}{Path.GetExtension(profilePicturePath)}"; // User's custom profile picture
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var list = await _recordsRepo.GetRecordsByUserIdAsync(userId);
            return View(new Tuple<List<Records>, UpdateUserModel>(list, new UpdateUserModel()));
        }

        [Authorize]
        public async Task<IActionResult> DeleteRecord(string deleteButton)
        {
            var del = JsonSerializer.Deserialize<Records>(deleteButton); // ✅ ADDED
            _recordsRepo.Delete(del);
            await _recordsRepo.SaveChangesAsync();
            return RedirectToAction("Profile");
        }

        [Authorize]
        public async Task<IActionResult> RequestMovie(string name, int year, string type)
        {
            string cookieNameMovie = "movie_name_" + HttpContext.User.Identity.Name;
            if (HttpContext.Request.Cookies.ContainsKey(cookieNameMovie))
            {
                return View("ThankYou", "You have already requested a movie. Try again after 24 hours.");
            }

            try
            {
                _recordsRepo.Add(new Records
                {
                    Name = name,
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    Year = year,
                    Type = type
                });

                await _recordsRepo.SaveChangesAsync();
                HttpContext.Response.Cookies.Append(cookieNameMovie, name, new CookieOptions { Expires = DateTime.Now.AddDays(1) });
                return View("ThankYou", $"Your requested movie '{name}' has been received.");
            }
            catch (Exception ex)
            {
                return View("ThankYou", ex.Message);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditRecord(int id, string name, int year, string type)
        {
            if (ModelState.IsValid)
            {
                var record = _recordsRepo.GetById(id);
                record.Name = name;
                record.Year = year;
                record.Type = type;
                record.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _recordsRepo.Update(record);
                await _recordsRepo.SaveChangesAsync();
            }
            return RedirectToAction("Profile");
        }

        [Authorize]
        public IActionResult EditRecord(int id) => View("EditRecord", _recordsRepo.GetById(id));

        [Authorize]
        [HttpPost]
        public async Task<string> UpdatePassword(UpdateUserModel model)
        {
            if (!ModelState.IsValid)
                return "New Password and Confirm Password Do Not Match";

            var user = await _userRepo.GetUserAsync(User);
            var result = await _userRepo.UpdatePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            return result.Succeeded ? "Password Updated Successfully" : "Current Password Is Incorrect";
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin() => View(await _userRepo.GetAllUsersAsync());

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string deleteButton)
        {
            var user = JsonSerializer.Deserialize<AppUser>(deleteButton); // ✅ ADDED
            await _userRepo.DeleteUserAsync(user);
            return RedirectToAction("Admin");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userRepo.GetUserByIdAsync(id); // This should return Task<AppUser>
            if (user == null)
            {
                return NotFound();
            }

            return View("EditUser", user);
        }

        public async Task<IActionResult> UpdateUser(string id)
        {
            var user = await _userRepo.GetUserByIdAsync(id);
            return View(user);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmail(string id, string email)
        {
            await _userRepo.UpdateEmailAsync(id, email);
            return RedirectToAction("Admin");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture(List<IFormFile> updatedProfilePicture)
        {
            if (updatedProfilePicture.Count == 0)
                return BadRequest("Error");

            if (ModelState.IsValid)
            {
                var result = await _userRepo.UpdateProfilePictureAsync(updatedProfilePicture[0], User.Identity.Name);
                return Content(result ? "Profile Picture Updated Successfully" : "Error While Updating The Profile Picture");
            }
            return Content("Error While Updating The Profile Picture");
        }
    }
}
