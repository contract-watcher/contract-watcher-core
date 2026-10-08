using ContractWatcher.Core.Data.Domain;
using FluentValidation;

namespace ContractWatcher.Core.Models.Projects;

/// <summary>
/// Запрос на создание проекта
/// </summary>
public record CreateProjectRequest(string Name);

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Project.NameMaxLength);
    }
}
