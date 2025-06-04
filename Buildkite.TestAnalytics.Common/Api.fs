namespace Buildkite.TestAnalytics.Common

/// Api client
///
/// Handles posting a Payload to the API.
module Api =

    open System.Net.Http
    open System.Net.Http.Headers
    open System

    /// Configuration options for the API client.
    type Config = {
          batchSize: int
          apiToken: string
        }

    /// The default API configuration
    let defaultConfig = {
        batchSize = 100
        apiToken = Environment.GetEnvironmentVariable("BUILDKITE_ANALYTICS_TOKEN")
      }

    let private submitBatch (payload: Payload.Payload, client: HttpClient) =
        let payload = Payload.ToJson(payload)
        let content = new StringContent(payload)
        let _ = content.Headers.ContentType <- new MediaTypeHeaderValue("application/json")

        task {

            let! response =
                client.PostAsync(
                    "https://analytics-api.buildkite.com/v1/uploads",
                    content
                )

            let! body = response.Content.ReadAsStringAsync()


            if response.IsSuccessStatusCode = false then
                Printf.eprintfn $"Posting execution results to Buildkite API failed: {response.StatusCode}"
        }

    /// <summary>Submits a payload to the Buildkite API.</summary>
    /// <remarks>Note that this function should never fail (so as to not disrupt
    /// the normal function of your test suite), also that for large payloads it
    /// may make several API calls depending the value of the batchSize
    /// configuration value</remarks>
    let submit (payload: Payload.Payload, config: Config option) =
        let config = (defaultConfig, config) ||> Option.defaultValue
        let client = new HttpClient()
        let _ = client.DefaultRequestHeaders.Authorization <- new AuthenticationHeaderValue("Token", config.apiToken)
        let payloads = Payload.IntoBatches(payload, config.batchSize)

        if String.IsNullOrWhiteSpace config.apiToken then
          printf "Skipping sending test execution data to Buildkite, BUILDKITE_ANALYTICS_TOKEN is not set."
        else
          printf "Sending test execution data to Buildkite..."

          let task = task {
              for payload in payloads do
                  let task = submitBatch (payload, client)
                  task.Wait()
                  ()
          }
          task.Wait()

          printfn " done."
