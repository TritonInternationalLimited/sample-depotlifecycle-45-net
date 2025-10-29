This project contains a sample call to Triton's Test IICL Depot Lifecycle API using .net 4.5.

A valid API token is expected to be set to the environment variable TRITON_API_TOKEN.
TLS 1.2 is required. It is set explicitly using the following line:

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

One NuGet package dependency is Newtonsoft.Json - older versions of Visual Studio 2013 default to TLS 1.0/1.1. This prevents NuGet from reaching the package index. To set in visual studio go to Tools > NuGet Package Manager > Package Manager Console. In the console, use the following command:

[Net.ServicePointManager]::SecurityProtocol=[Net.ServicePointManager]::SecurityProtocol-bOR [Net.SecurityProtocolType]::Tls12

This will set the TLS for NuGet for that single session. If you close or restart Visual Studio, this command will need to be ran again.
