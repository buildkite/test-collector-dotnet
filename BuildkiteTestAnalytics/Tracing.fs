namespace BuildkiteTestAnalytics

/// Support for storing trace information
module Tracing =
    open System.Collections.Generic
    open BuildkiteTestAnalytics.Timing

    /// Allowed section types
    type public Section =
        | Http
        | Sql
        | Sleep
        | Annotation

    /// An individual trace span
    type public Span =
        { Section: Section
          Duration: Duration
          StartAt: Instant option
          EndAt: Instant option
          Detail: string option }

    /// <summary>Initialise a new Span</summary>
    /// <param name="section">The "section" of the span</param>
    /// <param name="duration">How long the section took to execute</param>
    /// <param name="startAt">The time that the section started</param>
    /// <param name="endAt">The time that the section ended</param>
    let public Init
        (
            section: Section,
            duration: Duration,
            startAt: Instant option,
            endAt: Instant option,
            detail: string option
        ) : Span =
        { Section = section
          Duration = duration
          StartAt = startAt
          EndAt = endAt
          Detail = detail }


    let private sectionToString (section: Section) : string =
        match section with
        | Http -> "http"
        | Sql -> "sql"
        | Sleep -> "sleep"
        | Annotation -> "annotation"

    /// <summary>Convert the Span into a format directly serialisable as
    /// JSON</summary>
    /// <param name="span">The Span to format</param>
    /// <param name="epoch">The time relative to which to calculate time
    /// offsets</param>
    let public AsJson (span: Span, epoch: Instant) : Map<string, obj> =
        let attrs =
            [ ("section", sectionToString (span.Section) :> obj)
              ("duration", asSeconds (span.Duration) :> obj) ]

        let attrs =
            match span.StartAt with
            | Some (startAt) ->
                ("start_at", Timing.elapsedSeconds (startAt, epoch) :> obj)
                :: attrs
            | None -> attrs

        let attrs =
            match span.EndAt with
            | Some (endAt) ->
                ("end_at", Timing.elapsedSeconds (endAt, epoch) :> obj)
                :: attrs
            | None -> attrs

        let attrs =
            match span.Detail with
            | Some (detail) -> ("detail", detail :> obj) :: attrs
            | None -> attrs

        Map attrs
