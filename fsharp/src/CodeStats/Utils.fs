namespace Utils
open Microsoft.Extensions.FileSystemGlobbing

module List =
  let split (predicate : 'a -> bool) (list : 'a list) : 'a list * 'a list =
    let folder (item : 'a) ((left, right) : 'a list * 'a list) : 'a list * 'a list =
      if predicate item then
        (left @ [item], right)
      else
        (left, right @ [item])

    List.foldBack folder list ([], [])  

module String =
  let trim (str : string) = str.Trim()
  let startsWith (value : string) (str : string) = str.StartsWith(value)

module Matcher =
  let performMatch (filePath : string) (matcher : Matcher) = 
    matcher.Match(filePath)
  
  let hasMatches (result : PatternMatchingResult) =
    result.HasMatches

