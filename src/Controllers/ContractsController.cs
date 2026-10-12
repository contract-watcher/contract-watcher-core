using ContractWatcher.Core.Models.Contracts;
using ContractWatcher.Core.Services.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/integrations/{integrationId:guid}/contracts")]
public class ContractsController(ContractService contractService) : ApiControllerBase
{
    /// <summary>
    /// Контракты интеграции
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ContractResponse>>> GetList(
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        var contracts = await contractService.GetListAsync(CurrentUserId, integrationId, cancellationToken);
        if (contracts is null)
            return NotFound();

        return contracts;
    }
    
    /// <summary>
    /// Контракт по ID
    /// </summary>
    [HttpGet("{contractId:guid}")]
    public async Task<ActionResult<ContractResponse>> Get(
        Guid integrationId,
        Guid contractId,
        CancellationToken cancellationToken
    )
    {
        var contract = await contractService.GetAsync(CurrentUserId, integrationId, contractId, cancellationToken);
        if (contract is null)
            return NotFound();

        return contract;
    }

    /// <summary>
    /// Создание контракта. Занятый в интеграции slug — 409
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ContractResponse>> Create(
        Guid integrationId,
        CreateContractRequest request,
        [FromServices] IValidator<CreateContractRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var (status, contract) = await contractService.CreateAsync(
            CurrentUserId,
            integrationId,
            request,
            cancellationToken
        );

        if (status == ContractCreateStatus.IntegrationNotFound)
            return NotFound();

        if (status == ContractCreateStatus.SlugTaken)
            return Problem(title: "Slug is already used in this integration", statusCode: StatusCodes.Status409Conflict);

        return CreatedAtAction(nameof(Get), new { integrationId, contractId = contract!.Id }, contract);
    }
}
