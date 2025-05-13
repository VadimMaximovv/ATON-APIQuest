using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace ATON_APIQuest.Users
{
    public class UsersContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;

        public UsersContext(DbContextOptions<UsersContext> options) : base(options) 
        {
            if (GetByLoginAndPassword("Admin", "Admin123") == null)
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
            
        }

        public User? GetByLoginAndPassword(string login, string password)
        {
            return Users
                .Where(u => u.Login == login && u.Password == password)
                .AsEnumerable()
                .FirstOrDefault();
        }

        public User? GetByLogin(string login)
        {
            return Users.Where(u => u.Login == login)
                .AsEnumerable()
                .FirstOrDefault();
        }

        public User? GetUsersOlderThan(int age)
        {
            return Users.Where(u => u.Birthday.HasValue && DateTime.Today.Year - u.Birthday.Value.Year > age)
                .AsEnumerable()
                .FirstOrDefault(u => u.RevokedOn == null);

        }
        public bool LoginExists(string login)
        {
            return Users.Any(u => u.Login==login);
        }
    }
}
