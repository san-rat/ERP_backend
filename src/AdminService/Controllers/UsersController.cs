using AdminService.Models;
using AdminService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class UsersController : ControllerBase
{
    private readonly IAdminUserService _userService;

    public UsersController(IAdminUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("managers")]
    [ProducesResponseType(typeof(CreateStaffResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateManager([FromBody] CreateStaffRequest request)
    {
        try
        {
            var response = await _userService.CreateStaffAsync(request, RoleNormalizer.Manager, HttpContext.RequestAborted);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (AdminValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (AdminConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("employees")]
    [ProducesResponseType(typeof(CreateStaffResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateStaffRequest request)
    {
        try
        {
            var response = await _userService.CreateStaffAsync(request, RoleNormalizer.Employee, HttpContext.RequestAborted);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (AdminValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (AdminConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StaffListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers([FromQuery] UserListQuery query)
    {
        try
        {
            var response = await _userService.GetUsersAsync(query, HttpContext.RequestAborted);
            return Ok(response);
        }
        catch (AdminValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StaffListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateStaffRequest request)
    {
        try
        {
            var response = await _userService.UpdateUserAsync(id, request, HttpContext.RequestAborted);
            return Ok(response);
        }
        catch (AdminValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (AdminConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (AdminNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(StaffListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStaffStatusRequest request)
    {
        try
        {
            var response = await _userService.UpdateUserStatusAsync(id, request, HttpContext.RequestAborted);
            return Ok(response);
        }
        catch (AdminValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (AdminNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest? _ = null)
    {
        try
        {
            var response = await _userService.ResetPasswordAsync(id, HttpContext.RequestAborted);
            return Ok(response);
        }
        catch (AdminValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (AdminNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
