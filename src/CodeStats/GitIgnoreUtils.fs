module CodeStats.GitIgnoreUtils

open System
open Utils

let isValidGitIgnoreEntry (entry : string) =
  (entry.StartsWith("#") || String.IsNullOrWhiteSpace entry || entry = "!") |> not

let isNegatingPattern = String.startsWith "!"

let fromNegatingPattern (pattern : string) =
  pattern.Substring(1, pattern.Length - 1)

let getTrailingWildcardPattern (pattern : string) = pattern + "/**"

let getStartingWildcardPattern (pattern : string) =
  if pattern |> String.startsWith "/" |> not then
    "**/" + pattern |> Some
  else 
    None
