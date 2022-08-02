namespace Buildkite.TestAnalytics.Common

open System

/// The main data storage for test analytics data.
module Payload =
    open Newtonsoft.Json

    /// Stores information about the detected runtime environment.
    type RuntimeEnvironment =
        { Ci: string
          Key: string
          Number: string option
          JobId: string option
          Branch: string option
          CommitSha: string option
          Message: string option
          Url: string option }

    /// Stores information about the test suite and it's results.
    type Payload =
        { RuntimeEnvironment: RuntimeEnvironment
          Data: TestData.Test list
          StartedAt: Timing.Instant }

    let private Detect (getEnvVar: string -> string) : RuntimeEnvironment option =
        let optionalEnvVar (key: string) : string option =
            let var = getEnvVar key

            if
                String.IsNullOrEmpty(var)
                || String.IsNullOrWhiteSpace(var)
            then
                None
            else
                Some(var)

        if Option.isSome (optionalEnvVar "BUILDKITE_BUILD_ID") then
            Some(
                { Ci = "buildkite"
                  Key = getEnvVar "BUILDKITE_BUILD_ID"
                  Number = optionalEnvVar "BUILDKITE_BUILD_NUMBER"
                  JobId = optionalEnvVar "BUILDKITE_JOB_ID"
                  Branch = optionalEnvVar "BUILDKITE_BRANCH"
                  CommitSha = optionalEnvVar "BUILDKITE_COMMIT"
                  Message = optionalEnvVar "BUILDKITE_MESSAGE"
                  Url = optionalEnvVar "BUILDKITE_BUILD_URL" }
            )
        elif Option.isSome (optionalEnvVar "GITHUB_ACTION") then
            Some(
                { Ci = "github_actions"
                  Key =
                    sprintf
                        "%s-%s-%s"
                        (getEnvVar "GITHUB_ACTION")
                        (getEnvVar "GITHUB_RUN_NUMBER")
                        (getEnvVar "GITHUB_RUN_ATTEMPT")
                  Number = optionalEnvVar "GITHUB_RUN_NUMBER"
                  JobId = None
                  Branch = optionalEnvVar "GITHUB_REF"
                  CommitSha = optionalEnvVar "GITHUB_SHA"
                  Message = None
                  Url =
                    Some(
                        sprintf
                            "https://github.com/%s/actions/run/%s"
                            (getEnvVar "GITHUB_REPOSITORY")
                            (getEnvVar "GITHUB_RUN_ID")
                    ) }
            )
        elif Option.isSome (optionalEnvVar "CIRCLE_BUILD_NUM") then
            Some(
                { Ci = "circleci"
                  Key = sprintf "%s-%s" (getEnvVar "CIRCLE_WORKFLOW_ID") (getEnvVar "CIRCLE_BUILD_NUM")
                  Number = optionalEnvVar "CIRCLE_BUILD_NUM"
                  JobId = None
                  Branch = optionalEnvVar "CIRCLE_BRANCH"
                  CommitSha = optionalEnvVar "CIRCLE_SHA1"
                  Message = None
                  Url = optionalEnvVar "CIRCLE_BUILD_URL" }
            )
        elif Option.isSome (optionalEnvVar "CI") then
            Some(
                { Ci = "generic"
                  Key = Guid.NewGuid().ToString()
                  Number = None
                  JobId = None
                  Branch = None
                  CommitSha = None
                  Message = None
                  Url = None }
            )
        else
            None

    /// <summary>(maybe) Initialise a new empty Payload. Attempts to to detect
    /// the CI environment which the code is running in - if said detection
    /// fails then returns None.</summary>
    /// <param name="getEnvVar">Allows you to provide a function which overrides
    /// the default environment variable lookup.  Used by tests.</param>
    /// <returns>Some Payload when the environment is detected, otherwise
    /// None.</returns>
    let public Init (getEnvVar: (string -> string) option) : Payload option =
        let getEnvVar =
            match getEnvVar with
            | Some (getEnvVar) -> getEnvVar
            | None -> Environment.GetEnvironmentVariable

        Detect(getEnvVar)
        |> Option.map (fun env ->
            { RuntimeEnvironment = env
              Data = []
              StartedAt = Timing.now () })

    /// <summary>Update the payload to mark it as having started.</summary>
    /// <remarks>All times in JSON output are calculated relative to
    /// this.</remarks>
    /// <returns>An updated payload</returns>
    let public Started (payload: Payload) : Payload =
      { payload with StartedAt = Timing.now() }

    /// <summary>Add a new test result to the payload</summary>
    /// <returns>An updated payload</returns>
    let public AddTestResult (payload: Payload, testData: TestData.Test) : Payload =
        { payload with Data = testData :: payload.Data }

    let rec private RecurseIntoBatches (payload: Payload, batchSize: int, batches: Payload list) =
        match payload.Data with
        | data when List.length (data) < batchSize -> payload :: batches
        | data ->
            let (thisBatch, remainder) = data |> List.splitAt (batchSize)
            let newPayload = { payload with Data = thisBatch }
            let batches = newPayload :: batches
            let payload = { payload with Data = remainder }
            RecurseIntoBatches(payload, batchSize, batches)

    /// <summary>Splits a payload into a list of smaller payloads based on the batch size.</summary>
    /// <remarks>Called internally by the Api module - so you probably don't need to worry about it.</remarks>
    let public IntoBatches (payload: Payload, batchSize: int) : Payload list =
        List.rev(RecurseIntoBatches(payload, batchSize, []))

    let private maybeAppend name option attrs =
        match option with
        | Some (value) -> attrs |> Map.add name (value :> obj)
        | None -> attrs

    let private runtimeEnvironmentAsJson (env: RuntimeEnvironment) : Map<string, obj> =
        Map [
          ("CI", env.Ci :> obj)
          ("key", env.Key :> obj)
        ]
        |> maybeAppend "number" env.Number
        |> maybeAppend "branch" env.Branch
        |> maybeAppend "commit_sha" env.CommitSha
        |> maybeAppend "url" env.Url

    let dataAsJson (payload: Payload) : Map<string, obj> list =
        payload.Data
        |> List.map (fun testData -> TestData.AsJson(testData, payload.StartedAt))

    /// Convert the payload into a form ready for direct serialisation into
    /// JSON.
    let public AsJson (payload: Payload) : Map<string, obj> =
        let runEnv = runtimeEnvironmentAsJson (payload.RuntimeEnvironment)
        let data = dataAsJson (payload)

        [ ("format", "json" :> obj)
          ("run_env", runEnv :> obj)
          ("data", data :> obj) ]
        |> Map


    /// Convert the payload into a JSON-encoded string.
    let public ToJson (payload: Payload) : string =
      let toSerialize = AsJson(payload)
      JsonConvert.SerializeObject(toSerialize)
