using ContractWatcher.Common.Contracts;
using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Models.Contracts;
using ContractWatcher.Core.Services.Sdk;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

/// <summary>
/// Эндпоинты для SDK. Доступ — по ключу интеграции (X-Api-Key), JWT пользователя здесь не принимается
/// </summary>
[ApiController]
[Authorize(Policy = AuthExtension.SdkPolicy)]
[Route("api/sdk/contracts")]
public class SdkContractsController(SdkContractService sdkContractService) : ControllerBase
{
    /// <summary>
    /// Опубликованный контракт по slug. Ищется только в интеграции ключа. Нет контракта или публикаций — 404
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<PublishedContract>> Get(string slug, CancellationToken cancellationToken)
    {
        var contract = await sdkContractService.GetPublishedAsync(User.GetIntegrationId(), slug, cancellationToken);
        if (contract is null)
            return NotFound();

        return contract;
    }

    /// <summary>
    /// Публикация контракта, построенного SDK по коду, в интеграции ключа. Создан контракт или новая версия — 201,
    /// правила совпали с последней опубликованной версией — 200. В ответе версия, по которой SDK отправляет отчёты.
    /// Параллельные запросы не дали сохранить — 409
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PublishedContract>> Publish(
        ScannedContractRequest request,
        [FromServices] IValidator<ScannedContractRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var (status, contract) = await sdkContractService.PublishScannedAsync(
            User.GetIntegrationId(),
            request,
            cancellationToken
        );

        if (status == SdkContractPublishStatus.Conflict)
            return Problem(title: "Contract was changed concurrently, retry", statusCode: StatusCodes.Status409Conflict);

        if (status == SdkContractPublishStatus.Created)
            return CreatedAtAction(nameof(Get), new { slug = contract!.Slug }, contract);

        return contract!;
    }
}
