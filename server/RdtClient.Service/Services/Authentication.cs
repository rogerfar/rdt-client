using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using RdtClient.Data.Data;

namespace RdtClient.Service.Services
{
    public class Authentication 
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly UserData _userData;

        public Authentication(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, UserData userData)
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
