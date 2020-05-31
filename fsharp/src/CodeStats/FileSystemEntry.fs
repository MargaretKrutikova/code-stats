module CodeStats.FileSystemEntry

open System.IO

type FileSystemEntry =
  | DirectoryEntry of Directory
  | FileEntry of File
and Directory = Directory of directoryPath: string
and File = File of filePath: string

let createFromPath (path : string) : FileSystemEntry option =
  if path |> Directory.Exists then
    Directory path |> DirectoryEntry |> Some
  else if path |> File.Exists then
    File path |> FileEntry |> Some
  else 
    None

let rec split (entries : FileSystemEntry list) =
  let folder entry (files, directories) =
    match entry with
    | FileEntry file -> files @ [file], directories
    | DirectoryEntry directory -> files, directories @ [directory]

  List.foldBack folder entries ([], [])  
  
