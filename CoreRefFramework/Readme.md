# .NET Core Api Referencing .NET Framework Assembly Causing Issues During Integration Tests

This is a sample project to demonstrate the issue I am facing when trying to reference a .NET Framework assembly from a .NET Core API project. The issue is that the integration tests are failing even though I'm not directly testing the code that references the .NET Framework assembly.

Strangely enough, the Api runs correctly and I can successfully enqueue and process the Hangfire job when I just debug the Api and test via Postman (or similar). The issue only occurs when running the integration tests.

**Reference Links**

1. My [Hangfire Issue](https://github.com/HangfireIO/Hangfire/issues/2400) asking about mixing .NET Core and .NET Framework assemblies.  I worked around that issue by creating a `string` overload in `JobInvoker`.
1. My [XUnit Question](https://github.com/xunit/xunit/discussions/2923) asking about mixing .NET Core and .NET Framework assemblies.  I created a `MSTest` version of the test and the issue occurred in both XUnit and MSTest and the call stack hinted towards FastEndpoints.
1. My [FastEndpoints Discussion](https://discord.com/channels/933662816458645504/1237514511490220072/1237514511490220072) where I asked same questions and was requested to make a repository to demonstrate the issue.

**Repository Structure**

This repository was created as 'fast/easy/simple' as I could.  To get around my real project hierarchy, I simply copied any file I needed from a dependent project into a folder names `[CamelotDependencies]`.  I don't think you'll ever have to look in these folders/code.  Excuse the bloated using statements.  As I cut code out when possible, my OmniSharp extension was not working so I couldn't remove unused usings easily, so I just left them.

You might also come across a `FastEndpointsExtensions.cs` file.  You'll see that it is basically just a copy of your (recent) source so that I had the proper call to the new(ish) `AddAuthenticationJwtBearer` extension method letting me prepare for a migration to the latest FE.

**Reproduction Steps**

The code is currently pushed in a state that will cause an error if you execute `dotnet test Camelot.Api.Excel.Tests.Integration.csproj`.  To demonstrate that the issue is the reference to .NET Framework, you can do the following steps to run the tests successfully.

1. In the `Camelot.Api.Excel.csproj` file, you can comment out the reference to `BTR.Evolution.Hangfire.Schedulers`.

```xml
<ItemGroup>
	<Reference Include="BTR.Evolution.Hangfire.Schedulers">
		<HintPath>..\..\Assemblies\BTR.Evolution.Hangfire.Schedulers.dll</HintPath>
	</Reference>
</ItemGroup>
```

2. In `EmailBlast.cs`, comment out the following lines:

```csharp
var hangfireId = HF.BackgroundJob.Enqueue( () => 
	new BTR.Evolution.Hangfire.Schedulers.JobInvoker().Invoke(
		$"Email Blast From {authId}",
		inputPackage.ToString(),
		null,
		HF.JobCancellationToken.Null 
	)
);
await SendStringAsync( hangfireId, cancellation: c );
```

3. Finally, in the same file, uncomment out (to execute) `await SendStringAsync( "1234", cancellation: c );` and re-issue the `dotnet test` command.

**Strange Behavior in this Repository**

When I build Camelot.Api.Excel.csproj, I get warnings in the following format:

```
C:\Program Files\dotnet\sdk\8.0.204\Microsoft.Common.CurrentVersion.targets(2389,5): warning MSB3277: Found conflicts between different versions of "Newtonsoft.Json" that could not be resolved. [C:\BTR\Camelot\Api\FastEndpoints\CoreRefFramework\Api\src\Camelot.Api.Excel.csproj]
C:\Program Files\dotnet\sdk\8.0.204\Microsoft.Common.CurrentVersion.targets(2389,5): warning MSB3277: There was a conflict between "Newtonsoft.Json, Version=11.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed" and "Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed". [C:\BTR\Camelot\Api\FastEndpoints\CoreRefFramework\Api\src\Camelot.Api.Excel.csproj]
```

1. My .NET Framework library **does** reference Newtonsoft.Json 13.0.0.1, but my .NET Core project(s) **do not** reference Newtonsoft.Json at all that I can find.  I'm not sure why this warning is occurring.
1. To add to the confusion, when I build my projects in their real structure/repositories, I do not get any warnings like this.

**FastEndpoints**: Let me know if you want me to post my real project structure.