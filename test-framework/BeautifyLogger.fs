namespace BeautifyLogger

open System
open System.IO
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging


[<FriendlyName(Constants.friendlyName)>]
[<ExtensionUri(Constants.extensionUri)>]
type BeautifyLogger () =
    interface ITestLogger with
        member _.Initialize (events: TestLoggerEvents, testRunDirectory: string): unit =
            let logFilePath: string = Path.Combine (testRunDirectory, "testResult.txt")
            use writer: StreamWriter = new StreamWriter (logFilePath, true)

            writer.WriteLine "BeautifyLogger"
            writer.WriteLine "--------------------"

            events.TestResult.Add (fun (testResultEventArgs: TestResultEventArgs) ->
                let result: TestResult = testResultEventArgs.Result
                writer.WriteLine $"Test: %s{result.DisplayName}"
                writer.WriteLine $"Outcome: %s{result.Outcome.ToString ()}"
                writer.WriteLine $"Duration: %f{result.Duration.TotalMilliseconds} ms"
                if not (String.IsNullOrEmpty result.ErrorMessage) then
                    writer.WriteLine $"Error Message: %s{result.ErrorMessage}"
                if not (String.IsNullOrEmpty result.ErrorStackTrace) then
                    writer.WriteLine $"Error Stacktrace: %s{result.ErrorStackTrace}"
                writer.Flush ()
            )

            events.TestRunComplete.Add (fun (testRunCompleteEventArgs: TestRunCompleteEventArgs) ->
                writer.WriteLine "Test run completed."
                let testRunStatistics: ITestRunStatistics = testRunCompleteEventArgs.TestRunStatistics
                writer.WriteLine $"Total tests: %d{testRunStatistics.ExecutedTests}"

                let mutable passedCount: int64 = 0L
                let mutable failedCount: int64 = 0L
                let mutable skippedCount: int64 = 0L

                if testRunStatistics.Stats.TryGetValue(TestOutcome.Passed, &passedCount) then
                    writer.WriteLine $"Passed: %d{passedCount}"
                else
                    writer.WriteLine $"No passed test count found."

                if testRunStatistics.Stats.TryGetValue(TestOutcome.Failed, &failedCount) then
                    writer.WriteLine $"Failed: %d{failedCount}"
                else
                    writer.WriteLine $"No failed test count found."

                if testRunStatistics.Stats.TryGetValue(TestOutcome.Skipped, &skippedCount) then
                    writer.WriteLine $"Skipped: %d{skippedCount}"
                else
                    writer.WriteLine $"No skipped test count found."

                writer.WriteLine $"Total duration: %f{testRunCompleteEventArgs.ElapsedTimeInRunningTests.TotalMilliseconds} ms"
                writer.Flush ()
            )
    end

