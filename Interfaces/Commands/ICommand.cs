namespace API_DigiBook.Interfaces.Commands
{
    /// <summary>
    /// Interface for Command Pattern
    /// </summary>
    /// <typeparam name="TResult">Type of result returned by command</typeparam>
    public interface ICommand<TResult>
    {
        Task<TResult> ExecuteAsync();
    }

    /// <summary>
    /// Base result class for commands
    /// </summary>
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? Error { get; set; }

        public static CommandResult SuccessResult(string message, object? data = null)
        {
            return new CommandResult
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static CommandResult FailureResult(string message, string? error = null)
        {
            return new CommandResult
            {
                Success = false,
                Message = message,
                Error = error
            };
        }
    }
}
