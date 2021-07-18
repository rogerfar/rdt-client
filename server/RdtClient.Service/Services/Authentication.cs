using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using RdtClient.Data.Data;

namespace RdtClient.Service.Services
{
    public interface IAuthentication
    {
        Task<IdentityResult> Register(String userName, String password);
        Task<SignInResult> Login(String userName, String password);
        Task<IdentityUser> GetUser();
        Task Logout();
    }

    public class Authentication : IAuthentication
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserData _userData;

        public Authentication(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IUserData userData)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userData = userData;
        }

        public async Task<IdentityResult> Register(String userName, String password)
        {
            var user = new IdentityUser(userName);

            var result = await _userManager.CreateAsync(user, password);

            return result;
        }

        public async Task<SignInResult> Login(String userName, String password)
        {
            if (String.IsNullOrWhiteSpace(userName) || String.IsNullOrWhiteSpace(password))
            {
                return SignInResult.Failed;
            }

            var result = await _signInManager.PasswordSignInAsync(userName, password, true, false);

            return result;
        }

        public async Task<IdentityUser> GetUser()
        {
            return await _userData.GetUser();
        }

        public async Task Logout()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
