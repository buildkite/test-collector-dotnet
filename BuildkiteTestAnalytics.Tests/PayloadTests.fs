module PayloadTests

open System
open System.Collections.Generic
open Xunit
open BuildkiteTestAnalytics

let getEnvVarFactory (env: Map<string, string>) : (string -> string) =
    (fun key ->
        let maybe = env |> Map.tryFind key
        Option.defaultValue "" maybe)

let rand = Random()

[<Fact>]
let ``when it cannot detect the environment it returns none`` () =
    let getEnvVar = getEnvVarFactory (Map [])
    let payload = Payload.Init(Some getEnvVar)
    Assert.Same(payload, None)

[<Fact>]
let ``when it detects a Buildkite CI environment it returns an empty payload`` () =
    let buildId = Guid.NewGuid.ToString()

    let env =
        Map [ ("BUILDKITE_BUILD_ID", buildId)
              ("BUILDKITE_BUILD_URL", sprintf "https://example.test/buildkite/%s" buildId)
              ("BUILDKITE_BRANCH", "feat/add-mr-fusion-to-delorean")
              ("BUILDKITE_COMMIT", buildId)
              ("BUILDKITE_BUILD_NUMBER", rand.Next(999).ToString())
              ("BUILDKITE_JOB_ID", rand.Next(999).ToString())
              ("BUILDKITE_MESSAGE",
               "Silence, Earthling! My Name Is Darth Vader. I Am An Extraterrestrial From The Planet Vulcan!") ]

    let payload = Payload.Init(Some(getEnvVarFactory env))
    Assert.True(Option.isSome payload)

    let payload = payload.Value
    let now = Timing.now ()
    Assert.InRange(payload.StartedAt, now - 20, now + 20)
    Assert.Empty(payload.Data)

    let runEnv = payload.RuntimeEnvironment
    Assert.Equal(runEnv.Ci, "buildkite")
    Assert.Equal(runEnv.Key, env.["BUILDKITE_BUILD_ID"])
    Assert.Equal(runEnv.Number, Some(env.["BUILDKITE_BUILD_NUMBER"]))
    Assert.Equal(runEnv.JobId, Some(env.["BUILDKITE_JOB_ID"]))
    Assert.Equal(runEnv.Branch, Some(env.["BUILDKITE_BRANCH"]))
    Assert.Equal(runEnv.CommitSha, Some(env.["BUILDKITE_COMMIT"]))
    Assert.Equal(runEnv.Message, Some(env.["BUILDKITE_MESSAGE"]))
    Assert.Equal(runEnv.Url, Some(env.["BUILDKITE_BUILD_URL"]))


[<Fact>]
let ``when it detects a Github Actions CI environment it returns an empty payload`` () =
    let env =
        Map [ ("GITHUB_ACTION", "__doc-brown_grandfather-paradox_flux-capacitor")
              ("GITHUB_RUN_NUMBER", rand.Next(999).ToString())
              ("GITHUB_RUN_ATTEMPT", rand.Next(999).ToString())
              ("GITHUB_REPOSITORY", "doc-brown/flux-capacitor")
              ("GITHUB_REF", "feat/add-time-circuits")
              ("GITHUB_RUN_ID", Guid.NewGuid.ToString())
              ("GITHUB_SHA", Guid.NewGuid.ToString()) ]

    let payload = Payload.Init(Some(getEnvVarFactory env))
    Assert.True(Option.isSome payload)

    let payload = payload.Value
    let now = Timing.now ()
    Assert.InRange(payload.StartedAt, now - 20, now + 20)
    Assert.Empty(payload.Data)

    let runEnv = payload.RuntimeEnvironment
    Assert.Equal(runEnv.Ci, "github_actions")

    Assert.Equal(
        runEnv.Key,
        sprintf "%s-%s-%s" env.["GITHUB_ACTION"] env.["GITHUB_RUN_NUMBER"] env.["GITHUB_RUN_ATTEMPT"]
    )

    Assert.Equal(runEnv.Number, Some(env.["GITHUB_RUN_NUMBER"]))
    Assert.Same(runEnv.JobId, None)
    Assert.Equal(runEnv.Branch, Some(env.["GITHUB_REF"]))
    Assert.Equal(runEnv.CommitSha, Some(env.["GITHUB_SHA"]))
    Assert.Equal(runEnv.Message, None)

    Assert.Equal(
        runEnv.Url,
        Some(sprintf "https://github.com/doc-brown/flux-capacitor/actions/run/%s" env.["GITHUB_RUN_ID"])
    )

[<Fact>]
let ``when it detects a Circle CI environment it returns an empty payload`` () =
    let env =
        Map [ ("CIRCLE_BUILD_NUM", rand.Next(999).ToString())
              ("CIRCLE_WORKFLOW_ID", Guid.NewGuid.ToString())
              ("CIRCLE_BUILD_URL", "https://example.test/circle")
              ("CIRCLE_BRANCH", "rufus")
              ("CIRCLE_SHA1", Guid.NewGuid.ToString()) ]

    let payload = Payload.Init(Some(getEnvVarFactory env))
    Assert.True(Option.isSome payload)

    let payload = payload.Value
    let now = Timing.now ()
    Assert.InRange(payload.StartedAt, now - 20, now + 20)
    Assert.Empty(payload.Data)

    let runEnv = payload.RuntimeEnvironment
    Assert.Equal(runEnv.Ci, "circleci")
    Assert.Equal(runEnv.Key, sprintf "%s-%s" env.["CIRCLE_WORKFLOW_ID"] env.["CIRCLE_BUILD_NUM"])
    Assert.Equal(runEnv.Number, Some(env.["CIRCLE_BUILD_NUM"]))
    Assert.Same(runEnv.JobId, None)
    Assert.Equal(runEnv.Branch, Some(env.["CIRCLE_BRANCH"]))
    Assert.Equal(runEnv.CommitSha, Some(env.["CIRCLE_SHA1"]))
    Assert.Equal(runEnv.Message, None)
    Assert.Equal(runEnv.Url, Some "https://example.test/circle")

[<Fact>]
let ``when it detects a generic CI environment it returns an empty payload`` () =
    let env = Map [ ("CI", "true") ]

    let payload = Payload.Init(Some(getEnvVarFactory env))
    Assert.True(Option.isSome payload)

    let payload = payload.Value
    let now = Timing.now ()
    Assert.InRange(payload.StartedAt, now - 20, now + 20)
    Assert.Empty(payload.Data)

    let runEnv = payload.RuntimeEnvironment
    Assert.Equal(runEnv.Ci, "generic")
    Guid.Parse(runEnv.Key) |> ignore
    Assert.Same(runEnv.Number, None)
    Assert.Same(runEnv.JobId, None)
    Assert.Same(runEnv.Branch, None)
    Assert.Same(runEnv.CommitSha, None)
    Assert.Same(runEnv.Message, None)
    Assert.Same(runEnv.Url, None)

let genericEmptyPayload () =
    let env = Map [ ("CI", "true") ]
    Payload.Init(Some(getEnvVarFactory env)).Value

let fakeTest () =
    TestData.Init(Guid.NewGuid.ToString(), Some("scope"), Some("name"), "identifier", Some "location", Some "fileName")

[<Fact>]
let ``AddTestResult adds a TestResult.Test to the Collection`` () =
    let payload = genericEmptyPayload ()

    Assert.Empty(payload.Data)

    let test = fakeTest()

    let payload = Payload.AddTestResult(payload, test)

    Assert.NotEmpty(payload.Data)

    Assert.Equal(test, payload.Data.Head)

[<Fact>]
let ``when the payload has fewer tests than the batch size IntoBatches returns the original payload`` () =
    let payload = Payload.AddTestResult(genericEmptyPayload(), fakeTest())
    let payloads = Payload.IntoBatches(payload, 10)
    Assert.Equal(payloads.Length, 1)
    Assert.Equal(payload, payloads.Head)

let rec payloadStuffer(payload: Payload.Payload, remaining: int) : Payload.Payload =
    if remaining = 0 then
      payload
    else
      let payload = Payload.AddTestResult(payload, fakeTest())
      payloadStuffer(payload, remaining - 1)


[<Fact>]
let ``when the payload has more tests than the batch size IntoBatches returns multiple payloads`` () =
  let payload = genericEmptyPayload()
  let payload = payloadStuffer(payload, 95)
  let payloads = Payload.IntoBatches(payload, 10)

  Assert.Equal(payloads.Length, 10)

  let first_payload = payloads.Head
  let last_payload = List.last payloads

  Assert.Equal(first_payload.Data.Length, 10)
  Assert.Equal(last_payload.Data.Length, 5)

[<Fact>]
let ``AsJson converts the payload into a map`` () =
    let payload = genericEmptyPayload()
    let payload = payloadStuffer(payload, 3)
    let json = Payload.AsJson(payload)

    Assert.Equal(json["format"], "json")
    Assert.Contains("run_env", json.Keys)
    Assert.Contains("data", json.Keys)

    let env = json["run_env"] :?> Map<string, obj>
    Assert.Equal(env["CI"], "generic")

    let data = json["data"] :?> Map<string, obj> list
    Assert.Equal(data.Length, 3)
