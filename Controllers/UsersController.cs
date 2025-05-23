﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ATON_APIQuest.Models.Users;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;

namespace ATON_APIQuest.Controllers
{
    [Route("api/Users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UsersService _service;

        public UsersController(UsersService service)
        {
            _service = service;
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var user = await _service.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        #region Create
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User request, [FromQuery] string adminLogin, string adminPassword)
        {
            var admin = _service.GetByLoginAndPassword(adminLogin, adminPassword);
            if (admin == null || !admin.Admin)
                return Unauthorized("Only admins can create users");

            if (!IsValidLogin(request.Login) || !IsValidPassword(request.Password) || !IsValidName(request.Name))
                return BadRequest("Invalid input data");

            if (_service.LoginExists(request.Login))
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

            _service.Users.Add(user);
            await _service.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        #endregion

        #region Update-1
        [HttpPut("details")]
        public async Task<ActionResult<User>> UpdateUserDetails([FromBody] User request, [FromQuery] string login, [FromQuery] string password)
        {
            var currentUser = _service.GetByLoginAndPassword(login, password);
            if (currentUser == null)
                return Unauthorized("Invalid credentials");

            var userToUpdate = _service.GetByLogin(request.Login);
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

            return await TryToSave(login);
        }

        [HttpPut("password")]
        public async Task<ActionResult<User>> UpdatePassword([FromBody] User request, [FromQuery] string login, [FromQuery] string password)
        {
            var currentUser = _service.GetByLoginAndPassword(login, password);
            if (currentUser == null)
                return Unauthorized("Invalid credentials");

            var userToUpdate = _service.GetByLogin(request.Login);
            if (userToUpdate == null)
                return NotFound("User not found");

            if (!currentUser.Admin && (currentUser.Login != userToUpdate.Login || userToUpdate.RevokedOn != null))
                return Unauthorized("You can only update your own active account");

            if (!IsValidPassword(request.Password))
                return BadRequest("Invalid password");

            userToUpdate.Password = request.Password;
            userToUpdate.ModifiedOn = DateTime.UtcNow;
            userToUpdate.ModifiedBy = currentUser.Login;

            return await TryToSave(login);
        }

        [HttpPut("login")]
        public async Task<ActionResult<User>> UpdateLogin([FromBody] User request, [FromQuery] string login, [FromQuery] string password)
        {
            var currentUser = _service.GetByLoginAndPassword(login, password);
            if (currentUser == null)
                return Unauthorized("Invalid credentials");

            var userToUpdate = _service.GetByLogin(login);
            if (userToUpdate == null)
                return NotFound("User not found");

            if (!currentUser.Admin && (currentUser.Login != userToUpdate.Login || userToUpdate.RevokedOn != null))
                return Unauthorized("You can only update your own active account");

            if (!IsValidLogin(request.Login))
                return BadRequest("Invalid login");

            if (_service.LoginExists(request.Login))
                return Conflict("Login already exists");

            userToUpdate.Login = request.Login;
            userToUpdate.ModifiedOn = DateTime.UtcNow;
            userToUpdate.ModifiedBy = currentUser.Login;

            return await TryToSave(login);
        }
        #endregion

        #region Read
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAllActiveUsers([FromQuery] string adminLogin, [FromQuery] string adminPassword)
        {
            var admin = _service.GetByLoginAndPassword(adminLogin, adminPassword);
            if (admin == null || !admin.Admin)
                return Unauthorized("Only admins can view all active users");

            return await _service.Users.ToListAsync();
        }

        [HttpGet("login/{login}")]
        public async Task<ActionResult<User>> GetUserByLogin(string login, [FromQuery] string adminLogin, [FromQuery] string adminPassword)
        {
            var admin = _service.GetByLoginAndPassword(adminLogin, adminPassword);
            if (admin == null || !admin.Admin)
                return Unauthorized("Only admins can view user details");

            var user = _service.GetByLogin(login);
            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                user.Name,
                user.Gender,
                user.Birthday,
                IsActive = user.RevokedOn == null
            });
        }
        
        [HttpGet("self")]
        public async Task<ActionResult<User>> GetCurrentUser([FromQuery] string login, [FromQuery] string password)
        {
            var user = _service.GetByLoginAndPassword(login, password);
            if (user == null || user.RevokedOn != null)
                return Unauthorized("Invalid credentials or inactive account");

            return user;
        }
        
        
        [HttpGet("older-than/{age}")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersOlderThan(int age, [FromQuery] string adminLogin, [FromQuery] string adminPassword)
        {
            var admin = _service.GetByLoginAndPassword(adminLogin, adminPassword);
            if (admin == null || !admin.Admin)
                return Unauthorized("Only admins can view users by age");

            return Ok(_service.GetUsersOlderThan(age));
        }
        #endregion

        #region Delete
        [HttpDelete("{login}")]
        public async Task<IActionResult> DeleteUser(string login, [FromQuery] bool softDelete, [FromQuery] string adminLogin, [FromQuery] string adminPassword)
        {
            var admin = _service.GetByLoginAndPassword(adminLogin, adminPassword);
            if (admin == null || !admin.Admin)
                return Unauthorized("Only admins can delete users");

            var userToDelete = _service.GetByLogin(login);
            if (userToDelete == null)
                return NotFound("User not found");
            
            if (softDelete)
            {
                userToDelete.RevokedOn = DateTime.UtcNow;
                userToDelete.RevokedBy = admin.Login;

                await _service.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                _service.Users.Remove(userToDelete);
                await _service.SaveChangesAsync();

                return NoContent();
            }    
        }
        #endregion

        #region Update-2
        [HttpPut("restore/{login}")]
        public async Task<ActionResult<User>> RestoreUser(string login, [FromQuery] string adminLogin, [FromQuery] string adminPassword)
        {
            var admin = _service.GetByLoginAndPassword(adminLogin, adminPassword);
            if (admin == null || !admin.Admin)
                return Unauthorized("Only admins can restore users");

            var userToRestore = _service.GetByLogin(login);
            if (userToRestore == null)
                return NotFound("User not found");

            userToRestore.RevokedOn = null;
            userToRestore.RevokedBy = null;

            await _service.SaveChangesAsync();
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

        private async Task<ActionResult<User>> TryToSave(string login)
        {
            try
            {
                await _service.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_service.LoginExists(login))
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
    }
}
