# Testing Functions

In many cases, the unit tests for a function won't need to use the
Functions Framework at all. They can simply create the function
instance directly, specifying appropriate forms of any dependencies, and
invoke the function.

Integration testing is relatively straightforward, based on the
regular ASP.NET Core pattern of creating a test server, then
invoking it with an appropriately-configured `HttpClient`.

## Creating an IHostBuilder with EntryPoint.CreateHostBuilder

The hosting [EntryPoint](../src/OpenFunction.Hosting/EntryPoint.cs) class
not only contains the `Main` method used automatically to start the
server, but an overloaded `CreateHostBuilder` method. The generic,
parameterless overload expects the type argument to be a function
type, and creates a suitable `IHostBuilder` with no additional
environment.

A non-generic overload accepting a `Type` parameter is equivalent to
the generic overload, but without needing to know the function type
at compile-time.

Finally, there's a non-generic overload accepting a string-to-string
dictionary of fake environment variables and a function assembly.
This provides more control for scenarios where you may want to
simulate running in a container, or running in a Knative environment.

The `IHostBuilder` can be used in conjunction with the
[Microsoft.AspNetCore.TestHost](https://www.nuget.org/packages/Microsoft.AspNetCore.TestHost)
NuGet package to create a test server and execute requests against it.

See [SimpleHttpFunctionTest](../examples/OpenFunction.Examples.IntegrationTests/SimpleHttpFunctionTest.cs)
for an example of this.

## Creating a FunctionTestServer

Using `TestHost` directly can be slightly verbose - it's not too bad
for an occasional test, but not something you'd want to use in a
large number of tests. The
[OpenFunction.Testing](../src/OpenFunction.Testing)
package provides a [FunctionTestServer](../src/OpenFunction.Testing/FunctionTestServer.cs)
class to simplify this. The generic `FunctionTestServer<TFunction>`
class is a derived class allowing you to use the function type as a
type argument, avoiding the need for any other configuration.

`FunctionTestServer` is typically used in one of four ways. The
examples given below are for xUnit, but other test frameworks provide
similar functionality.

- Most simply, you can derive your test class from
  `FunctionTestBase<TFunction>`, which is described below.
- It can be constructed directly within the test. This should be in the context
  of a `using` statement to dispose of the server afterwards. See
  [SimpleHttpFunctionTest_WithTestServerInTest](../examples/OpenFunction.Examples.IntegrationTests/SimpleHttpFunctionTest_WithTestServerInTest.cs)
  for an example of this.
- It can be constructed directly in the test class constructor,
  and then disposed in an `IDisposable.Dispose()` implementation, which is
  automatically called by xUnit. This means a new server is constructed
  for each test. See
  [SimpleHttpFunctionTest_WithTestServerInCtor](../examples/OpenFunction.Examples.IntegrationTests/SimpleHttpFunctionTest_WithTestServerInCtor.cs)
  for an example of this.
- It can be automatically constructed by xUnit as part of a fixture, then disposed
  of automatically when the fixture is disposed. This means a single server
  is used for all tests in the fixture. See
  [SimpleHttpFunctionTest_WithTestServerFixture](../examples/OpenFunction.Examples.IntegrationTests/SimpleHttpFunctionTest_WithTestServerFixture.cs)
  for an example of this.
  
## Simple testing with `FunctionTestBase<TFunction>`

Where the test framework you're using supports it,
`FunctionTestBase<TFunction>` is the simplest way of writing
function tests. See
[SimpleHttpFunctionTest_WithFunctionTestBase](../examples/OpenFunction.Examples.IntegrationTests/SimpleHttpFunctionTest_WithFunctionTestBase.cs)
for an example.

`FunctionTestBase<TFunction>` is an abstract class designed to be a
base class for integration tests of a function (`TFunction`) in
test frameworks that automatically dispose of test classes. (This
includes NUnit and xUnit.) The `Dispose` implementation disposes of
the test server that is owned by the test class.

The parameterless constructor of `FunctionTestBase<TFunction>`
automatically creates a `FunctionTestServer<TFunction>`, or you can
pass the test server into the constructor if you want to customize it.

`FunctionTestBase<TFunction>` provides convenient access to the logs using
`GetFunctionLogEntries()`, as well as methods to execute HTTP
requests against the test server, in one of three ways:

- Performing a simple GET request, expecting the result to be text
  which is returned.
- Performing any HTTP request expressed via `HttpRequestMessage`,
  with validators executed against the `HttpResponseMessage` (before
  both the response message and the `HttpClient` are disposed)
- Performing an HTTP request containing a CloudEvent, and asserting
  that the response indicated success. (CloudEvent functions do not
  return a response, so typically a test would then make assertions
  about the side-effects of the request.)

## Customizing the test server with `FunctionTestServerBuilder`

By default, `FunctionTestServer` configures the server in the same
way as in production, just without using Kestrel or console logging.
For simple functions that's fine, but if your function uses
dependencies such as web APIs that you want to fake/mock in tests,
you need to be able to replace the [Functions Startup
classes](customization.md) that are usually used to configure
dependencies, middleware and so on.

`FunctionTestServerBuilder` allows you to create a test server that
uses specified `FunctionsStartup` instances instead of detecting the
Functions Startup classes based on assembly and type attributes. To create
the builder, start by calling one of the `Create` overloads, either
specifying the function target as a `Type` reference, or as a generic
type argument.

After that, just call the
`FunctionTestServerBuilder.UseFunctionsStartups` method and pass in
the complete set of startup instances, or `null` to use the
production behavior of using assembly attributes. The method returns
the same builder it's called on, so you can chain method calls easily.

Alternatively, you can decorate a type with the
`FunctionsStartupAttribute` to specify the startup classes to
use, and call `FunctionTestServerBuilder.MaybeUseFunctionsStartupsFromAttributes`
to apply the startup classes. This is called automatically in the
`FunctionTestBase<TFunction>` parameterless constructor, using the
test class itself - so adding the `FunctionsStartupAttribute` to
the test class is the simplest way of configuring startups in those
tests. If all your tests in a test project will need the same set of
test startup classes, you can specify those startup classes by
applying `FunctionsStartupAttribute` to the test assembly instead of
individual tests. Note that if *any* startup classes are detected on
either the test class or the test assembly, these will replace *all*
of the startup classes from the production code.

The `FunctionTestServerBuilder.UseFunctionsFrameworkConsoleLogging`
method can be used to enable console logging in addition to the
in-memory logging.

The `Build()` method on `FunctionTestServerBuilder` constructs a
`FunctionTestServer` for the given function target, using the
configured behavior.

See [TestableDependenciesTest.cs](../examples/OpenFunction.Examples.IntegrationTests/TestableDependenciesTest.cs)
for an example of using `FunctionTestServerBuilder` in
`FunctionTestBase` subclass to test a function that needs a custom
dependency for testing.

## Testing logs with FunctionTestServer

`FunctionTestServer` replaces the default console logging with an
in-memory logger provider, allowing you to retrieve logs by category
using the `GetLogEntries` method.

Additionally, the `GetFunctionLogEntries` method in
`FunctionTestServer<TFunction>` retrieves the log entries
corresponding with the function type's full name. If you inject an
`ILogger<T>` where the type argument is the function type (as is the
convention) then this method will provide the log entries for that
logger. See
[SimpleDependencyInjectionTest.cs](../examples/OpenFunction.Examples.IntegrationTests/SimpleDependencyInjectionTest.cs)
for an example of how this can be used.

Currently the functionality is somewhat primitive, but should meet
the demands of most users. Aspects that may change later include:

- The logger provider is configured after any other services. If you
  manually configure services during startup, eagerly loading
  singletons, the in-memory logger will not be configured.
- If the test server is constructed as part of a fixture, the logs
  persist between tests. (A `ClearLogs` method allows you to
  explicitly clear the logs if you need to.)
- The logger provider is unconditionally installed, and has an
  unlimited buffer size. This makes the test server inappropriate
  for long-running soak tests, for example. In the future we may
  provide options to disable the in-memory logger, or limit its
  log entry buffer.
- The logger only provides the log message rather than separate
  parts of the log entry that may be available, such as placeholder
  replacement values.
- The logger does not support log scopes; all log entries are stored
  in a flat structure.

## Testing logs in unit tests

If you want to write a unit test for a function whose constructor
accepts an `ILogger<T>`, you can use the built-in ASP.NET Core
`NullLogger<T>` if you don't need to test the log entries. However,
this doesn't help if you want to make sure that the expected log
entries are emitted. The logger implementation used by
`FunctionTestServer` is also available for standalone testing. Just
create an instance of
[FunctionTestServer&lt;TCategoryName>](../src/OpenFunction.Testing/MemoryLogger.cs)
and pass that to the constructor. You can then retrieve a snapshot
at any time by calling `ListLogEntries()` on the logger, or clear it
using the `Clear()` method.

See
[SimpleDependencyInjectionUnitTest.cs](../examples/OpenFunction.Examples.IntegrationTests/SimpleDependencyInjectionUnitTest.cs)
for an example of a unit test that validates a log entry.
