using ContractWatcher.Core.Models.Contracts;
using ContractWatcher.Core.Services.Contracts;
using FluentValidation;
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

    /// <summary>
    /// Версия контракта по номеру
    /// </summary>
    [HttpGet("{version:int}")]
    public async Task<ActionResult<ContractVersionResponse>> Get(
        Guid contractId,
        int version,
        CancellationToken cancellationToken
    )
    {
        var contractVersion = await contractVersionService.GetAsync(CurrentUserId, contractId, version, cancellationToken);
        if (contractVersion is null)
            return NotFound();

        return contractVersion;
    }

    /// <summary>
    /// Создание версии контракта. Номер уже занят параллельным запросом — 409
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ContractVersionResponse>> Create(
        Guid contractId,
        CreateContractVersionRequest request,
        [FromServices] IValidator<CreateContractVersionRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var (status, version) = await contractVersionService.CreateAsync(
            CurrentUserId,
            contractId,
            request,
            cancellationToken
        );

        if (status == ContractVersionCreateStatus.ContractNotFound)
            return NotFound();

        if (status == ContractVersionCreateStatus.VersionConflict)
            return Problem(title: "Version was created concurrently, retry", statusCode: StatusCodes.Status409Conflict);

        return CreatedAtAction(nameof(Get), new { contractId, version = version!.Version }, version);
    }
}
