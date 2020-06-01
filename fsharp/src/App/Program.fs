open CodeStats

[<EntryPoint>]
let main argv =
    let codeStats = CodeStats.getStats dirPath |> Async.RunSynchronously

    codeStats.FilesPerExtension 
    |> Map.toSeq 
    |> Seq.sortByDescending snd 
    |> Seq.iter (fun (key, count) -> printfn "%s - %d" key count)

    printfn "Hello World from F#!"
    0 // return an integer exit code
