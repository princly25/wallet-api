using Microsoft.AspNetCore.Mvc;
using SimpleWallet.Application.Interfaces;
using SimpleWallet.Api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace SimpleWallet.Api.Controllers;

// Temporary fail-closed policy until per-user wallet ownership authorization exists.
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetWallet(Guid id)
    {
        var wallet = await _walletService.GetWalletAsync(id);
        if (wallet == null)
            return NotFound();

        var response = new WalletResponse
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Balance = wallet.Balance
        };

        return Ok(response);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetWalletsByUserId(Guid userId)
    {
        var wallets = await _walletService.GetWalletsByUserIdAsync(userId);

        var response = wallets.Select(w => new WalletResponse
        {
            Id = w.Id,
            UserId = w.UserId,
            Balance = w.Balance
        });

        return Ok(response);
    }

    [HttpPost()]
    public async Task<ActionResult> CreateWalletAsync([FromBody] CreateWalletRequest request)
    {
        var wallet = await _walletService.CreateWalletAsync(request.UserId);

        var response = new WalletResponse{
            Id = wallet.Id,
            UserId = wallet.UserId,
            Balance = wallet.Balance
        };

        return CreatedAtAction(nameof(GetWallet), new { id = wallet.Id }, response);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWalletAsync(Guid id)
    {
        var wallet = await _walletService.GetWalletAsync(id);
        if (wallet == null)
            return NotFound();

        await _walletService.DeleteWalletAsync(id);
        return NoContent();
    }
}
