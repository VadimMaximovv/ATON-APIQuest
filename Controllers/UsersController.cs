using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ATON_APIQuest.Users;

namespace ATON_APIQuest.Controllers
{
    [Route("api/Users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UsersContext _context;

        public UsersController(UsersContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        /*[HttpPut("{id}")]
        public async Task<IActionResult> PutUser(Guid id, [FromBody] User user)
        {
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.Name = user.Name;
            existingUser.Gender = user.Gender;
            existingUser.Birthday = user.Birthday;

            existingUser.ModifiedOn = DateTime.UtcNow;
            existingUser.ModifiedBy = user.Login; // Здесь должен быть логин текущего пользователя

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }*/

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        /*[HttpPost]
        public async Task<ActionResult<User>> PostUserItem(User UserItem)
        {
            _context.Users.Add(UserItem);
            await _context.SaveChangesAsync();

            //    return CreatedAtAction("GetTodoItem", new { id = todoItem.Id }, todoItem);
            return CreatedAtAction(nameof(GetUser), new { id = UserItem.Id }, UserItem);
        }*/


        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(Guid id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        #region Create
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User request, [FromQuery] string adminLogin, string adminPassword)
        {
            var admin = _context.GetByLoginAndPassword(adminLogin, adminPassword);
            if (admin == null || !admin.Admin)
                return Unauthorized("Only admins can create users");

            if (!IsValidLogin(request.Login) || !IsValidPassword(request.Password) || !IsValidName(request.Name))
                return BadRequest("Invalid input data");

            if (_context.LoginExists(request.Login))
                return Conflict("Login already exists");

            var user = new User
            {
                Login = request.Login,
                Password = request.Password,
                Name = request.Name,
                Gender = request.Gender,
                Birthday = request.Birthday,
                Admin = request.Admin,
                CreatedBy = admin.Login
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            //    return CreatedAtAction("GetTodoItem", new { id = todoItem.Id }, todoItem);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        #endregion

        #region Update-1
        [HttpPut("details")]
        public async Task<ActionResult<User>> UpdateUserDetails([FromBody] User request, [FromQuery] string login, [FromQuery] string password)
        {
            var currentUser = _context.GetByLoginAndPassword(login, password);
            if (currentUser == null)
                return Unauthorized("Invalid credentials");

            var userToUpdate = _context.GetByLogin(request.Login);
            if (userToUpdate == null)
                return NotFound("User not found");

            if (!currentUser.Admin && (currentUser.Login != userToUpdate.Login || userToUpdate.RevokedOn != null))
                return Unauthorized("You can only update your own active account");

            if (!IsValidName(request.Name))
                return BadRequest("Invalid name");

            userToUpdate.Name = request.Name;
            userToUpdate.Gender = request.Gender;
            userToUpdate.Birthday = request.Birthday;

            userToUpdate.ModifiedOn = DateTime.UtcNow;
            userToUpdate.ModifiedBy = currentUser.Login;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LoginExists(login))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPut("password")]
        public async Task<ActionResult<User>> UpdatePassword([FromBody] User request, [FromQuery] string login, [FromQuery] string password)
        {
            var currentUser = _context.GetByLoginAndPassword(login, password);
            if (currentUser == null)
                return Unauthorized("Invalid credentials");

            var userToUpdate = _context.GetByLogin(request.Login);
            if (userToUpdate == null)
                return NotFound("User not found");

            if (!currentUser.Admin && (currentUser.Login != userToUpdate.Login || userToUpdate.RevokedOn != null))
                return Unauthorized("You can only update your own active account");

            if (!IsValidPassword(request.Password))
                return BadRequest("Invalid password");

            userToUpdate.Password = request.Password;
            userToUpdate.ModifiedOn = DateTime.UtcNow;
            userToUpdate.ModifiedBy = currentUser.Login;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LoginExists(login))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        //[HttpPut("login")]
        [HttpPut]
        public async Task<ActionResult<User>> UpdateLogin([FromBody] User request, [FromQuery] string login, [FromQuery] string password)
        {
            var currentUser = _context.GetByLoginAndPassword(login, password);
            if (currentUser == null)
                return Unauthorized("Invalid credentials");

            var userToUpdate = _context.GetByLogin(login);
            if (userToUpdate == null)
                return NotFound("User not found");

            if (!currentUser.Admin && (currentUser.Login != userToUpdate.Login || userToUpdate.RevokedOn != null))
                return Unauthorized("You can only update your own active account");

            if (!IsValidLogin(request.Login))
                return BadRequest("Invalid login");

            if (_context.LoginExists(request.Login))
                return Conflict("Login already exists");

            userToUpdate.Login = request.Login;
            userToUpdate.ModifiedOn = DateTime.UtcNow;
            userToUpdate.ModifiedBy = currentUser.Login;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LoginExists(login))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        #endregion

        #region Helpers
        private static bool IsValidLogin(string login)
        {
            return !string.IsNullOrWhiteSpace(login) && login.All(c => char.IsLetterOrDigit(c) && c < 128);

        }

        private static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.All(c => char.IsLetterOrDigit(c) && c < 128);
        }

        private static bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.All(c => char.IsLetter(c) || c == ' ');
        }
        #endregion
    }
}
