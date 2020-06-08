module CodeStats.FileSystemTree

open System.IO
open FSharp.Control
open GitIgnoreMatcher

type FileSystemTree = {
  Entry : FileSystemEntry.FileSystemEntry
  Children : FileSystemTree seq
}

let private isFileEntryHidden path =
  File.GetAttributes(path).HasFlag(FileAttributes.Hidden);

let private getGitIgnorePath (FileSystemEntry.Directory dirPath) : string =
  Path.Combine [| dirPath; ".gitignore" |]

let private readGitIgnoreIO (dirEntry : FileSystemEntry.Directory) : Async<GitIgnorePatterns> =
  async {
    let gitignorePath = getGitIgnorePath dirEntry
  
    if not <| File.Exists gitignorePath then
      return Seq.empty |> GitIgnorePatterns
    else
      let! entriesToIgnore = File.ReadAllLinesAsync gitignorePath |> Async.AwaitTask
      return entriesToIgnore |> transformToGitIgnorePatterns
  }

let rec private traverseDirectory 
  (readGitIgnore : FileSystemEntry.Directory -> Async<GitIgnorePatterns>)
  (createFromPath : string -> FileSystemEntry.FileSystemEntry option)
  (shouldIgnore : string -> bool)
  (dirEntry : FileSystemEntry.Directory) : Async<FileSystemTree list> =
  async {
    let! ignorePatterns = readGitIgnore dirEntry

    let (FileSystemEntry.Directory dirPath) = dirEntry
    let currentShouldIgnore = shouldIgnoreAbsolutePath ignorePatterns dirPath
    let shouldIgnore entry = shouldIgnore entry || currentShouldIgnore entry

    let createTree = createFileSystemTree shouldIgnore readGitIgnore createFromPath
    
    let tree = 
      dirPath 
        |> Directory.EnumerateFileSystemEntries
        |> Seq.filter (shouldIgnore >> not)
        |> Seq.map createFromPath
        |> Seq.choose id
        |> AsyncSeq.ofSeq
        |> AsyncSeq.mapAsync createTree
        |> AsyncSeq.toListAsync

    return! tree
  }
and createFileSystemTree
  (shouldIgnore : string -> bool)
  (readFolderGitIgnorePatters : FileSystemEntry.Directory -> Async<GitIgnorePatterns>)
  (createFromPath : string -> FileSystemEntry.FileSystemEntry option)
  (entry : FileSystemEntry.FileSystemEntry) : Async<FileSystemTree>
 = match entry with
    | FileSystemEntry.FileEntry _ as file -> 
      async { return { Entry = file; Children = Seq.empty } }
    | FileSystemEntry.DirectoryEntry directory as directoryEntry -> 
      async {
        let! children = traverseDirectory readFolderGitIgnorePatters createFromPath shouldIgnore directory
        return { Entry = directoryEntry; Children = children }
      }

let buildFileSystemTree 
  (isFileHidden : string -> bool)
  (readFolderGitIgnorePatters : FileSystemEntry.Directory -> Async<GitIgnorePatterns>)
  (createFromPath : string -> FileSystemEntry.FileSystemEntry option)
  (directoryPath : string) : Async<FileSystemTree option> =
  async {
    match createFromPath directoryPath with
    | Some directory -> 
      let! tree = createFileSystemTree isFileHidden readFolderGitIgnorePatters createFromPath directory
      return Some tree
    | None -> return None
  }

let buildFileSystemIO (directoryPath : string) : Async<FileSystemTree option> =
  buildFileSystemTree isFileEntryHidden readGitIgnoreIO FileSystemEntry.createFromPathIO directoryPath
