using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Interfaces
{
    public interface IUserRepository
    {
        User Authenticate(string username, string password);
    }
}
