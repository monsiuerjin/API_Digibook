using API_DigiBook.Interfaces.Commands;

namespace API_DigiBook.Commands
{
    /// <summary>
    /// Command Invoker - Executes commands and provides additional functionality
    /// like logging, undo/redo (if needed)
    /// </summary>
    public class CommandInvoker
    {
        private readonly ILogger<CommandInvoker> _logger;
        private readonly List<ICommand<CommandResult>> _commandHistory;

        public CommandInvoker(ILogger<CommandInvoker> logger)
        {
            _logger = logger;
            _commandHistory = new List<ICommand<CommandResult>>();
        }

        /// <summary>
        /// Execute a command
        /// </summary>
        public async Task<CommandResult> ExecuteAsync<TResult>(ICommand<TResult> command)
        {
            try
            {
                _logger.LogInformation("Executing command: {CommandType}", command.GetType().Name);

                var result = await command.ExecuteAsync();

                // Store in history if it's a CommandResult type
                if (command is ICommand<CommandResult> cmdResult)
                {
                    _commandHistory.Add(cmdResult);
                }

                if (result is CommandResult cmdRes)
                {
                    if (cmdRes.Success)
                    {
                        _logger.LogInformation("Command executed successfully: {CommandType}", 
                            command.GetType().Name);
                    }
                    else
                    {
                        _logger.LogWarning("Command failed: {CommandType}. Message: {Message}", 
                            command.GetType().Name, cmdRes.Message);
                    }

                    return cmdRes;
                }

                return CommandResult.SuccessResult("Command executed", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command: {CommandType}", command.GetType().Name);
                return CommandResult.FailureResult("Command execution failed", ex.Message);
            }
        }

        /// <summary>
        /// Get command history count
        /// </summary>
        public int GetHistoryCount()
        {
            return _commandHistory.Count;
        }

        /// <summary>
        /// Clear command history
        /// </summary>
        public void ClearHistory()
        {
            _commandHistory.Clear();
        }
    }
}
