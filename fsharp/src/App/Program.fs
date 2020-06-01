open CodeStats

[<EntryPoint>]
let main argv =
    let codeStats = CodeStats.getStats dirPath |> Async.RunSynchronously

    Map.iter (printfn "%s - %d") codeStats.FilesPerExtension

    printfn "Hello World from F#!"
    0 // return an integer exit code
