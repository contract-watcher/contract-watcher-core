using ContractWatcher.Core.Models.Contracts;
using ContractWatcher.Core.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/contracts/{contractId:guid}/versions")]
public class ContractVersionController(ContractVersionService contractVersionService) : ApiControllerBase
{
    /// <summary>
    /// Версии контракта
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ContractVersionResponse>>> GetList(
        Guid contractId,
        CancellationToken cancellationToken
    )
    {
        var versions = await contractVersionService.GetListAsync(CurrentUserId, contractId, cancellationToken);
        if (versions is null)
            return NotFound();

        return versions;
    }
}
