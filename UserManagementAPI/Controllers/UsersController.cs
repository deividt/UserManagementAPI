using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace UserManagementAPI.Controllers;

// User model
public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s'-\.]+$", ErrorMessage = "Name contains invalid characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(254, ErrorMessage = "Email must not exceed 254 characters")]
    public string Email { get; set; } = string.Empty;
}

[ApiController]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // Static list to store users
    private static readonly List<User> Users =
    [
        new() { Id = 1, Name = "John Doe", Email = "john.doe@example.com" },
        new() { Id = 2, Name = "Jane Smith", Email = "jane.smith@example.com" },
        new() { Id = 3, Name = "Bob Johnson", Email = "bob.johnson@example.com" }
    ];

    // GET: api/users
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpGet]
    public ActionResult<object> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? nameFilter = null,
        [FromQuery] string? emailFilter = null,
        [FromQuery] string sortBy = "id",
        [FromQuery] string sortOrder = "asc")
    {
        try
        {
            // Validate query parameters
            if (page < 1)
            {
                return BadRequest(new { 
                    Message = "Invalid page number", 
                    Error = "Page number must be greater than 0",
                    ProvidedPage = page 
                });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { 
                    Message = "Invalid page size", 
                    Error = "Page size must be between 1 and 100",
                    ProvidedPageSize = pageSize 
                });
            }

            var validSortFields = new[] { "id", "name", "email" };
            if (!validSortFields.Contains(sortBy.ToLower()))
            {
                return BadRequest(new { 
                    Message = "Invalid sort field", 
                    Error = $"Sort field must be one of: {string.Join(", ", validSortFields)}",
                    ProvidedSortBy = sortBy 
                });
            }

            var validSortOrders = new[] { "asc", "desc" };
            if (!validSortOrders.Contains(sortOrder.ToLower()))
            {
                return BadRequest(new { 
                    Message = "Invalid sort order", 
                    Error = "Sort order must be 'asc' or 'desc'",
                    ProvidedSortOrder = sortOrder 
                });
            }

            // Start with all users
            var query = Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(nameFilter))
            {
                query = query.Where(u => u.Name.Contains(nameFilter.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(emailFilter))
            {
                query = query.Where(u => u.Email.Contains(emailFilter.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder.ToLower() == "desc" 
                    ? query.OrderByDescending(u => u.Name) 
                    : query.OrderBy(u => u.Name),
                "email" => sortOrder.ToLower() == "desc" 
                    ? query.OrderByDescending(u => u.Email) 
                    : query.OrderBy(u => u.Email),
                _ => sortOrder.ToLower() == "desc" 
                    ? query.OrderByDescending(u => u.Id) 
                    : query.OrderBy(u => u.Id)
            };

            var totalUsers = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

            // Apply pagination
            var pagedUsers = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new
            {
                Data = pagedUsers,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalUsers = totalUsers,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                },
                Filters = new
                {
                    NameFilter = nameFilter,
                    EmailFilter = emailFilter
                },
                Sorting = new
                {
                    SortBy = sortBy,
                    SortOrder = sortOrder
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new {
                Message = "Internal server error",
                Error = "An unexpected error occurred while retrieving users",
                Details = ex.Message
            });
        }
    }

    // GET: api/users/{id}
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpGet("{id:int}")]
    public ActionResult<User> GetUser(int id)
    {
        try
        {
            // Validate ID parameter
            if (id <= 0)
            {
                return BadRequest(new { 
                    Message = "Invalid user ID", 
                    Error = "User ID must be a positive integer greater than 0",
                    ProvidedId = id 
                });
            }

            var user = Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new { 
                    Message = "User not found", 
                    Error = $"No user exists with ID {id}",
                    RequestedId = id,
                    AvailableUserIds = Users.Select(u => u.Id).ToList()
                });
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new {
                Message = "Internal server error",
                Error = "An unexpected error occurred while retrieving the user",
                Details = ex.Message
            });
        }
    }

    // POST: api/users
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<User> CreateUser(User user)
    {
        try
        {
            // Check if user object is null
            if (user == null)
            {
                return BadRequest("User data is required.");
            }

            // Validate model state (checks DataAnnotations)
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Message = "Validation failed", Errors = errors });
            }

            // Trim whitespace from inputs
            user.Name = user.Name?.Trim() ?? string.Empty;
            user.Email = user.Email?.Trim() ?? string.Empty;

            // Additional manual validation
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                return BadRequest("Name cannot be empty or contain only whitespace.");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return BadRequest("Email cannot be empty or contain only whitespace.");
            }

            // Validate email format with regex as additional check
            var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.IgnoreCase);
            if (!emailRegex.IsMatch(user.Email))
            {
                return BadRequest("Please provide a valid email address.");
            }

            // Check if email already exists
            if (Users.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("A user with this email already exists.");
            }

            // Generate new ID
            user.Id = Users.Count > 0 ? Users.Max(u => u.Id) + 1 : 1;
            Users.Add(user);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                $"An error occurred while creating the user: {ex.Message}");
        }
    }

    // PUT: api/users/{id}
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPut("{id:int}")]
    public IActionResult UpdateUser(int id, User user)
    {
        try
        {
            // Validate ID parameter
            if (id <= 0)
            {
                return BadRequest("Invalid user ID provided.");
            }

            // Check if user object is null
            if (user == null)
            {
                return BadRequest("User data is required.");
            }

            var existingUser = Users.FirstOrDefault(u => u.Id == id);
            if (existingUser == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            // Validate model state (checks DataAnnotations)
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Message = "Validation failed", Errors = errors });
            }

            // Trim whitespace from inputs
            user.Name = user.Name?.Trim() ?? string.Empty;
            user.Email = user.Email?.Trim() ?? string.Empty;

            // Additional manual validation
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                return BadRequest("Name cannot be empty or contain only whitespace.");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return BadRequest("Email cannot be empty or contain only whitespace.");
            }

            // Validate email format with regex as additional check
            var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.IgnoreCase);
            if (!emailRegex.IsMatch(user.Email))
            {
                return BadRequest("Please provide a valid email address.");
            }

            // Check if email already exists for another user
            if (Users.Any(u => u.Id != id && u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("A user with this email already exists.");
            }

            existingUser.Name = user.Name;
            existingUser.Email = user.Email;

            return Ok(existingUser);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                $"An error occurred while updating the user: {ex.Message}");
        }
    }

    // DELETE: api/users/{id}
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpDelete("{id:int}")]
    public IActionResult DeleteUser(int id)
    {
        try
        {
            // Validate ID parameter
            if (id <= 0)
            {
                return BadRequest(new { 
                    Message = "Invalid user ID", 
                    Error = "User ID must be a positive integer greater than 0",
                    ProvidedId = id 
                });
            }

            var user = Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new { 
                    Message = "User not found", 
                    Error = $"Cannot delete user: No user exists with ID {id}",
                    RequestedId = id,
                    AvailableUserIds = Users.Select(u => u.Id).ToList()
                });
            }

            Users.Remove(user);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new {
                Message = "Internal server error",
                Error = "An unexpected error occurred while deleting the user",
                Details = ex.Message
            });
        }
    }
}