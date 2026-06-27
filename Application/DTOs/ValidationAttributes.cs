using System.ComponentModel.DataAnnotations;

namespace SphereScheduleAPI.Application.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // FutureDateAttribute - Validates that a date is in the future
    // Skips validation when value is null (allows partial updates)
    // ─────────────────────────────────────────────────────────────────────────
    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Skip validation if value is null (field not provided in request)
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                if (dateTimeOffset > DateTimeOffset.UtcNow)
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult(ErrorMessage ?? "Date must be in the future");
            }

            if (value is DateTime dateTime)
            {
                if (dateTime > DateTime.UtcNow)
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult(ErrorMessage ?? "Date must be in the future");
            }

            return new ValidationResult("Invalid date format");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PastDateAttribute - Validates that a date is in the past
    // Skips validation when value is null (allows partial updates)
    // ─────────────────────────────────────────────────────────────────────────
    public class PastDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Skip validation if value is null (field not provided in request)
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                if (dateTimeOffset < DateTimeOffset.UtcNow)
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult(ErrorMessage ?? "Date must be in the past");
            }

            if (value is DateTime dateTime)
            {
                if (dateTime < DateTime.UtcNow)
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult(ErrorMessage ?? "Date must be in the past");
            }

            return new ValidationResult("Invalid date format");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DateAfterAttribute - Validates that EndDateTime is after StartDateTime
    // Only validates when BOTH dates are provided in the request
    // Skips validation if either field is null (partial update)
    // ─────────────────────────────────────────────────────────────────────────
    public class DateAfterAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateAfterAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Skip validation if EndDateTime is not provided in request
            if (value == null)
            {
                return ValidationResult.Success;
            }

            // Get the current value (EndDateTime from request)
            DateTimeOffset? currentValue = null;

            if (value is DateTimeOffset dto)
            {
                currentValue = dto;
            }
            else if (value is DateTime dt)
            {
                currentValue = new DateTimeOffset(dt, TimeSpan.Zero);
            }
            else
            {
                return new ValidationResult("Invalid date format");
            }

            // Get the comparison property (StartDateTime from request)
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);
            if (property == null)
            {
                return new ValidationResult($"Unknown property: {_comparisonProperty}");
            }

            var comparisonRaw = property.GetValue(validationContext.ObjectInstance);

            // Skip validation if StartDateTime is not provided in request
            if (comparisonRaw == null)
            {
                return ValidationResult.Success;
            }

            DateTimeOffset? comparisonValue = null;

            if (comparisonRaw is DateTimeOffset comparisonDto)
            {
                comparisonValue = comparisonDto;
            }
            else if (comparisonRaw is DateTime comparisonDt)
            {
                comparisonValue = new DateTimeOffset(comparisonDt, TimeSpan.Zero);
            }
            else
            {
                return ValidationResult.Success;
            }

            // Both dates were provided in the request - validate them
            if (currentValue.Value > comparisonValue.Value)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage ?? "End date/time must be after start date/time");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DateRangeAttribute - Validates that a date is within a range
    // Skips validation when value is null (allows partial updates)
    // ─────────────────────────────────────────────────────────────────────────
    public class DateRangeAttribute : ValidationAttribute
    {
        private readonly string _minDateProperty;
        private readonly string _maxDateProperty;

        public DateRangeAttribute(string minDateProperty, string maxDateProperty)
        {
            _minDateProperty = minDateProperty;
            _maxDateProperty = maxDateProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Skip validation if value is null (field not provided in request)
            if (value == null)
            {
                return ValidationResult.Success;
            }

            DateTimeOffset? currentValue = null;

            if (value is DateTimeOffset dto)
            {
                currentValue = dto;
            }
            else if (value is DateTime dt)
            {
                currentValue = new DateTimeOffset(dt, TimeSpan.Zero);
            }
            else
            {
                return new ValidationResult("Invalid date format");
            }

            // Get min date from request
            var minProperty = validationContext.ObjectType.GetProperty(_minDateProperty);
            DateTimeOffset? minValue = null;
            if (minProperty != null)
            {
                var minRaw = minProperty.GetValue(validationContext.ObjectInstance);
                if (minRaw != null)
                {
                    if (minRaw is DateTimeOffset minDto)
                    {
                        minValue = minDto;
                    }
                    else if (minRaw is DateTime minDt)
                    {
                        minValue = new DateTimeOffset(minDt, TimeSpan.Zero);
                    }
                }
            }

            // Get max date from request
            var maxProperty = validationContext.ObjectType.GetProperty(_maxDateProperty);
            DateTimeOffset? maxValue = null;
            if (maxProperty != null)
            {
                var maxRaw = maxProperty.GetValue(validationContext.ObjectInstance);
                if (maxRaw != null)
                {
                    if (maxRaw is DateTimeOffset maxDto)
                    {
                        maxValue = maxDto;
                    }
                    else if (maxRaw is DateTime maxDt)
                    {
                        maxValue = new DateTimeOffset(maxDt, TimeSpan.Zero);
                    }
                }
            }

            // Skip validation if range boundaries are not provided in request
            if (!minValue.HasValue && !maxValue.HasValue)
            {
                return ValidationResult.Success;
            }

            // Validate against min date
            if (minValue.HasValue && currentValue.Value < minValue.Value)
            {
                return new ValidationResult(ErrorMessage ?? $"Date must be after {_minDateProperty}");
            }

            // Validate against max date
            if (maxValue.HasValue && currentValue.Value > maxValue.Value)
            {
                return new ValidationResult(ErrorMessage ?? $"Date must be before {_maxDateProperty}");
            }

            return ValidationResult.Success;
        }
    }
}