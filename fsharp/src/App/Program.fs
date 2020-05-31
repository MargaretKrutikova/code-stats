open CodeStats

[<EntryPoint>]
let main argv =
    let dirPath = ""
    let lines = CodeStats.getStats dirPath |> Async.RunSynchronously
    lines |> Seq.iter (printfn "%A") 

    printfn "Hello World from F#!"
    0 // return an integer exit code
