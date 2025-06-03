namespace Buildkite.TestAnalytics.Xunit.reporters

/// The Xunit plugin for Buildkite Test Analytics
module Sink =
  open Xunit
  open Xunit.Abstractions
  open Buildkite.TestAnalytics.Common
  open System
  open System.Security.Cryptography

  let testIdToUuid (input: string) : string =
    use hasher = MD5.Create()
    let inputBytes = Text.Encoding.UTF8.GetBytes(input)
    let hash = hasher.ComputeHash(inputBytes)

    Guid(hash).ToString("D")

  /// Implements the required <c>IMessageSink</c> and <c>IRunnerReporter</c>
  /// interfaces.
  type Collector(payload: Payload.Payload option) =
    let mutable isEnvironmentallyEnabled = Option.isSome(payload)
    let mutable payload = payload.Value
    let mutable inFlight: Map<string, TestData.Test> = Map []

    new() =
      Collector(Payload.Init(None))

    interface IMessageSink with
      member this.OnMessage(message: IMessageSinkMessage) : bool =
        match message with
          | :? TestAssemblyExecutionStarting  ->
            payload <- Payload.Started(payload)
          | :? Sdk.TestStarting as test ->
            let testCollectoinUuid = test.TestCollection.UniqueID.ToString("D")
            let testCaseUuid = testIdToUuid(test.TestCase.UniqueID)
            let (location, fileName) = match test.TestCase.SourceInformation with
                                        | null -> (None, None)
                                        | source -> (Some(source.FileName + ":" + source.LineNumber.ToString()), Some(source.FileName))
            let testData = TestData.Init(testCaseUuid, Some(test.TestCollection.DisplayName), Some(test.Test.DisplayName), location, fileName)
            inFlight <- Map.add test.TestCase.UniqueID testData inFlight
            ()
          | :? Sdk.TestPassed as test ->
            let maybeTestData = Map.tryFind test.TestCase.UniqueID inFlight
            if Option.isSome maybeTestData then
                let testData = maybeTestData.Value
                inFlight <- Map.remove test.TestCase.UniqueID inFlight
                let duration = float test.ExecutionTime
                let testData = TestData.Passed(testData)
                let testData = TestData.WithDuration(testData, Timing.fromSeconds(duration))
                payload <- Payload.AddTestResult(payload, testData)
            ()
          | :? Sdk.TestFailed as test ->
            let maybeTestData = Map.tryFind test.TestCase.UniqueID inFlight
            if Option.isSome maybeTestData then
                let testData = maybeTestData.Value
                inFlight <- Map.remove test.TestCase.UniqueID inFlight
                let duration = float test.ExecutionTime
                let failureReason = if test.Messages.Length > 0 then
                                      Some(String.concat "\n"  test.Messages)
                                    else
                                      None
                let testData = TestData.Failed(testData, failureReason)
                let testData = TestData.WithDuration(testData, Timing.fromSeconds(duration))
                payload <- Payload.AddTestResult(payload, testData)
            ()
          | :? Sdk.TestSkipped as test ->
            let maybeTestData = Map.tryFind test.TestCase.UniqueID inFlight
            if Option.isSome maybeTestData then
                let testData = maybeTestData.Value
                inFlight <- Map.remove test.TestCase.UniqueID inFlight
                let testData = TestData.Skipped(testData)
                payload <- Payload.AddTestResult(payload, testData)
            ()
          | :? TestAssemblyExecutionFinished ->
            let _ = Api.submit(payload, None)
            ()
          | _ -> ()

        true

    interface IRunnerReporter with
      member this.Description = "Buildkite Test Analytics Collector for Xunit"
      member this.IsEnvironmentallyEnabled = isEnvironmentallyEnabled
      member this.RunnerSwitch = "wat"

      member this.CreateMessageHandler(logger: IRunnerLogger) : IMessageSink =
        this
