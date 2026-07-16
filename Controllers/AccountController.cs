using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using KonXProWebApp.Models;

namespace KonXProWebApp.Controllers
{
    [Route("Account/[action]")]
    public partial class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly IWebHostEnvironment env;
        private readonly IConfiguration configuration;

        private readonly Data.ApplicationIdentityDbContext context;

        public AccountController(Data.ApplicationIdentityDbContext context, IWebHostEnvironment env, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager, IConfiguration configuration)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.env = env;
            this.configuration = configuration;

            this.context = context;
        }

        private IActionResult RedirectWithError(string error, string redirectUrl = null)
        {
             if (!string.IsNullOrEmpty(redirectUrl))
             {
                 return Redirect($"~/Login?error={error}&redirectUrl={Uri.EscapeDataString(redirectUrl.Replace("~", ""))}");
             }
             else
             {
                 return Redirect($"~/Login?error={error}");
             }
        }

        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            if (returnUrl != "/" && !string.IsNullOrEmpty(returnUrl))
            {
                return Redirect($"~/Login?redirectUrl={Uri.EscapeDataString(returnUrl)}");
            }

            return Redirect("~/Login");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string userName, string password, string redirectUrl)
        {
            redirectUrl = string.IsNullOrEmpty(redirectUrl) ? "~/permit-intel/search" : redirectUrl.StartsWith("/") ? redirectUrl : $"~/{redirectUrl}";

            if (env.EnvironmentName == "Development" && userName == "admin" && password == "admin")
            {
                var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Name, "admin"),
                    new Claim(ClaimTypes.Email, "admin")
                };

                roleManager.Roles.ToList().ForEach(r => claims.Add(new Claim(ClaimTypes.Role, r.Name)));
                await signInManager.SignInWithClaimsAsync(new ApplicationUser { UserName = userName, Email = userName }, isPersistent: false, claims);

                return Redirect(redirectUrl);
            }

            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {

                var user = await userManager.FindByNameAsync(userName);

                if (user == null)
                {
                    return RedirectWithError("Invalid user or password", redirectUrl);
                }

                if (!user.EmailConfirmed)
                {
                    return RedirectWithError("User email not confirmed", redirectUrl);
                }

                var isTenantsAdmin = userName == "tenantsadmin";    
                var isTwoFactor = await userManager.GetTwoFactorEnabledAsync(user);
                if (!isTwoFactor && !isTenantsAdmin)
                {
                    await userManager.SetTwoFactorEnabledAsync(user, true);
                }
                var result = await signInManager.PasswordSignInAsync(userName, password, false, false);


                if (result.RequiresTwoFactor && !isTenantsAdmin)
                {
                    var code = await userManager.GenerateTwoFactorTokenAsync(user, "Email");

                    var text = $@"Hi, <br /> <br />
We received your request for a single-use code to use with your KonXProWebApp account. <br /> <br />
Your single-use code is: {code} <br /> <br />
If you didn't request this code, you can safely ignore this email. Someone else might have typed your email address by mistake.";

                    await SendEmailAsync(user.Email, "Your single-use code", text);

                    return Redirect($"~/SecurityCode?email={Uri.EscapeDataString(user.Email)}");
                }
                if (result.Succeeded)
                {

                    if (user != null)
                    {
                        var tenant = context.Tenants.Where(t => t.Id == user.TenantId).FirstOrDefault();
                        if (tenant != null && !tenant.Hosts.Split(',').Where(h => h.Contains(this.HttpContext.Request.Host.Value)).Any())
                        {
                            await signInManager.SignOutAsync();
                            return RedirectWithError("Invalid user or password", redirectUrl);
                        }
                    }
                    return Redirect(redirectUrl);
                }
            }

            return RedirectWithError("Invalid user or password", redirectUrl);
        }

        [HttpPost]
        public async Task<IActionResult> VerifySecurityCode(string code)
        {
            var result = await signInManager.TwoFactorSignInAsync("Email", code, false, false);

            if (!result.Succeeded)
            {
                return RedirectWithError("Invalid security code");
            }

            return Redirect("~/");
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword)
        {
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            {
                return BadRequest("Invalid password");
            }

            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await userManager.FindByIdAsync(id);

            userManager.UserValidators.Clear();
            var result = await userManager.ChangePasswordAsync(user, oldPassword, newPassword);

            if (result.Succeeded)
            {
                return Ok();
            }

            var message = string.Join(", ", result.Errors.Select(error => error.Description));

            return BadRequest(message);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required.");
            }

            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.Email = email;
            user.UserName = email;
            user.NormalizedEmail = email.ToUpperInvariant();
            user.NormalizedUserName = email.ToUpperInvariant();

            userManager.UserValidators.Clear();
            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok();
            }

            var msg = string.Join(", ", result.Errors.Select(error => error.Description));
            return BadRequest(msg);
        }

        [HttpPost]
        public ApplicationAuthenticationState CurrentUser()
        {
            return new ApplicationAuthenticationState
            {
                IsAuthenticated = User.Identity.IsAuthenticated,
                Name = User.Identity.Name,
                Claims = User.Claims.Select(c => new ApplicationClaim { Type = c.Type, Value = c.Value })
            };
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();

            return Redirect("~/");
        }

        [HttpPost]
        public async Task<IActionResult> Register(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                return BadRequest("Invalid user name or password.");
            }

            var user = new ApplicationUser { UserName = userName, Email = userName };

            var tenant = context.Tenants.ToList().Where(t => t.Hosts.Split(',').Where(h => h.Contains(this.HttpContext.Request.Host.Value)).Any()).FirstOrDefault();
            if (tenant != null)
            {
                userManager.UserValidators.Clear();

                if (context.Users.Any(u => u.TenantId == tenant.Id && u.UserName == user.Name))
                {
                    return BadRequest("User with the same name already exist for this tenant.");
                }
            }
            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                try
                {
                    if (Request.Cookies.TryGetValue("cid", out string cidStr) && int.TryParse(cidStr, out int cid))
                    {
                        var dbContext = (Data.db_9f8bee_konxdevContext)HttpContext.RequestServices.GetService(typeof(Data.db_9f8bee_konxdevContext));
                        if (dbContext != null)
                        {
                            var contractor = dbContext.HomeImprovementContractors.FirstOrDefault(c => c.Id == cid);
                            if (contractor != null)
                            {
                                contractor.SalesStatus = "Subscribed";
                                contractor.CampaignConverted = true;
                                contractor.CampaignConvertedAt = DateTime.UtcNow;
                                dbContext.SaveChanges();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error tracking campaign conversion: {ex.Message}");
                }

                try
                {
                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(code));

                    var shortToken = Guid.NewGuid().ToString("N");
                    _confirmationTokens[shortToken] = (user.Id, code, DateTime.UtcNow.AddHours(24));

                    var callbackUrl = $"{Request.Scheme}://{Request.Host}/Verify/ConfirmEmail?token={shortToken}";

                    var text = $@"Hi, <br /> <br />
We received your registration request for KonXProWebApp. <br /> <br />
To confirm your registration please click the following link: <a href=""{callbackUrl}"">confirm your registration</a> <br /> <br />
If you didn't request this registration, you can safely ignore this email. Someone else might have typed your email address by mistake.";                    

                    await SendEmailAsync(user.Email, "Confirm your registration", text);


                    var newUser = context.Users.FirstOrDefault(u => u.TenantId == null && u.UserName == userName);
                    if (newUser != null && tenant != null)
                    {
                        newUser.TenantId = tenant.Id;
                        context.Users.Update(newUser);
                        context.SaveChanges();
                    }

                    return Ok();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error during registration: {ex}");
                    return BadRequest(ex.Message);
                }
            }

            var message = string.Join(", ", result.Errors.Select(error => error.Description));

            return BadRequest(message);
        }

        // In-memory store for email confirmation tokens to keep URLs short and WAF-safe
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string UserId, string Code, DateTime Expiry)> _confirmationTokens = new();

        [HttpGet("/Verify/ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            if (string.IsNullOrEmpty(token) || !_confirmationTokens.TryRemove(token, out var entry))
            {
                return RedirectWithError("Invalid or expired confirmation link");
            }

            if (entry.Expiry < DateTime.UtcNow)
            {
                return RedirectWithError("Confirmation link has expired");
            }

            var user = await userManager.FindByIdAsync(entry.UserId);
            if (user == null)
            {
                return RedirectWithError("Invalid user");
            }

            var code = entry.Code;
            try
            {
                code = System.Text.Encoding.UTF8.GetString(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(code));
            }
            catch
            {
                // Fallback for raw legacy codes
            }

            var result = await userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                return Redirect("~/Login?info=Your registration has been confirmed");
            }

            return RedirectWithError("Invalid user or confirmation code");
        }

        public async Task<IActionResult> ResetPassword(string userName)
        {
            var user = await userManager.FindByNameAsync(userName);

            if (user == null)
            {
                return BadRequest("Invalid user name.");
            }

            try
            {
                var code = await userManager.GeneratePasswordResetTokenAsync(user);
                code = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(code));

                var shortToken = Guid.NewGuid().ToString("N");
                _confirmationTokens[shortToken] = (user.Id, code, DateTime.UtcNow.AddHours(24));

                var callbackUrl = $"{Request.Scheme}://{Request.Host}/Verify/ConfirmPasswordReset?token={shortToken}";

                var body = string.Format(@"<a href=""{0}"">{1}</a>", callbackUrl, "Please confirm your password reset.");

                await SendEmailAsync(user.Email, "Confirm your password reset", body);

                return Ok();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error resetting password: {ex}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("/Verify/ConfirmPasswordReset")]
        public async Task<IActionResult> ConfirmPasswordReset(string token)
        {
            if (string.IsNullOrEmpty(token) || !_confirmationTokens.TryRemove(token, out var entry))
            {
                return Redirect("~/Login?error=Invalid or expired reset link");
            }

            if (entry.Expiry < DateTime.UtcNow)
            {
                return Redirect("~/Login?error=Reset link has expired");
            }

            var user = await userManager.FindByIdAsync(entry.UserId);

            if (user == null)
            {
                return Redirect("~/Login?error=Invalid user");
            }

            var code = entry.Code;
            try
            {
                code = System.Text.Encoding.UTF8.GetString(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(code));
            }
            catch
            {
                // Fallback for raw legacy codes
            }

            var password = GenerateRandomPassword();

            var result = await userManager.ResetPasswordAsync(user, code, password);

            if (result.Succeeded)
            {
                await SendEmailAsync(user.Email, "New password", $"<p>Your new password is: {password}</p><p>Please change it after login.</p>");

                return Redirect("~/Login?info=Password reset successful. You will receive an email with your new password.");
            }

            return Redirect("~/Login?error=Invalid user or confirmation code");
        }

        private static string GenerateRandomPassword()
        {
            var options = new PasswordOptions
            {
                RequiredLength = 8,
                RequiredUniqueChars = 4,
                RequireDigit = true,
                RequireLowercase = true,
                RequireNonAlphanumeric = true,
                RequireUppercase = true
            };

            var randomChars = new[] {
                "ABCDEFGHJKLMNOPQRSTUVWXYZ",
                "abcdefghijkmnopqrstuvwxyz",
                "0123456789",
                "!@$?_-"
            };

            var rand = new Random(Environment.TickCount);
            var chars = new List<char>();

            if (options.RequireUppercase)
            {
                chars.Insert(rand.Next(0, chars.Count), randomChars[0][rand.Next(0, randomChars[0].Length)]);
            }

            if (options.RequireLowercase)
            {
                chars.Insert(rand.Next(0, chars.Count), randomChars[1][rand.Next(0, randomChars[1].Length)]);
            }

            if (options.RequireDigit)
            {
                chars.Insert(rand.Next(0, chars.Count), randomChars[2][rand.Next(0, randomChars[2].Length)]);
            }

            if (options.RequireNonAlphanumeric)
            {
                chars.Insert(rand.Next(0, chars.Count), randomChars[3][rand.Next(0, randomChars[3].Length)]);
            }

            for (int i = chars.Count; i < options.RequiredLength || chars.Distinct().Count() < options.RequiredUniqueChars; i++)
            {
                string rcs = randomChars[rand.Next(0, randomChars.Length)];
                chars.Insert(rand.Next(0, chars.Count), rcs[rand.Next(0, rcs.Length)]);
            }

            return new string(chars.ToArray());
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {

            var mailMessage = new System.Net.Mail.MailMessage();
            mailMessage.From = new System.Net.Mail.MailAddress(configuration.GetValue<string>("Smtp:User"));
            mailMessage.Body = body;
            mailMessage.Subject = subject;
            mailMessage.BodyEncoding = System.Text.Encoding.UTF8;
            mailMessage.SubjectEncoding = System.Text.Encoding.UTF8;
            mailMessage.IsBodyHtml = true;
            mailMessage.To.Add(to);

            var client = new System.Net.Mail.SmtpClient(configuration.GetValue<string>("Smtp:Host"))
            {
                UseDefaultCredentials = false,
                EnableSsl = configuration.GetValue<bool>("Smtp:Ssl"),
                Port = configuration.GetValue<int>("Smtp:Port"),
                Credentials = new System.Net.NetworkCredential(configuration.GetValue<string>("Smtp:User"), configuration.GetValue<string>("Smtp:Password"))
            };

            await client.SendMailAsync(mailMessage);
        }
    }
}
