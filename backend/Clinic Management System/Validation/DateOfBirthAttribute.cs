using Clinic_Business.Entities;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.Validation
{
    /// <summary>
    /// Reports the existing business rule (not in the future, age 0-100) as a per-field 400
    /// instead of a bare Save() == false. The rule itself still lives in
    /// PersonValidators.IsValidDateOfBirth — this only surfaces it over HTTP, so the two
    /// can never disagree.
    /// </summary>
    public sealed class DateOfBirthAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return ValidationResult.Success;

            if (value is DateOnly dateOfBirth && PersonValidators.IsValidDateOfBirth(dateOfBirth))
                return ValidationResult.Success;

            return new ValidationResult(
                "Date of birth must be in the past and imply an age of 100 or less.",
                new[] { validationContext.MemberName! });
        }
    }
}
