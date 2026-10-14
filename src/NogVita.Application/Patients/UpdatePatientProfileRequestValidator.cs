using FluentValidation;
using NogVita.Application.Common.Validation;

namespace NogVita.Application.Patients;

public sealed class UpdatePatientProfileRequestValidator : AbstractValidator<UpdatePatientProfileRequest>
{
    public UpdatePatientProfileRequestValidator(TimeProvider timeProvider)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.BirthDate).ValidPatientBirthDate(timeProvider);

        RuleFor(x => x.BiologicalSex)
            .IsInEnum().WithMessage("O sexo biológico informado não é válido.");

        RuleFor(x => x.HeightInCm)
            .GreaterThan(0).WithMessage("A altura precisa ser maior que zero.");

        RuleFor(x => x.Goal)
            .IsInEnum().WithMessage("O objetivo informado não é válido.");

        RuleFor(x => x.BedTime)
            .NotEqual(x => x.WakeUpTime)
            .When(x => x.WakeUpTime is not null && x.BedTime is not null)
            .WithMessage("A hora de acordar e a de dormir não podem ser iguais.");
    }
}
