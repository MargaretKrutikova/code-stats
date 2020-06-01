module CodeStats.CodeStats

open System.IO
open System
open FSharp.Control
open GitIgnoreMatcher

type FilesPerExtension = Map<string, int>

type CodeStats = {
  FilesPerExtension : FilesPerExtension
}

let private isFileEntryHidden path =
  File.GetAttributes(path).HasFlag(FileAttributes.Hidden);

let getGitIgnorePath (directoryPath : string) : string =
  Path.Combine [| directoryPath; ".gitignore" |]

let readGitIgnorePatterns (folder : string) : Async<GitIgnorePatterns> =
  async {
    let gitignorePath = getGitIgnorePath folder
  
    if not <| File.Exists gitignorePath then
      return Seq.empty |> GitIgnorePatterns
    else
      let! entriesToIgnore = File.ReadAllLinesAsync gitignorePath |> Async.AwaitTask
      return entriesToIgnore |> transformToGitIgnorePatterns
  }

let rec private extractFileEntries (parentShouldKeep : string -> bool) (FileSystemEntry.Directory rootPath) : Async<FileSystemEntry.File list> =
  async {
    let! ignorePatterns = readGitIgnorePatterns rootPath
    let shouldKeepEntry = shouldIgnoreAbsolutePath ignorePatterns rootPath >> not
    
    let combined entry = parentShouldKeep entry && shouldKeepEntry entry

    let allEntries = 
      rootPath 
      |> Directory.EnumerateFileSystemEntries
      |> Seq.filter (isFileEntryHidden >> not)
      |> Seq.filter combined
      |> Seq.map FileSystemEntry.createFromPath
      |> Seq.choose id
      |> Seq.toList
     
    let (files, directories) = FileSystemEntry.split allEntries

    let! innerFiles = 
      directories 
      |> AsyncSeq.ofSeq
      |> AsyncSeq.mapAsync (extractFileEntries combined)
      |> AsyncSeq.concatSeq
      |> AsyncSeq.toListAsync

    return files @ innerFiles
  }

let getStats (directoryPath : string) : Async<CodeStats> =
  async {
    let! nonIgnoredFiles = extractFileEntries (fun _ -> true) (FileSystemEntry.Directory directoryPath)

    let filesPerExtension =
      nonIgnoredFiles 
      |> Seq.map (fun (FileSystemEntry.File path) -> Path.GetExtension(path))
      |> Seq.filter (String.IsNullOrWhiteSpace >> not)
      |> Seq.groupBy (id)
      |> Seq.map (fun (key, files) -> key, Seq.length files)
      |> Map.ofSeq

    return { FilesPerExtension = filesPerExtension }
  }
