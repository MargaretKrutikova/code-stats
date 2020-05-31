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
let private fromNegatingPattern (pattern : string) =
  pattern.Substring(1, pattern.Length)

let private toGitIgnorePattern (ValidGitIgnoreEntry entry) =
  if isNegatingPattern entry then
    { Pattern = fromNegatingPattern entry; IsNegating = true }
  else 
    { Pattern = entry; IsNegating = false }

let getGitIgnorePath (directoryPath : string) : string =
  Path.Combine [| directoryPath; ".gitignore" |]

let transformToGitIgnorePatterns (lines : string seq) =
    lines
    |> Seq.map toValidGitIgnoreEntry
    |> Seq.choose id
    |> Seq.map toGitIgnorePattern
    |> GitIgnorePatterns

let shouldIgnoreFile (filePath : string) (GitIgnorePatterns patterns) =
  let folder (matcher : Matcher) entry =
    if entry.IsNegating then
      matcher.AddExclude(entry.Pattern)
    else
      matcher.AddInclude(entry.Pattern)

  patterns 
  |> Seq.fold folder (Matcher())
  |> Matcher.performMatch filePath
  |> Matcher.hasMatches

let readGitIgnorePatterns (folder : string) : Async<GitIgnorePatterns> =
  async {
    let gitignorePath = getGitIgnorePath folder
  
    if not <| File.Exists gitignorePath then
      return Seq.empty |> GitIgnorePatterns
    else
      let! entriesToIgnore = File.ReadAllLinesAsync gitignorePath |> Async.AwaitTask
      return entriesToIgnore |> transformToGitIgnorePatterns
  }

