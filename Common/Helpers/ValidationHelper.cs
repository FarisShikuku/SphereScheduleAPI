using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;


namespace SphereScheduleAPI.Common.Helpers
{
    public static class ValidationHelper
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PhoneRegex = new Regex(
            @"^\+?[1-9]\d{1,14}$", // E.164 format
            RegexOptions.Compiled);

        private static readonly Regex UrlRegex = new Regex(
            @"^(https?|ftp)://[^\s/$.?#].[^\s]*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex UsernameRegex = new Regex(
            @"^[a-zA-Z0-9._-]{3,50}$",
            RegexOptions.Compiled);

        private static readonly Regex PasswordRegex = new Regex(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            RegexOptions.Compiled);

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return EmailRegex.IsMatch(email) && new EmailAddressAttribute().IsValid(email);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            try
            {
                return PhoneRegex.IsMatch(phoneNumber);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                return UrlRegex.IsMatch(url) && Uri.TryCreate(url, UriKind.Absolute, out _);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            try
            {
                return UsernameRegex.IsMatch(username);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                return PasswordRegex.IsMatch(password);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidGuid(string guidString)
        {
            if (string.IsNullOrWhiteSpace(guidString))
                return false;

            return Guid.TryParse(guidString, out _);
        }

        public static bool IsValidDateRange(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            return startDate < endDate;
        }

        public static bool IsFutureDate(DateTimeOffset date)
        {
            return date > DateTimeOffset.UtcNow;
        }

        public static bool IsPastDate(DateTimeOffset date)
        {
            return date < DateTimeOffset.UtcNow;
        }

        public static bool IsWithinRange<T>(T value, T min, T max) where T : IComparable<T>
        {
            return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
        }

        public static bool IsNullOrEmpty<T>(IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }

        public static bool HasDuplicates<T>(IEnumerable<T> collection)
        {
            if (collection == null)
                return false;

            var set = new HashSet<T>();
            foreach (var item in collection)
            {
                if (!set.Add(item))
                    return true;
            }
            return false;
        }

        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                System.Text.Json.JsonDocument.Parse(json);
                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }

        public static bool IsValidBase64(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return false;

            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool IsValidHexColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return false;

            return Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
        }

        public static bool IsValidTimeZone(string timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return false;

            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return timeZone != null;
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
        }

        public static bool IsValidLanguageCode(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return false;

            try
            {
                var culture = new System.Globalization.CultureInfo(languageCode);
                return culture != null;
            }
            catch (System.Globalization.CultureNotFoundException)
            {
                return false;
            }
        }

        public static bool IsValidCountryCode(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
                return false;

            try
            {
                var region = new System.Globalization.RegionInfo(countryCode);
                return region != null;
            }
            catch (System.Globalization.CultureNotFoundException)
            {
                return false;
            }
        }

        public static bool IsValidCurrencyCode(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
                return false;

            try
            {
                var culture = System.Globalization.CultureInfo
                    .GetCultures(System.Globalization.CultureTypes.AllCultures)
                    .FirstOrDefault(c => new System.Globalization.RegionInfo(c.LCID).ISOCurrencySymbol == currencyCode);

                return culture != null;
            }
            catch
            {
                return false;
            }
        }

        public static string SanitizeHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            // Basic HTML sanitization - in production, use a proper HTML sanitizer library
            return Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Remove potentially dangerous characters
            return Regex.Replace(input, @"[<>""'&;]", "");
        }

        public static bool IsValidEnumValue<TEnum>(string value) where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Enum.TryParse<TEnum>(value, true, out _);
        }

        public static bool IsValidEnumValue<TEnum>(int value) where TEnum : struct, Enum
        {
            return Enum.IsDefined(typeof(TEnum), value);
        }

        public static bool IsValidLatitude(decimal latitude)
        {
            return latitude >= -90 && latitude <= 90;
        }

        public static bool IsValidLongitude(decimal longitude)
        {
            return longitude >= -180 && longitude <= 180;
        }

        public static bool IsValidCoordinate(decimal latitude, decimal longitude)
        {
            return IsValidLatitude(latitude) && IsValidLongitude(longitude);
        }

        public static bool IsValidPercentage(int percentage)
        {
            return percentage >= 0 && percentage <= 100;
        }

        public static bool IsValidPercentage(decimal percentage)
        {
            return percentage >= 0 && percentage <= 100;
        }

        public static bool IsValidDuration(TimeSpan duration)
        {
            return duration > TimeSpan.Zero;
        }

        public static bool IsValidFileSize(long fileSize, long maxSizeInBytes)
        {
            return fileSize > 0 && fileSize <= maxSizeInBytes;
        }

        public static bool IsValidMimeType(string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
                return false;

            return Regex.IsMatch(mimeType, @"^[a-z]+/[a-z0-9\-+.]+$", RegexOptions.IgnoreCase);
        }

        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var invalidChars = Path.GetInvalidFileNameChars();
            return !fileName.Any(c => invalidChars.Contains(c));
        }

        public static bool IsValidFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var invalidChars = Path.GetInvalidPathChars();
            return !filePath.Any(c => invalidChars.Contains(c));
        }

        public static ValidationResult ValidateObject(object obj)
        {
            if (obj == null)
                return new ValidationResult("Object cannot be null");

            var context = new ValidationContext(obj);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool isValid = Validator.TryValidateObject(obj, context, results, true);

            return new ValidationResult
            {
                IsValid = isValid,
                Errors = results.Select(r => r.ErrorMessage).Where(e => !string.IsNullOrWhiteSpace(e)).ToList()
            };
        }

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();

            // Add these constructors:
            public ValidationResult() { }

            public ValidationResult(string errorMessage)
            {
                IsValid = false;
                Errors = new List<string> { errorMessage };
            }

            public ValidationResult(bool isValid, List<string> errors = null)
            {
                IsValid = isValid;
                Errors = errors ?? new List<string>();
            }
        }
    }
}