using Microsoft.EntityFrameworkCore;

namespace ATON_APIQuest.Users
{
    public class UsersContext : DbContext
    {
        public UsersContext(DbContextOptions<UsersContext> options) : base(options) 
        {
            var admin = new User
            {
                Login = "Admin",
                Password = "Admin123",
                Name = "System Administrator",
                Gender = 1,
                Birthday = null,
                Admin = true,
                CreatedBy = "System"
            };

            Users.AddAsync(admin);
            SaveChangesAsync();
        }

        public DbSet<User> Users { get; set; } = null!;
    }
}
