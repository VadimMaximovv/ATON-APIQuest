using Microsoft.EntityFrameworkCore;


namespace ATON_APIQuest.Users
{
    public class UsersContext : DbContext
    {
        public UsersContext(DbContextOptions<UsersContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
    }
}
