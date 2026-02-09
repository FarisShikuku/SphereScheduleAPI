namespace SphereScheduleAPI.Common.Models
{
    public class ApiErrorResponse
    {
        public bool Success { get; set; } = false;  // Add this
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
        public string? StackTrace { get; set; }  // Add this
        public string? Details { get; set; }     // Add this

        // Default constructor
        public ApiErrorResponse()
        {
            Timestamp = DateTime.UtcNow;
            Success = false; // Default to false for errors
        }

        // Constructor with message
        public ApiErrorResponse(string message)
        {
            Success = false;
            Message = message;
            Timestamp = DateTime.UtcNow;
        }

        // Constructor with message and error code
        public ApiErrorResponse(string message, string errorCode)
        {
            Success = false;
            Message = message;
            ErrorCode = errorCode;
            Timestamp = DateTime.UtcNow;
        }

        // Constructor with message, error code, and validation errors
        public ApiErrorResponse(string message, string errorCode, Dictionary<string, string[]> errors)
        {
            Success = false;
            Message = message;
            ErrorCode = errorCode;
            Errors = errors;
            Timestamp = DateTime.UtcNow;
        }
    }
}