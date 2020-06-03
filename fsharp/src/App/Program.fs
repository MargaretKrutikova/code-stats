open CodeStats

[<EntryPoint>]
let main argv =
    let codeStats = CodeStats.getStats dirPath |> Async.RunSynchronously

    codeStats.FilesPerExtension 
    |> Map.toSeq 
    |> Seq.sortByDescending snd 
    |> Seq.iter (fun (key, count) -> printfn "%s - %d" key count)

    let fileSystemTree = FileSystemTree.buildFileSystemIO dirPath |> Async.RunSynchronously
    printfn "%A" fileSystemTree

    printfn "Hello World from F#!"
    0 // return an integer exit code
