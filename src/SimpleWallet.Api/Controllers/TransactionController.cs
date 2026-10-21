using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SimpleWallet.Application.Interfaces;
using SimpleWallet.Api.DTOs;

namespace SimpleWallet.Api.Controllers;

// Until user identities are mapped to wallet ownership, monetary operations
// are restricted to administrators rather than exposed to any authenticated user.
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly IWalletService _walletService;

    public TransactionController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> DepositAsync([FromBody] DepositRequest request)
    {
        await _walletService.DepositAsync(request.WalletId, request.Amount);
        return NoContent();
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> WithdrawAsync([FromBody] WithdrawRequest request)
    {
        await _walletService.WithdrawAsync(request.WalletId, request.Amount);
        return NoContent();
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> TransferAsync([FromBody] TransferRequest request)
    {
        await _walletService.TransferAsync(request.FromWalletId, request.ToWalletId, request.Amount);
        return NoContent();
    }
}
