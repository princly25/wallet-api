using Microsoft.AspNetCore.Mvc;
using SimpleWallet.Application.Interfaces;
using SimpleWallet.Api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace SimpleWallet.Api.Controllers;

// Personal details are not public; owner-based access will follow in a later PR.
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
	private readonly IUserService _userService;

	public UserController(IUserService userService)
	{
		_userService = userService;
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> GetUserAsync(Guid id)
	{
		var user = await _userService.GetUserAsync(id);
		if (user == null)
			return NotFound();

		var response = new UserResponse
		{
			Id = user.Id,
			Name = user.Name,
			Email = user.Email
		};

		return Ok(response);
	}

	[Authorize(Roles = "Admin")]
	[HttpPatch("{id}")]
	public async Task<IActionResult> PatchUserAsync(Guid id, [FromBody] UpdateUserPatchRequest request)
	{
		await _userService.UpdateProfileAsync(id, request.Name, request.Email);
		return NoContent();
	}
}

