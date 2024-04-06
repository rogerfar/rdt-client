using Microsoft.AspNetCore.Identity;
using RdtClient.Data.Data;

namespace RdtClient.Service.Services;

public class Authentication(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, UserData userData)
{
    public async Task<IdentityResult> Register(String userName, String password)
    {
        var user = new IdentityUser(userName);

        var result = await userManager.CreateAsync(user, password);

        return result;
    }

    public async Task<SignInResult> Login(String userName, String password)
    {
        if (String.IsNullOrWhiteSpace(userName) || String.IsNullOrWhiteSpace(password))
        {
            return SignInResult.Failed;
        }

        var result = await signInManager.PasswordSignInAsync(userName, password, true, false);

        return result;
    }

    public async Task<IdentityUser?> GetUser()
    {
        return await userData.GetUser();
    }

    public async Task Logout()
    {
        await signInManager.SignOutAsync();
    }

    public async Task<IdentityResult> Update(String newUserName, String newPassword)
    {
        var user = await GetUser() ?? throw new("No logged in user found");

        if (!String.IsNullOrWhiteSpace(newUserName))
        {
            user.UserName = newUserName;
        }

        await userManager.UpdateAsync(user);

        if (!String.IsNullOrWhiteSpace(newPassword))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, newPassword);

            return result;
        }

        return IdentityResult.Success;
    }
}