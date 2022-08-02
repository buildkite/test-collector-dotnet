module TracingTests

open System
open Xunit
open Buildkite.TestAnalytics.Common

let rand = Random()

[<Fact>]
let ``Init creates a new span`` () =
    let section = Tracing.Section.Sql
    let duration = rand.Next(1000)
    let startAt = Timing.now ()
    let endAt = Timing.now () + duration
    let detail = "Little Bobby Tables"

    let span = Tracing.Init(section, duration, Some(startAt), Some(endAt), Some(detail))

    Assert.Equal(span.Section, section)
    Assert.Equal(span.Duration, duration)
    Assert.Equal(span.StartAt, Some(startAt))
    Assert.Equal(span.EndAt, Some(endAt))
    Assert.Equal(span.Detail, Some(detail))

[<Fact>]
let ``AsJson converts the span into a dict`` () =
    let epoch = Timing.now ()
    let section = Tracing.Section.Sql
    let duration = rand.Next(1000)
    let startAt = Timing.now () + duration
    let endAt = startAt + duration * 2
    let detail = "Little Bobby Tables"

    let span = Tracing.Init(section, duration, Some(startAt), Some(endAt), Some(detail))
    let json = Tracing.AsJson(span, epoch)

    Assert.Equal(json["section"], "sql")
    Assert.Equal(json["duration"], Timing.asSeconds (duration))
    Assert.Equal(json["start_at"], Timing.elapsedSeconds (startAt, epoch))
    Assert.Equal(json["end_at"], Timing.elapsedSeconds (endAt, epoch))
    Assert.Equal(json["detail"], detail)

    let span = { span with StartAt = None }
    let json = Tracing.AsJson(span, epoch)

    Assert.DoesNotContain(json.Keys, (fun key -> key = "start_at"))

    let span = { span with EndAt = None }
    let json = Tracing.AsJson(span, epoch)

    Assert.DoesNotContain(json.Keys, (fun key -> key = "duration_at"))

    let span = { span with Detail = None }
    let json = Tracing.AsJson(span, epoch)

    Assert.DoesNotContain(json.Keys, (fun key -> key = "detail"))
