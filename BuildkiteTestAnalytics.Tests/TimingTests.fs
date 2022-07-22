module TimingTests

open System
open Xunit
open BuildkiteTestAnalytics
open System.Threading

[<Fact>]
let ``now uses the monotonic clock`` () =
    let instant = Timing.now ()
    let tc = Environment.TickCount
    Assert.InRange(instant, tc - 10, tc + 10)

[<Fact>]
let ``subtracing two instants returns a duration`` () =
    let i0 = Timing.now ()
    Thread.Sleep(75)
    let i1 = Timing.now ()
    let duration = Timing.sub (i1, i0)
    Assert.InRange(duration, 70, 80)

[<Fact>]
let ``adding a duration to an instant returns a new instant`` () =
    let instant = Timing.now ()
    let duration = 25
    let result = Timing.add (instant, duration)
    let tc = Environment.TickCount
    Assert.InRange(result, tc + 20, tc + 30)

[<Fact>]
let ``asSeconds converts a duration into seconds`` () =
    let duration = 27565
    let seconds = Timing.asSeconds duration
    Assert.InRange(seconds, 27.55, 27.57)

[<Fact>]
let ``elapsedSeconds returns the seconds between two instants`` () =
    let i0 = Timing.now ()
    let i1 = i0 + 27565
    let seconds = Timing.elapsedSeconds (i1, i0)
    Assert.InRange(seconds, 27.55, 27.57)
