using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace RdtClient.Data.Data
{
    public class UserData
    {
        private readonly DataContext _dataContext;

        public UserData(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IdentityUser> GetUser()
        {
            return await _dataContext.Users.FirstOrDefaultAsync();
        }
    }
}
