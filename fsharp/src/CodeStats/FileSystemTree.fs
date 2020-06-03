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

let private getGitIgnorePath (directoryPath : string) : string =
  Path.Combine [| directoryPath; ".gitignore" |]

let private readGitIgnorePatterns (folder : string) : Async<GitIgnorePatterns> =
  async {
    let gitignorePath = getGitIgnorePath folder
  
    if not <| File.Exists gitignorePath then
      return Seq.empty |> GitIgnorePatterns
    else
      let! entriesToIgnore = File.ReadAllLinesAsync gitignorePath |> Async.AwaitTask
      return entriesToIgnore |> transformToGitIgnorePatterns
  }

let rec private buildDirectoryChildren 
  (isFileHidden : string -> bool)
  (readFolderGitIgnorePatters : string -> Async<GitIgnorePatterns>)
  (createFromPath : string -> FileSystemEntry.FileSystemEntry option)
  (parentShouldIgnore : string -> bool)
  (FileSystemEntry.Directory rootPath) : Async<FileSystemTree list> =
  async {
    let! ignorePatterns = readFolderGitIgnorePatters rootPath
    let currentShouldIgnore = shouldIgnoreAbsolutePath ignorePatterns rootPath
    let shouldIgnore entry = parentShouldIgnore entry || currentShouldIgnore entry

    let createTree = createFileSystemTree shouldIgnore isFileHidden readFolderGitIgnorePatters createFromPath
    
    let tree = 
      rootPath 
        |> Directory.EnumerateFileSystemEntries
        |> Seq.filter (fun entry -> (isFileHidden entry || shouldIgnore entry) |> not)
        |> Seq.map createFromPath
        |> Seq.choose id
        |> AsyncSeq.ofSeq
        |> AsyncSeq.mapAsync createTree
        |> AsyncSeq.toListAsync

    return! tree
  }
and createFileSystemTree
  (parentShouldIgnore : string -> bool)
  (isFileHidden : string -> bool)
  (readFolderGitIgnorePatters : string -> Async<GitIgnorePatterns>)
  (createFromPath : string -> FileSystemEntry.FileSystemEntry option)
  (entry : FileSystemEntry.FileSystemEntry) : Async<FileSystemTree>
 = match entry with
    | FileSystemEntry.FileEntry _ as file -> 
      async { return { Entry = file; Children = Seq.empty } }
    | FileSystemEntry.DirectoryEntry directory as directoryEntry -> 
      async {
        let! children = buildDirectoryChildren isFileHidden readFolderGitIgnorePatters createFromPath parentShouldIgnore directory
        return { Entry = directoryEntry; Children = children }
      }

let buildFileSystemTree 
  (isFileHidden : string -> bool)
  (readFolderGitIgnorePatters : string -> Async<GitIgnorePatterns>)
  (createFromPath : string -> FileSystemEntry.FileSystemEntry option)
  (directoryPath : string) : Async<FileSystemTree option> =
  async {
    match createFromPath directoryPath with
    | Some directory -> 
      let! tree = createFileSystemTree (fun _ -> false) isFileHidden readFolderGitIgnorePatters createFromPath directory
      return Some tree
    | None -> return None
  }

let buildFileSystemIO (directoryPath : string) : Async<FileSystemTree option> =
  buildFileSystemTree isFileEntryHidden readGitIgnorePatterns FileSystemEntry.createFromPathIO directoryPath
