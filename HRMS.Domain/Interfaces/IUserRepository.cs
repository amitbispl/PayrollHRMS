using HRMS.Domain.Entities;

namespace HRMS.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
    }
}
