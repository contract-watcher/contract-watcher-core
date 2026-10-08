using ContractWatcher.Core.Models.ApiKeys;
using ContractWatcher.Core.Services.ApiKeys;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/integrations/{integrationId:guid}/keys")]
public class ApiKeysController(ApiKeyService apiKeyService) : ApiControllerBase
{
    /// <summary>
    /// Ключи интеграции: маска, статус, дата последнего использования
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ApiKeyResponse>>> GetList(
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        var keys = await apiKeyService.GetListAsync(CurrentUserId, integrationId, cancellationToken);
        if (keys is null)
            return NotFound();

        return keys;
    }

    /// <summary>
    /// Выпуск ключа. Полный ключ есть только в этом ответе
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreatedApiKeyResponse>> Create(
        Guid integrationId,
        CreateApiKeyRequest request,
        [FromServices] IValidator<CreateApiKeyRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var key = await apiKeyService.CreateAsync(CurrentUserId, integrationId, request.Name, cancellationToken);
        if (key is null)
            return NotFound();

        return StatusCode(StatusCodes.Status201Created, key);
    }

    /// <summary>
    /// Отзыв ключа. После отзыва SDK с этим ключом получит отказ
    /// </summary>
    [HttpPost("{keyId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid integrationId, Guid keyId, CancellationToken cancellationToken)
    {
        var found = await apiKeyService.RevokeAsync(CurrentUserId, integrationId, keyId, cancellationToken);
        if (!found)
            return NotFound();

        return NoContent();
    }
}
