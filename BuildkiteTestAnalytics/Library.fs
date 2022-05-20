namespace BuildkiteTestAnalytics

open System

type public TestResult =
    | Passed
    | Failed of failureReason: string option

type public TestHistory =
    { Section: string
      StartAt: int64
      EndAt: int64 option
      Duration: int64 option
      Children: TestHistory list }

type public TestData =
    { Id: string
      Scope: string
      Name: string
      Identifier: string
      Result: TestResult
      History: TestHistory }

type public RuntimeEnvironment =
    { Ci: string
      Key: string
      Number: string option
      JobId: string option
      Branch: string option
      CommitSha: string option
      Message: string option
      Url: string option }

type public Payload =
    { RuntimeEnvironment: RuntimeEnvironment
      Data: TestData list
      StartedAt: int64
      FinishedAt: int64 option }

module public RuntimeEnvironment =

    let private GetEnvVar (key: string) : string = Environment.GetEnvironmentVariable key

    let private OptionalEnvVar (key: string) : string option =
        let var = GetEnvVar key

        if String.length var > 0 then
            Some(var)
        else
            None


    let public detect () : RuntimeEnvironment option =
        if Option.isSome (OptionalEnvVar "BUILDKITE_BUILD_ID") then
            Some(
                { Ci = "buildkite"
                  Key = GetEnvVar "BUILDKITE_BUILD_ID"
                  Number = OptionalEnvVar "BUILDKITE_BUILD_NUMBER"
                  JobId = OptionalEnvVar "BUILDKITE_JOB_ID"
                  Branch = OptionalEnvVar "BUILDKITE_BRANCH"
                  CommitSha = OptionalEnvVar "BUILDKITE_COMMIT"
                  Message = OptionalEnvVar "BUILDKITE_MESSAGE"
                  Url = OptionalEnvVar "BUILDKITE_BUILD_URL" }
            )
        elif Option.isSome (OptionalEnvVar "GITHUB_ACTION") then
            Some(
                { Ci = "github_actions"
                  Key =
                    sprintf
                        "%s-%s-%s"
                        (GetEnvVar "GITHUB_ACTION")
                        (GetEnvVar "GITHUB_RUN_NUMBER")
                        (GetEnvVar "GITHUB_RUN_ATTEMPT")
                  Number = OptionalEnvVar "GITHUB_RUN_NUMBER"
                  JobId = None
                  Branch = OptionalEnvVar "GITHUB_REF"
                  CommitSha = OptionalEnvVar "GITHUB_SHA"
                  Message = None
                  Url =
                    Some(
                        sprintf
                            "https://github.com/%s/actions/run/%s"
                            (GetEnvVar "GITHUB_REPOSITORY")
                            (GetEnvVar "GITHUB_RUN_ID")
                    ) }
            )
        elif Option.isSome (OptionalEnvVar "CIRCLE_BUILD_NUM") then
            Some(
                { Ci = "circleci"
                  Key = sprintf "%s-%s" (GetEnvVar "CIRCLE_WORKFLOW_ID") (GetEnvVar "CIRCLE_BUILD_NUM")
                  Number = OptionalEnvVar "CIRCLE_BUILD_NUM"
                  JobId = None
                  Branch = OptionalEnvVar "CIRCLE_BRANCH"
                  CommitSha = OptionalEnvVar "CIRCLE_SHA1"
                  Message = None
                  Url = OptionalEnvVar "CIRCLE_BUILD_URL" }
            )
        elif Option.isSome (OptionalEnvVar "CI") then
            Some(
                { Ci = "generic"
                  Key = string Guid.NewGuid
                  Number = None
                  JobId = None
                  Branch = None
                  CommitSha = None
                  Message = None
                  Url = None }
            )
        else
            None

module public Payload =

    let private Now () : int64 = Diagnostics.Stopwatch.GetTimestamp()

    let public Init (runtimeEnvironment: RuntimeEnvironment) : Payload =
        { RuntimeEnvironment = runtimeEnvironment
          Data = []
          StartedAt = Now()
          FinishedAt = None }

    let public AddTestResult (payload: Payload, testData: TestData) : Payload =
        { payload with Data = testData :: payload.Data }
