module CodeStats.CodeStats

open System.IO

let isFileEntryHidden path =
  File.GetAttributes(path).HasFlag(FileAttributes.Hidden);

let rec extractFileEntries (shouldIncludeEntry) (FileSystemEntry.Directory rootPath) : FileSystemEntry.File list =
  let allEntries = 
    rootPath 
    |> Directory.EnumerateFileSystemEntries
    |> Seq.filter (isFileEntryHidden >> not)
    |> Seq.filter shouldIncludeEntry
    |> Seq.map FileSystemEntry.createFromPath
    |> Seq.choose id
    |> Seq.toList
   
  let (files, directories) = FileSystemEntry.split allEntries

  files @ (directories |> Seq.collect (extractFileEntries shouldIncludeEntry) |> Seq.toList)

let getStats (directoryPath : string) =
  async {
    let! ignorePatterns = GitIgnoreMatcher.readGitIgnorePatterns directoryPath

    let shouldIncludePath path = 
      Path.GetRelativePath(directoryPath, path) |> GitIgnoreMatcher.shouldIgnoreFile ignorePatterns |> not
    
    return extractFileEntries shouldIncludePath (FileSystemEntry.Directory directoryPath)
  }
