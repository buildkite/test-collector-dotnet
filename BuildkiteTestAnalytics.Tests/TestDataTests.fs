module TestDataTests

open System
open Xunit
open BuildkiteTestAnalytics

let rand = Random()

let fakeTest () =
    TestData.Init(Guid.NewGuid.ToString(), Some("scope"), Some("name"), "identifier", Some "location", Some "fileName")

let fakeSpan () =
    Tracing.Init(Tracing.Section.Sql, rand.Next(1000), None, None, None)

[<Fact>]
let ``Init creates a new test`` () =
    let id = Guid.NewGuid().ToString()
    let scope = "BuildkiteTestAnalytics.Tests"
    let name = "Init creates a new test"
    let identifier = "BuildkiteTestAnalytics.TestDataTests.Init creates a new test"
    let location = "BuildkiteTestAnalytics.TestDataTests/TestDataTests.fs: line 8"
    let fileName = "TestDataTests.fs"

    let testData =
        TestData.Init(id, Some(scope), Some(name), identifier, Some(location), Some(fileName))

    let now = Timing.now ()

    Assert.Equal(testData.Id, id)
    Assert.Equal(testData.Scope, Some(scope))
    Assert.Equal(testData.Name, Some(name))
    Assert.Equal(testData.Identifier, identifier)
    Assert.Equal(testData.Location, Some(location))
    Assert.Equal(testData.FileName, Some(fileName))
    Assert.InRange(testData.StartAt, now - 5, now + 5)
    Assert.Equal(testData.EndAt, None)
    Assert.Equal(testData.Duration, None)
    Assert.Empty(testData.Children)
    Assert.Same(testData.Result, TestData.TestResult.Unknown)


[<Fact>]
let ``AddSpan adds a span to a test`` () =
    let test = fakeTest ()
    let span = fakeSpan ()
    let test = TestData.AddSpan(test, span)
    Assert.NotEmpty(test.Children)

[<Fact>]
let ``Passed marks the test as passed`` () =
    let test = fakeTest ()
    let test = TestData.Passed(test)

    Assert.Same(test.Result, TestData.TestResult.Passed)

[<Fact>]
let ``Failed marks the test as failed`` () =
    let test = fakeTest ()
    let test = TestData.Failed(test, None)

    Assert.Equal(test.Result, TestData.TestResult.Failed None)

[<Fact>]
let ``AsJson converts the test into a dict`` () =
    let now = Timing.now ()
    let test = fakeTest ()
    let test = TestData.AddSpan(test, fakeSpan ())
    let json = TestData.AsJson(test, now)

    Assert.Equal(json["id"], test.Id)
    Assert.Equal(json["scope"], test.Scope.Value)
    Assert.Equal(json["name"], test.Name.Value)
    Assert.Equal(json["identifier"], test.Identifier)
    Assert.Equal(json["result"], "unknown")

    let test = TestData.Passed(test)
    let json = TestData.AsJson(test, now)
    Assert.Equal(json["result"], "passed")

    let test = TestData.Failed(test, None)
    let json = TestData.AsJson(test, now)
    Assert.Equal(json["result"], "failed")
    Assert.DoesNotContain(json.Keys, (fun key -> key = "failure_reason"))

    let test = TestData.Failed(test, Some("a perfectly reasonable reason to fail"))
    let json = TestData.AsJson(test, now)
    Assert.Equal(json["result"], "failed")
    Assert.Equal(json["failure_reason"], "a perfectly reasonable reason to fail")

    let history = json["history"] :?> Map<string, obj>
    Assert.Equal(history["section"], "top")
    Assert.Equal(history["start_at"], Timing.elapsedSeconds (test.StartAt, now))
    Assert.Equal(history["end_at"], Timing.elapsedSeconds (test.EndAt.Value, now))
    Assert.Equal(history["duration"], Timing.elapsedSeconds (test.EndAt.Value, test.StartAt))

    let test = TestData.WithDuration(test, 32000)
    let json = TestData.AsJson(test, now)
    let history = json["history"] :?> Map<string, obj>
    Assert.Equal(history["duration"], 32.0)
