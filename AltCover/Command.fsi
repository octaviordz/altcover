﻿// # namespace `AltCover`
// ```
namespace AltCover
// ```
// ## module `Command`
// ```
module Command = begin
  val Prepare : args:Abstract.IPrepareOptions -> log:AltCover.LoggingOptions -> int
  val Collect : args:Abstract.ICollectOptions -> log:AltCover.LoggingOptions -> int
  val ImportModule : unit -> string
  val Version : unit -> System.Version
  val FormattedVersion : unit -> string
  val Summary : unit -> string
end
// ```
//
// where `int` results are 0 for success and otherwise for failure (this would be the return code of the operation if run as a command-line function)
//
// `FormattedVersion` return is of the form "AltCover version #.#.##.##" as per the command-line "Version" option.
//
// `Summary` returns the last computed coverage summary if any