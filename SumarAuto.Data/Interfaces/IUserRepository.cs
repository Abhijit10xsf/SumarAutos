using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Interfaces
{
    public interface IUserRepository
    {
        User Authenticate(string emailOrAccount, string password);
        bool Register(User newUser, out string errorMessage);
        User GetUserByEmail(string email);
        User GetUserById(int id);
    }
}
