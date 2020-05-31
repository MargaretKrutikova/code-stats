module CodeStats.GitIgnoreMatcher

open System.IO
open System
open Microsoft.Extensions.FileSystemGlobbing
open Utils

type GitIgnorePatterns = GitIgnorePatterns of patterns : string seq

let private fileMatchesGitIgnore filePath glob =
  let result = Matcher().AddInclude(glob).Match([filePath])
  result.HasMatches

let private getGitIgnorePath (directoryPath : string) : string =
  Path.Combine [| directoryPath; ".gitignore" |]

let private isValidGitIgnoreEntry (entry : string) =
  (entry.StartsWith("#") || String.IsNullOrWhiteSpace entry) |> not

let transformGitignoreToGlob lines =
  lines 
  |> Seq.map String.trim
  |> Seq.filter isValidGitIgnoreEntry
  // |> Seq.map (fun line -> )

let readGitIgnorePatterns (folder : string) : Async<GitIgnorePatterns> =
  async {
    let gitignorePath = getGitIgnorePath folder
  
    if not <| File.Exists gitignorePath then
      return Seq.empty |> GitIgnorePatterns
    else
      let! entriesToIgnore = File.ReadAllLinesAsync gitignorePath |> Async.AwaitTask
      return entriesToIgnore 
        |> Seq.ofArray 
        |> Seq.map String.trim
        |> Seq.filter isValidGitIgnoreEntry
        |> GitIgnorePatterns
  }

let shouldIgnore (GitIgnorePatterns patterns) (path : string) =
  patterns |> Seq.exists (fileMatchesGitIgnore path)
