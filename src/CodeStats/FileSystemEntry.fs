module CodeStats.FileSystemEntry

open System.IO

type FileSystemEntry =
  | DirectoryEntry of Directory
  | FileEntry of File
and Directory = Directory of directoryPath: string
and File = File of filePath: string

let createFromPath 
  (fileExists : string -> bool)
  (directoryExists : string -> bool) 
  (path : string) : FileSystemEntry option =
  if path |> directoryExists then
    Directory path |> DirectoryEntry |> Some
  else if path |> fileExists then
    File path |> FileEntry |> Some
  else 
    None

let createFromPathIO (path : string) : FileSystemEntry option =
  createFromPath File.Exists Directory.Exists path

let rec split (entries : FileSystemEntry list) =
  let folder entry (files, directories) =
    match entry with
    | FileEntry file -> file :: files, directories
    | DirectoryEntry directory -> files, directory :: directories

  List.foldBack folder entries ([], [])  
  
