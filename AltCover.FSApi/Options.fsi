﻿#if RUNNER
// # namespace `AltCover`
// ```
namespace AltCover
// ```
#else
// # namespace `AltCoverFake.DotNet.Testing`
// ```
namespace AltCoverFake.DotNet.Testing
// ```
#endif

open System.Collections.Generic

// ## module `Options`
// This holds the weakly ("stringly") typed equivalent of the command line options in a C# friendly manner
// as classes with the values expressed as properties
//
// Refer to the types in C# either as
//
// ```
// using Altcover;
// Options.PrepareOptions prep = ...  // or whichever
// ```
// or
// ```
// using static Altcover.Options;
// PrepareOptions prep = ... // or whichever
// ```
//
// method `new` is the default constructor
// static method `Copy` takes an interface instance and returns an instance of this concrete type with the same values. Sequence values are copied.
//
// ```
module Options =
// ```
// ## type `CollectOptions`
//  This type defines the Collect (runner) behaviour.  The properties map on to the command line arguments for `altcover runner`
//
// ```
  type CollectOptions =
    class
      interface Abstract.ICollectOptions
      new : unit -> CollectOptions
      member Cobertura : System.String with get, set
      member CommandLine : IEnumerable<System.String> with get, set
      member Executable : System.String with get, set
      member ExposeReturnCode : bool with get, set
      member LcovReport : System.String with get, set
      member OutputFile : System.String with get, set
      member RecorderDirectory : System.String with get, set
      member SummaryFormat : System.String with get, set
      member Threshold : System.String with get, set
      member WorkingDirectory : System.String with get, set
      static member Copy : source:Abstract.ICollectOptions -> CollectOptions
    end

// ```
// ## type `PrepareOptions`
//
//  This type defines the Prepare (instrumentation) behaviour.    The properties map on to the command line arguments for `altcover`
// ```
  type PrepareOptions =
    class
      interface Abstract.IPrepareOptions
      new : unit -> PrepareOptions
      member AssemblyExcludeFilter : IEnumerable<System.String> with get, set
      member AssemblyFilter : IEnumerable<System.String> with get, set
      member AttributeFilter : IEnumerable<System.String> with get, set
      member BranchCover : bool with get, set
      member CallContext : IEnumerable<System.String> with get, set
      member CommandLine : IEnumerable<System.String> with get, set
      member Defer : bool with get, set
      member Dependencies : IEnumerable<System.String> with get, set
      member ExposeReturnCode : bool with get, set
      member FileFilter : IEnumerable<System.String> with get, set
      member InPlace : bool with get, set
      member InputDirectories : IEnumerable<System.String> with get, set
      member Keys : IEnumerable<System.String> with get, set
      member LineCover : bool with get, set
      member LocalSource : bool with get, set
      member MethodFilter : IEnumerable<System.String> with get, set
      member MethodPoint : bool with get, set
      member OutputDirectories : IEnumerable<System.String> with get, set
      member PathFilter : IEnumerable<System.String> with get, set
      member ReportFormat : System.String with get, set
      member Save : bool with get, set
      member ShowGenerated : bool with get, set
      member ShowStatic : string with get, set
      member SingleVisit : bool with get, set
      member SourceLink : bool with get, set
      member StrongNameKey : System.String with get, set
      member SymbolDirectories : IEnumerable<System.String> with get, set
      member TypeFilter : IEnumerable<System.String> with get, set
      member VisibleBranches : bool with get, set
      member XmlReport : System.String with get, set
      member ZipFile : bool with get, set
      static member Copy : source:Abstract.IPrepareOptions -> PrepareOptions
  end

// ```
#if RUNNER
// ### type `LoggingOptions`
// Defines how to log output from the `altcover` operation
//
// ```
  type LoggingOptions =
    class
      interface Abstract.ILoggingOptions
      new : unit -> LoggingOptions
      member Echo : System.Action<System.String> with get, set
// ```
// Sink for the synthetic command line in case of inconsistent inputs.
// ```
      member Failure : System.Action<System.String> with get, set
// ```
// Sink for error messages.
// ```
      member Info : System.Action<System.String> with get, set
// ```
// Sink for informational messages.
// ```
      member Warn : System.Action<System.String> with get, set
// ```
// Sink for warning messages
// ```
    end

#endif