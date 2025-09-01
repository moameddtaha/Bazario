namespace Bazario.Auth.DTO
{
    /// <summary>
    /// Result of user operations with success/failure status and data
    /// </summary>
    public class UserResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? User { get; set; }
        public string? ErrorCode { get; set; }

        public static UserResult Success(object user, string message = "User retrieved successfully")
        {
            return new UserResult
            {
                IsSuccess = true,
                Message = message,
                User = user
            };
        }

        public static UserResult NotFound(string message = "User not found")
        {
            return new UserResult
            {
                IsSuccess = false,
                Message = message,
                ErrorCode = "USER_NOT_FOUND"
            };
        }

        public static UserResult Error(string message, string errorCode = "USER_ERROR")
        {
            return new UserResult
            {
                IsSuccess = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }
}
