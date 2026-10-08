using ContractWatcher.Core.Data.Domain;
using FluentValidation;

namespace ContractWatcher.Core.Models.ApiKeys;

/// <summary>
/// Запрос на выпуск ключа интеграции
/// </summary>
public record CreateApiKeyRequest(string Name);

public class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
{
    public CreateApiKeyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ApiKey.NameMaxLength);
    }
}
