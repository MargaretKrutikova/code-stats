open CodeStats

type CommandLineOptions = {
    Path: string
}

let parseArgs args: CommandLineOptions option =
    match args with
    | [path] -> { Path = path } |> Some
    | _ -> None

[<EntryPoint>]
let main argv =
    let options = argv |> Seq.toList |> parseArgs
    match options with
    | None ->
        printfn "Incorrect arguments supplied"
        42
    | Some { Path = path } ->
        let codeStats = CodeStats.getStats path |> Async.RunSynchronously

        codeStats.FilesPerExtension 
        |> Map.toSeq 
        |> Seq.sortByDescending snd 
        |> Seq.iter (fun (key, count) -> printfn "%s - %d" key count)

        0
