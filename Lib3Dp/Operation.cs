namespace Lib3Dp
{
	/// <summary>
	/// Represents the result of an operation execution
	/// </summary>
	public class OperationResult
	{
		public bool Success { get; init; }
		public required string? Message { get; init; }
		public Exception? Exception { get; init; }

		public static OperationResult Ok(string? message = null) => new() { Success = true, Message = message };

		public static OperationResult Fail(string message, Exception? ex = null) => new() { Success = false, Message = message, Exception = ex };

		public override string ToString()
		{
			if (this.Success)
			{
				return "Success";
			}
			else return $"Failure, {Message}\n{Exception}";
		}

		/// <exception cref="Exception"></exception>
		public void ThrowIfFailed()
		{
			if (!this.Success)
			{
				throw new Exception($"Operation Failed: {Message ?? "No Message Provided"}", this.Exception);
			}
		}
	}

	/// <summary>
	/// Base class for all operations with execute, completion checking, and undo support
	/// </summary>
	public abstract class Operation(TimeSpan? timeout = null)
	{
		private readonly TimeSpan _defaultTimeout = timeout ?? TimeSpan.FromSeconds(30);

		public bool IsCompleted { get; private set; }
		public bool IsExecuting { get; private set; }
		public OperationResult? Result { get; private set; }

		/// <summary>
		/// Executes the operation and waits for completion
		/// </summary>
		public async Task<OperationResult> RunAsync(CancellationToken cancellationToken = default)
		{
			return await RunAsync(_defaultTimeout, cancellationToken);
		}

		/// <summary>
		/// Executes the operation with a specific timeout
		/// </summary>
		public async Task<OperationResult> RunAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
		{
			if (IsExecuting)
				return OperationResult.Fail("Operation is already executing");

			if (IsCompleted)
				return OperationResult.Fail("Operation has already completed");

			IsExecuting = true;
			var startTime = DateTime.UtcNow;

			try
			{
				// Execute the operation
				await ExecuteAsync(cancellationToken);

				// Poll for completion
				while (!cancellationToken.IsCancellationRequested)
				{
					var elapsed = DateTime.UtcNow - startTime;

					if (elapsed > timeout)
					{
						Result = OperationResult.Fail($"Operation timed out after {timeout.TotalSeconds}s");
						await OnTimeoutAsync(cancellationToken);
						return Result;
					}

					var completionStatus = CheckCompletionAsync(cancellationToken);

					if (completionStatus.IsComplete)
					{
						IsCompleted = true;
						Result = completionStatus.Success
							? OperationResult.Ok(completionStatus.Message)
							: OperationResult.Fail(completionStatus.Message);
						return Result;
					}

					await Task.Delay(250, cancellationToken);
				}

				Result = OperationResult.Fail("Operation was cancelled");
				return Result;
			}
			catch (OperationCanceledException)
			{
				Result = OperationResult.Fail("Operation was cancelled");
				return Result;
			}
			catch (Exception ex)
			{
				Result = OperationResult.Fail($"Operation failed: {ex.Message}", ex);
				return Result;
			}
			finally
			{
				IsExecuting = false;
			}
		}

		/// <summary>
		/// Attempts to undo the operation if supported
		/// </summary>
		public async Task<OperationResult> UndoAsync(CancellationToken cancellationToken = default)
		{
			if (!IsCompleted)
				return OperationResult.Fail("Cannot undo: operation has not completed");

			if (!SupportsUndo())
				return OperationResult.Fail("This operation does not support undo");

			try
			{
				await UndoExecuteAsync(cancellationToken);
				IsCompleted = false;
				return OperationResult.Ok("Operation undone successfully");
			}
			catch (Exception ex)
			{
				return OperationResult.Fail($"Undo failed: {ex.Message}", ex);
			}
		}

		// Abstract methods to be implemented by derived classes

		/// <summary>
		/// Execute the operation (send the command, start the action)
		/// </summary>
		protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Check if the operation has completed and return status
		/// </summary>
		protected abstract CompletionStatus CheckCompletionAsync(CancellationToken cancellationToken);

		// Virtual methods with default implementations

		/// <summary>
		/// Override to provide undo functionality
		/// </summary>
		protected virtual Task UndoExecuteAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Override to return true if operation supports undo
		/// </summary>
		protected virtual bool SupportsUndo() => false;

		/// <summary>
		/// Called when operation times out. Override for custom timeout handling
		/// </summary>
		protected virtual Task OnTimeoutAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// Represents the completion status of an operation
	/// </summary>
	public class CompletionStatus
	{
		public bool IsComplete { get; init; }
		public bool Success { get; init; }
		public string? Message { get; init; }

		public static CompletionStatus NotComplete() => new() { IsComplete = false };

		public static CompletionStatus Complete(bool success = true, string? message = null) => new() { IsComplete = true, Success = success, Message = message };

		public static CompletionStatus Condition(bool isCompleteAndSuccess, string? message = null) => new() { IsComplete = isCompleteAndSuccess, Success = isCompleteAndSuccess, Message = message };
	}

	/// <summary>
	/// Simple operation that uses delegates instead of requiring a full class implementation
	/// </summary>
	public class RunnableOperation : Operation
	{
		private readonly Func<CancellationToken, Task> _executeAction;
		private readonly Func<CancellationToken, CompletionStatus> _checkCompletion;
		private readonly Func<CancellationToken, Task>? _undoAction;

		public RunnableOperation(
			Func<CancellationToken, Task> execute,
			Func<CancellationToken, CompletionStatus> isSuccess,
			Func<CancellationToken, Task>? undo = null,
			TimeSpan? timeout = null)
			: base(timeout)
		{
			_executeAction = execute;
			_checkCompletion = isSuccess;
			_undoAction = undo;
		}

		protected override Task ExecuteAsync(CancellationToken cancellationToken)
		{
			return _executeAction(cancellationToken);
		}

		protected override CompletionStatus CheckCompletionAsync(CancellationToken cancellationToken)
		{
			return _checkCompletion(cancellationToken);
		}

		protected override bool SupportsUndo() => _undoAction != null;

		protected override Task UndoExecuteAsync(CancellationToken cancellationToken)
		{
			return _undoAction?.Invoke(cancellationToken) ?? Task.CompletedTask;
		}
	}
}
