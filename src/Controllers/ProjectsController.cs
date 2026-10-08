using ContractWatcher.Core.Models.Projects;
using ContractWatcher.Core.Services.Projects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContractWatcher.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController(ProjectService projectService) : ApiControllerBase
{
    /// <summary>
    /// Проекты текущего пользователя
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProjectResponse>>> GetList(CancellationToken cancellationToken) =>
        await projectService.GetListAsync(CurrentUserId, cancellationToken);

    /// <summary>
    /// Проект по ID. Чужой проект — 404
    /// </summary>
    [HttpGet("{projectId:guid}")]
    public async Task<ActionResult<ProjectResponse>> Get(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projectService.GetAsync(CurrentUserId, projectId, cancellationToken);
        if (project is null)
            return NotFound();

        return project;
    }

    /// <summary>
    /// Создание проекта. Владелец — текущий пользователь
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        CreateProjectRequest request,
        [FromServices] IValidator<CreateProjectRequest> validator,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailed(validation);

        var project = await projectService.CreateAsync(CurrentUserId, request.Name, cancellationToken);

        return CreatedAtAction(nameof(Get), new { projectId = project.Id }, project);
    }
}
