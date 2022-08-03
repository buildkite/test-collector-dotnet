# Buildkite Test Collector for .NET (Beta)

The official .NET adapter for [Buildkite Test Analytics](https://buildkite.com/test-analytics) which collects information about your tests.

⚒ **Supported test frameworks:** Xunit.

📦 **Supported CI systems:** Buildkite, GitHub Actions, CircleCI, and others via the `BUILDKITE_ANALYTICS_*` environment variables.


## 👉 Installing

1. [Create a test suite](https://buildkite.com/docs/test-analytics), and copy the API token that it gives you.

2. Add `Buildkite.TestAnalytics.Xunit` to your list of dependencies in your Xunit test project:

```sh
$ dotnet add package Buildkite.TestAnalytics.Xunit
```

3. Set up your API token

Add the `BUILDKITE_ANALYTICS_TOKEN` environment variable to your build system's environment.

4. Run your tests

Run your tests like normal.  Note that we attempt to detect the presence of several common CI environments, however if this fails you can set the `CI` environment variable to any value and it will work.

```sh
$ dotnet test Buildkite.TestAnalytics.Tests
```

5. Verify that it works

If all is well, you should see the test run in the test analytics section of the Buildkite dashboard.

## 🔜 Roadmap

See the [GitHub 'enhancement' issues](https://github.com/buildkite/test-collector-dotnet/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement) for planned features. Pull requests are always welcome, and we’ll give you feedback and guidance if you choose to contribute 💚

## ⚒ Developing

1. Cloning the repository.
2. Run the tests:
   `dotnet test`

Useful resources for developing collectors include the [Buildkite Test Analytics docs](https://buildkite.com/docs/test-analytics) and the [RSpec and Minitest collectors](https://github.com/buildkite/rspec-buildkite-analytics).

## 👩‍💻 Contributing

Bug reports and pull requests are welcome on GitHub at https://github.com/buildkite/test-collector-python

## 🚀 Releasing

1. Version bump the code, tag and push.
2. Publish to [NuGet](https://www.nuget.org/).

3. Create a [new github release](https://github.com/buildkite/test-collector-dotnet/releases).

## 📜 License

The package is available as open source under the terms of the [MIT License](https://opensource.org/licenses/MIT).

## 🤙 Thanks

Thanks to the folks at [Alembic](https://alembic.com.au/) for building and maintaining this package.
