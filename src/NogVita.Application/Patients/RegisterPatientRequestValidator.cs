using FluentValidation;
using NogVita.Application.Common.Validation;
using NogVita.Domain.Users;

namespace NogVita.Application.Patients;

public sealed class RegisterPatientRequestValidator : AbstractValidator<RegisterPatientRequest>
{
    public RegisterPatientRequestValidator(TimeProvider timeProvider)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(150).WithMessage("O nome pode ter no máximo 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .MaximumLength(254).WithMessage("O e-mail pode ter no máximo 254 caracteres.")
            .EmailAddress().WithMessage("O e-mail informado não é válido.");

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("O CPF é obrigatório.")
            .Must(Cpf.IsValid).WithMessage("O CPF informado não é válido.");

        RuleFor(x => x.Password).StrongPassword();

        RuleFor(x => x.BirthDate)
            .Must(birthDate => birthDate <= DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("A data de nascimento não pode estar no futuro.");

        RuleFor(x => x.BiologicalSex)
            .IsInEnum().WithMessage("O sexo biológico informado não é válido.");

        RuleFor(x => x.HeightInCm)
            .GreaterThan(0).WithMessage("A altura precisa ser maior que zero.");

        RuleFor(x => x.Goal)
            .IsInEnum().WithMessage("O objetivo informado não é válido.");
    }
}