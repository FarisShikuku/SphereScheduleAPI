namespace SphereScheduleAPI.Common.Constants
{
    public static class AppConstants
    {
        // Application
        public const string AppName = "Sphere Schedule";
        public const string AppVersion = "1.0.0";
        public const string DefaultTimezone = "UTC";
        public const string DefaultLanguage = "en";

        // Pagination
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int DefaultPageNumber = 1;

        // Security
        public const int MinimumPasswordLength = 8;
        public const int MaximumPasswordLength = 128;
        public const int TokenExpirationMinutes = 60;
        public const int RefreshTokenExpirationDays = 7;
        public const int MaxLoginAttempts = 5;
        public const int LockoutDurationMinutes = 15;

        // Validation
        public const int MaxEmailLength = 255;
        public const int MaxUsernameLength = 100;
        public const int MaxDisplayNameLength = 100;
        public const int MaxFirstNameLength = 50;
        public const int MaxLastNameLength = 50;
        public const int MaxPhoneNumberLength = 20;
        public const int MaxTaskTitleLength = 255;
        public const int MaxTaskDescriptionLength = 5000;
        public const int MaxAppointmentTitleLength = 255;
        public const int MaxAppointmentDescriptionLength = 5000;
        public const int MaxCategoryNameLength = 100;
        public const int MaxLocationLength = 500;

        // Date/Time
        public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";
        public const string DateFormat = "yyyy-MM-dd";
        public const string TimeFormat = "HH:mm";

        // Categories
        public static class Categories
        {
            public const string Personal = "personal";
            public const string Work = "job";
            public const string Health = "health";
            public const string Education = "education";
            public const string Shopping = "shopping";
            public const string Finance = "finance";
            public const string Entertainment = "entertainment";
            public const string Other = "unspecified";

            public static readonly string[] DefaultCategories =
            {
                Personal,
                Work,
                Health,
                Education,
                Shopping,
                Finance,
                Entertainment,
                Other
            };
        }

        // Task Statuses
        public static class TaskStatus
        {
            public const string Pending = "pending";
            public const string InProgress = "in_progress";
            public const string Completed = "completed";
            public const string Cancelled = "cancelled";
            public const string Deferred = "deferred";

            public static readonly string[] AllStatuses =
            {
                Pending,
                InProgress,
                Completed,
                Cancelled,
                Deferred
            };
        }

        // Task Priorities
        public static class TaskPriority
        {
            public const string Low = "low";
            public const string Medium = "medium";
            public const string High = "high";
            public const string Critical = "critical";

            public static readonly string[] AllPriorities =
            {
                Low,
                Medium,
                High,
                Critical
            };
        }

        // Appointment Statuses
        public static class AppointmentStatus
        {
            public const string Scheduled = "scheduled";
            public const string Confirmed = "confirmed";
            public const string Cancelled = "cancelled";
            public const string Completed = "completed";
            public const string Rescheduled = "rescheduled";

            public static readonly string[] AllStatuses =
            {
                Scheduled,
                Confirmed,
                Cancelled,
                Completed,
                Rescheduled
            };
        }

        // User Account Types
        public static class AccountType
        {
            public const string Free = "free";
            public const string Premium = "premium";
            public const string Enterprise = "enterprise";
            public const string Admin = "admin";

            public static readonly string[] AllTypes =
            {
                Free,
                Premium,
                Enterprise,
                Admin
            };
        }

        // Notification Types
        public static class NotificationType
        {
            public const string TaskReminder = "task_reminder";
            public const string AppointmentReminder = "appointment_reminder";
            public const string TaskOverdue = "task_overdue";
            public const string AppointmentUpcoming = "appointment_upcoming";
            public const string SystemAlert = "system_alert";
            public const string UserActivity = "user_activity";
        }

        // Cache Keys
        public static class CacheKeys
        {
            public const string UserTasks = "user_tasks_{0}";
            public const string UserAppointments = "user_appointments_{0}";
            public const string UserDashboard = "user_dashboard_{0}";
            public const string SystemSettings = "system_settings";
            public const string UserPreferences = "user_preferences_{0}";
        }

        // Error Messages
        public static class ErrorMessages
        {
            public const string InvalidCredentials = "Invalid email or password";
            public const string UserNotFound = "User not found";
            public const string UserInactive = "User account is inactive";
            public const string UserLocked = "User account is locked";
            public const string EmailAlreadyExists = "Email already exists";
            public const string UsernameAlreadyExists = "Username already exists";
            public const string InvalidToken = "Invalid or expired token";
            public const string UnauthorizedAccess = "Unauthorized access";
            public const string ResourceNotFound = "Resource not found";
            public const string ValidationFailed = "Validation failed";
            public const string InternalServerError = "An internal server error occurred";
            public const string ServiceUnavailable = "Service temporarily unavailable";
        }

        // Success Messages
        public static class SuccessMessages
        {
            public const string LoginSuccessful = "Login successful";
            public const string RegistrationSuccessful = "Registration successful";
            public const string LogoutSuccessful = "Logout successful";
            public const string PasswordChanged = "Password changed successfully";
            public const string ProfileUpdated = "Profile updated successfully";
            public const string TaskCreated = "Task created successfully";
            public const string TaskUpdated = "Task updated successfully";
            public const string TaskDeleted = "Task deleted successfully";
            public const string AppointmentCreated = "Appointment created successfully";
            public const string AppointmentUpdated = "Appointment updated successfully";
            public const string AppointmentDeleted = "Appointment deleted successfully";
        }

        // HTTP Headers
        public static class HttpHeaders
        {
            public const string CorrelationId = "X-Correlation-ID";
            public const string ApiVersion = "X-API-Version";
            public const string RequestId = "X-Request-ID";
            public const string UserAgent = "User-Agent";
            public const string Authorization = "Authorization";
            public const string ContentType = "Content-Type";
            public const string Accept = "Accept";
            public const string RateLimitLimit = "X-RateLimit-Limit";
            public const string RateLimitRemaining = "X-RateLimit-Remaining";
            public const string RateLimitReset = "X-RateLimit-Reset";
        }

        // Environment Names
        public static class Environments
        {
            public const string Development = "Development";
            public const string Staging = "Staging";
            public const string Production = "Production";
            public const string Testing = "Testing";
        }
    }
}