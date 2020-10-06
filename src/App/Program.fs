open CodeStats
open System
open System.IO

type CommandLineOptions = {
    Path: string
}

type CmdOptionsValidationError = InvalidCmdArgs | PathEmpty | PathNotFound

let parseArgs args: Result<CommandLineOptions, CmdOptionsValidationError> =
    match args with
    | [path] -> 
        if String.IsNullOrWhiteSpace path then
            Error PathEmpty
        else if Directory.Exists path |> not then 
            Error PathNotFound
        else 
            { Path = path } |> Ok
    | _ -> Error InvalidCmdArgs

let printError error: unit =
    match error with
    | InvalidCmdArgs -> 
        printfn "Incorrect arguments supplied"
    | PathEmpty ->
        printfn "Path cannot be empty"
    | PathNotFound ->
        printfn "Path is not found on disk"

[<EntryPoint>]
let main argv =
    let options = argv |> Seq.toList |> parseArgs
    match options with
    | Error error ->
        printError error 
        42
    | Ok { Path = path } ->
        let codeStats = CodeStats.getStats path |> Async.RunSynchronously

        codeStats.FilesPerExtension 
        |> Map.toSeq 
        |> Seq.sortByDescending snd 
        |> Seq.iter (fun (key, count) -> printfn "%s - %d" key count)

        0
