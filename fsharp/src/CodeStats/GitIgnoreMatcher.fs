module CodeStats.GitIgnoreMatcher

open System.IO
open System
open Microsoft.Extensions.FileSystemGlobbing
open Utils

type GitIgnorePatterns = GitIgnorePatterns of patterns : GitIgnorePattern seq
and GitIgnorePattern = {
  Pattern : string
  IsNegating : bool
}

type GitIgnoreMatchResult = MatchesIgnorePattern | MatchesNegationPattern | NoMatch

type private ValidGitIgnoreEntry = ValidGitIgnoreEntry of string

let private isValidGitIgnoreEntry (entry : string) =
  (entry.StartsWith("#") || String.IsNullOrWhiteSpace entry || entry = "!") |> not

let private toValidGitIgnoreEntry (entry : string) =
  let transformed = entry |> String.trim
  if isValidGitIgnoreEntry transformed then 
    ValidGitIgnoreEntry transformed |> Some
  else 
    None

let private isNegatingPattern = String.startsWith "!"
let fromNegatingPattern (pattern : string) =
  pattern.Substring(1, pattern.Length - 1)

let private toGitIgnorePattern (ValidGitIgnoreEntry entry) =
  if isNegatingPattern entry then
    { Pattern = fromNegatingPattern entry; IsNegating = true }
  else 
    { Pattern = entry; IsNegating = false }

let private getTrailingWildcardPattern (pattern : string) =
  pattern + "/**"

let private getStartingWildcardPattern (pattern : string) =
  if pattern |> String.startsWith "/" |> not then
    "**/" + pattern |> Some
  else 
    None

let getGitIgnorePath (directoryPath : string) : string =
  Path.Combine [| directoryPath; ".gitignore" |]

let transformToGitIgnorePatterns (lines : string seq) =
    lines
    |> Seq.map toValidGitIgnoreEntry
    |> Seq.choose id
    |> Seq.map toGitIgnorePattern
    |> GitIgnorePatterns

let evaluateGitIgnoreMatchResult (filePath : string) (entry : GitIgnorePattern) : GitIgnoreMatchResult =
  let folder (matcher : Matcher) pattern = matcher.AddInclude(pattern)
  let matchResult = 
    [
      entry.Pattern |> Some
      getTrailingWildcardPattern entry.Pattern |> Some
      getStartingWildcardPattern entry.Pattern
    ]
     |> List.choose id
     |> List.fold folder (Matcher())
     |> Matcher.performMatch filePath

  if matchResult.HasMatches |> not then
    NoMatch 
  else if entry.IsNegating then
    MatchesNegationPattern
  else MatchesIgnorePattern

let combineMatchResults (left : GitIgnoreMatchResult) (right : GitIgnoreMatchResult) : GitIgnoreMatchResult =
  match left, right with
  | MatchesIgnorePattern, NoMatch -> MatchesIgnorePattern
  | MatchesNegationPattern, NoMatch -> MatchesNegationPattern
  | _, MatchesIgnorePattern -> MatchesIgnorePattern
  | _, MatchesNegationPattern -> MatchesNegationPattern
  | NoMatch, NoMatch -> NoMatch

let shouldIgnoreFile (GitIgnorePatterns patterns) (filePath : string) : bool =
  let folder (matchResult : GitIgnoreMatchResult) (pattern : GitIgnorePattern) =
    evaluateGitIgnoreMatchResult filePath pattern |> combineMatchResults matchResult

  match patterns |> Seq.fold folder NoMatch with 
  | MatchesIgnorePattern -> true
  | MatchesNegationPattern -> false
  | NoMatch -> false

let readGitIgnorePatterns (folder : string) : Async<GitIgnorePatterns> =
  async {
    let gitignorePath = getGitIgnorePath folder
  
    if not <| File.Exists gitignorePath then
      return Seq.empty |> GitIgnorePatterns
    else
      let! entriesToIgnore = File.ReadAllLinesAsync gitignorePath |> Async.AwaitTask
      return entriesToIgnore |> transformToGitIgnorePatterns
  }

