namespace Connect3Dp.Validation.Tests;

public enum TestOutcome { Pass, Fail, Skip }

public record TestResult(TestOutcome Outcome, string Message, string? Detail = null)
{
	public static TestResult Pass(string message = "OK") => new(TestOutcome.Pass, message);
	public static TestResult Fail(string message, string? detail = null) => new(TestOutcome.Fail, message, detail);
	public static TestResult Skip(string message = "Skipped") => new(TestOutcome.Skip, message);
}
