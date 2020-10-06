module CodeStats.GitIgnoreMatcher

open System.IO
open Microsoft.Extensions.FileSystemGlobbing
open GitIgnoreUtils
open Utils

type GitIgnorePatterns = GitIgnorePatterns of patterns : GitIgnorePattern seq
and GitIgnorePattern = {
  GlobPatterns : string seq
  IsNegating : bool
}

type GitIgnoreMatchResult = MatchesIgnorePattern | MatchesNegationPattern | NoMatch

let private getPatternsToMatch (entry : string) =
  [
    entry |> Some
    getTrailingWildcardPattern entry |> Some
    getStartingWildcardPattern entry
  ] |> List.choose id

let private toGitIgnorePattern (entry : string) =
  if isNegatingPattern entry then
    { GlobPatterns = fromNegatingPattern entry |> getPatternsToMatch; IsNegating = true }
  else 
    { GlobPatterns = entry |> getPatternsToMatch; IsNegating = false }

let private evaluateGitIgnoreMatchResult (filePath : string) (entry : GitIgnorePattern) : GitIgnoreMatchResult =
  let folder (matcher : Matcher) pattern = matcher.AddInclude(pattern)
  let matchResult = 
     entry.GlobPatterns
     |> Seq.fold folder (Matcher())
     |> Matcher.performMatch filePath

  if matchResult.HasMatches |> not then
    NoMatch 
  else if entry.IsNegating then
    MatchesNegationPattern
  else MatchesIgnorePattern

let private combineMatchResults (left : GitIgnoreMatchResult) (right : GitIgnoreMatchResult) : GitIgnoreMatchResult =
  match left, right with
  | MatchesIgnorePattern, NoMatch -> MatchesIgnorePattern
  | MatchesNegationPattern, NoMatch -> MatchesNegationPattern
  | _, MatchesIgnorePattern -> MatchesIgnorePattern
  | _, MatchesNegationPattern -> MatchesNegationPattern
  | NoMatch, NoMatch -> NoMatch

let shouldIgnoreRelativePath (GitIgnorePatterns patterns) (relativePath : string) : bool =
  let folder (matchResult : GitIgnoreMatchResult) (pattern : GitIgnorePattern) =
    evaluateGitIgnoreMatchResult relativePath pattern |> combineMatchResults matchResult

  match patterns |> Seq.fold folder NoMatch with 
  | MatchesIgnorePattern -> true
  | MatchesNegationPattern -> false
  | NoMatch -> false

let shouldIgnoreAbsolutePath (patterns : GitIgnorePatterns) (rootPath : string) (absolutePath : string) : bool =
  Path.GetRelativePath(rootPath, absolutePath) |> shouldIgnoreRelativePath patterns

let transformToGitIgnorePatterns (lines : string seq) =
    lines
    |> Seq.filter isValidGitIgnoreEntry
    |> Seq.map toGitIgnorePattern
    |> GitIgnorePatterns
