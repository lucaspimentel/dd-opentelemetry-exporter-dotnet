# Datadog Exporter for OpenTelemetry .NET

## Usage

### ASP.NET Core

- Add the following NuGet packages to your project:

```shell
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package LucasP.Datadog.OpenTelemetry.Exporter
```

- Add the exporter to your application:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    // ...
                    builder.AddDatadogExporter();
                };
```

### Console

- Add the following NuGet package to your project:

```shell
dotnet add package LucasP.Datadog.OpenTelemetry.Exporter
```

- Add the exporter to your application:

```csharp
var tracerProvider = Sdk.CreateTracerProviderBuilder()
                        .AddSource("MyCompany.MyProduct.MyLibrary")
                        .AddDatadogExporter()
                        .Build();
```