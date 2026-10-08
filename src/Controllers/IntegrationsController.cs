using ContractWatcher.Core.Models.Integrations;
using ContractWatcher.Core.Services.Integrations;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/integrations")]
public class IntegrationsController(IntegrationService integrationService) : ApiControllerBase
{
    /// <summary>
    /// Интеграции проекта
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<IntegrationResponse>>> GetList(
        Guid projectId,
        CancellationToken cancellationToken
    )
    {
        var integrations = await integrationService.GetListAsync(CurrentUserId, projectId, cancellationToken);
        if (integrations is null)
            return NotFound();

        return integrations;
    }

    /// <summary>
    /// Интеграция по ID
    /// </summary>
    [HttpGet("{integrationId:guid}")]
    public async Task<ActionResult<IntegrationResponse>> Get(
        Guid projectId,
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        var integration = await integrationService.GetAsync(
            CurrentUserId,
            projectId,
            integrationId,
            cancellationToken
        );
        if (integration is null)
            return NotFound();

        return integration;
    }

    /// <summary>
    /// Создание интеграции. Занятый в проекте slug — 409
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<IntegrationResponse>> Create(
        Guid projectId,
        IntegrationRequest request,
        [FromServices] IValidator<IntegrationRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var (status, integration) = await integrationService.CreateAsync(
            CurrentUserId,
            projectId,
            request,
            cancellationToken
        );

        if (status == IntegrationSaveStatus.NotFound)
            return NotFound();

        if (status == IntegrationSaveStatus.SlugTaken)
            return SlugTaken();

        return CreatedAtAction(nameof(Get), new { projectId, integrationId = integration!.Id }, integration);
    }

    /// <summary>
    /// Изменение интеграции: название, slug, описание
    /// </summary>
    [HttpPut("{integrationId:guid}")]
    public async Task<ActionResult<IntegrationResponse>> Update(
        Guid projectId,
        Guid integrationId,
        IntegrationRequest request,
        [FromServices] IValidator<IntegrationRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var (status, integration) = await integrationService.UpdateAsync(
            CurrentUserId,
            projectId,
            integrationId,
            request,
            cancellationToken
        );

        if (status == IntegrationSaveStatus.NotFound)
            return NotFound();

        if (status == IntegrationSaveStatus.SlugTaken)
            return SlugTaken();

        return integration!;
    }

    /// <summary>
    /// Удаление интеграции вместе с её ключами и контрактами
    /// </summary>
    [HttpDelete("{integrationId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid integrationId, CancellationToken cancellationToken)
    {
        var deleted = await integrationService.DeleteAsync(CurrentUserId, projectId, integrationId, cancellationToken);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    private ObjectResult SlugTaken() =>
        Problem(title: "Slug is already used in this project", statusCode: StatusCodes.Status409Conflict);
}
