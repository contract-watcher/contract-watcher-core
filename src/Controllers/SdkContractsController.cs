using ContractWatcher.Common.Contracts;
using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Services.Sdk;
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
}
