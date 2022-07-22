namespace BuildkiteTestAnalytics

open System

/// Helpers for dealing with monitonic time
module Timing =

    /// An instant in milliseconds based on the
    /// <c>System.Environment.TickCount</c>.
    type Instant = int

    /// A duration between two instants.
    type Duration = int

    /// The current monotonic time (in milliseconds).
    let public now () : Instant = Environment.TickCount

    /// Subtract two instants to return a duration.
    let sub (lhs: Instant, rhs: Instant) : Duration = lhs - rhs

    /// Add a duration to an instant and return a new instant.
    let add (lhs: Instant, rhs: Duration) : Instant = lhs + rhs

    /// Convert a duration into seconds.
    let public asSeconds (duration: Duration) : float =
        let duration = float duration
        duration / 1000.00

    /// Return the duration between two instants as seconds.
    let public elapsedSeconds (lhs: Instant, rhs: Instant) : float =
        let duration = sub (lhs, rhs)
        asSeconds duration

    /// <summary>Convert from seconds into a duration</summary>
    /// <remarks>This is inherently innaccurate and is only used in test suites</remarks>
    let public fromSeconds (seconds: float) : Duration =
      let duration = seconds * 1000.0
      int duration
