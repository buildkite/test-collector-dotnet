namespace Buildkite.TestAnalytics.Common

/// Deals with data related to individual tests.
module TestData =
    /// The result of the test.
    type TestResult =
        | Unknown
        | Passed
        | Skipped
        | Failed of failureReason: string option

    /// Data about an indivdual test.
    type public Test =
        { Id: string
          Scope: string option
          Name: string option
          Location: string option
          FileName: string option
          Result: TestResult
          StartAt: Timing.Instant
          EndAt: Timing.Instant option
          Duration: Timing.Duration option
          Children: Tracing.Span list }

    /// <summary>Initialise a new Test</summary>
    /// <param name="id">A unique identifier for this test result. If a test
    /// execution with this UUID already exists in the Test Analytics database,
    /// this result is ignored.</param>
    /// <param name="scope">The parent context under which the test is located -
    /// in our case usually the test class</param>
    /// <param name="name">The human-readable name of the test</param>
    /// <param name="location">The file and line number where the test
    /// originates, separated by a colon (:)</param>
    /// <param name="fileName">The file where the test originates</param>
    let public Init
        (
            id: string,
            scope: string option,
            name: string option,
            location: string option,
            fileName: string option
        ) : Test =
        { Id = id
          Scope = scope
          Name = name
          Location = location
          FileName = fileName
          Result = Unknown
          StartAt = Timing.now ()
          EndAt = None
          Duration = None
          Children = [] }

    let maybeFinish (test: Test) : Test =
        match test.EndAt with
        | Some (_) -> test
        | None -> { test with EndAt = Some(Timing.now ()) }

    /// Manually add a tracing span to the test
    let public AddSpan (test: Test, span: Tracing.Span) : Test =
        { test with Children = span :: test.Children }

    /// Mark the test as having passed.
    let public Passed (test: Test) : Test =
        maybeFinish { test with Result = Passed }

    /// Mark the test as having failed - with optional failure message.
    let public Failed (test: Test, reason: string option) : Test =
        maybeFinish { test with Result = Failed reason }

    let public Skipped (test: Test) : Test =
        maybeFinish { test with Result = Skipped }

    /// If the test runner provides an accurate test duration you can add it
    /// here.
    let public WithDuration (test: Test, duration: Timing.Duration) : Test =
        maybeFinish { test with Duration = Some(duration) }

    let private spansAsJson (spans: Tracing.Span list, epoch: Timing.Instant) : Map<string, obj> list =
        spans
        |> List.map (fun span -> Tracing.AsJson(span, epoch))

    let private historyAsJson (test: Test, epoch: Timing.Instant) : Map<string, obj> =
        let endAt =
            test.EndAt
            |> Option.map ((fun endAt -> Timing.elapsedSeconds (endAt, epoch)))

        let duration =
            match (test.Duration, test.EndAt) with
            | (Some (duration), _) -> Some(Timing.asSeconds (duration))
            | (None, Some (endAt)) -> Some(Timing.elapsedSeconds (endAt, test.StartAt))
            | (None, None) -> None

        let startAt = Timing.elapsedSeconds (test.StartAt, epoch)

        let children = spansAsJson (test.Children, epoch)

        let attrs =
            [ ("section", "top" :> obj)
              ("start_at", startAt :> obj)
              ("children", children :> obj) ]
          |> Map

        let attrs = if Option.isSome endAt then
                      let value = endAt.Value :> obj
                      attrs |> Map.add "end_at" value
                    else
                      attrs

        let attrs = if Option.isSome duration then
                      let value = duration.Value :> obj
                      attrs |> Map.add "duration" value
                    else
                      attrs

        attrs


    /// <summary>Convert the test into a format ready for serialisation to
    /// JSON</summary>
    /// <param name="test">The test to serialise</param>
    /// <param name="epoch">The time relative to which to calculate time
    /// offsets</param>
    let public AsJson (test: Test, epoch: Timing.Instant) : Map<string, obj> =
        let history = historyAsJson (test, epoch)

        let attrs =
            [ ("id", test.Id :> obj)
              ("history", history :> obj) ]

        let attrs = if Option.isSome(test.Scope) then
                      ("scope", test.Scope.Value :> obj) :: attrs
                    else
                      attrs

        let attrs = if Option.isSome(test.Name) then
                      ("name", test.Name.Value :> obj) :: attrs
                    else
                      attrs

        let attrs = if Option.isSome(test.Location) then
                      ("location", test.Location.Value :> obj) :: attrs
                    else
                      attrs

        let attrs = if Option.isSome(test.FileName) then
                      ("file_name", test.FileName.Value :> obj) :: attrs
                    else
                      attrs

        let attrs =
            match test.Result with
            | Passed -> ("result", "passed" :> obj) :: attrs
            | Failed (None) -> ("result", "failed" :> obj) :: attrs
            | Failed (Some (failureReason)) ->
                attrs
                |> List.append (
                    [ ("result", "failed" :> obj)
                      ("failure_reason", failureReason :> obj) ]
                )
            | Skipped -> ("result", "skipped" :> obj) :: attrs
            | Unknown -> ("result", "unknown" :> obj) :: attrs

        Map attrs
