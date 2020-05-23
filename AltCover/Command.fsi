﻿// # namespace `AltCover`
// ```
namespace AltCover
// ```
// ## module `Command`
//
// ```
///<summary>
/// This represents the various operations available
///</summary>
module Command = begin
  ///<summary>
  /// Instrument assemblies
  /// <param name="args">The command line</param>
  /// <param name="log">How to report feedback</param>
  /// <returns>operation return code.</returns>
  ///</summary>
  val Prepare : args:Abstract.IPrepareOptions -> log:AltCover.LoggingOptions -> int
  ///<summary>
  /// Process coverage
  /// <param name="args">The command line</param>
  /// <param name="log">How to report feedback</param>
  /// <returns>operation return code.</returns>
  ///</summary>
  val Collect : args:Abstract.ICollectOptions -> log:AltCover.LoggingOptions -> int
  ///<summary>
  /// Indicate how to consume for PowerShell
  /// <returns>The `Import-Module` command required.</returns>
  ///</summary>
  val ImportModule : unit -> string
  ///<summary>
  /// Indicate the current version
  /// <returns>The strongly-typed version.</returns>
  ///</summary>
  val Version : unit -> System.Version
  ///<summary>
  /// Indicate the current version
  /// <returns>The version as a string.</returns>
  ///</summary>
  val FormattedVersion : unit -> string
  ///<summary>
  /// Return the last computed coverage summary
  /// <returns>The last computed coverage summary.</returns>
  ///</summary>
  val Summary : unit -> string
end
// ```
//
// where `int` results are 0 for success and otherwise for failure (this would be the return code of the operation if run as a command-line function)
//
// `FormattedVersion` return is of the form "AltCover version #.#.##.##" as per the command-line "Version" option.
//
// `Summary` returns the last computed coverage summary if any