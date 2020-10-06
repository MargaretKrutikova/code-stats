module Tests

open Xunit
open CodeStats.GitIgnoreMatcher

// https://www.atlassian.com/git/tutorials/saving-changes/gitignore

[<Fact>]
let ``Transform patterns removes empty lines and comments`` () =
    let (GitIgnorePatterns patterns) = transformToGitIgnorePatterns [""; "    "; "# some comment here"]

    Assert.Equal(0, patterns |> Seq.length)

[<Fact>]
let ``Matches rules relative to current directory correctly`` () =
    let patterns = transformToGitIgnorePatterns ["trace.*"]
    
    let shouldIgnore = shouldIgnoreRelativePath patterns "important/trace.log"
    Assert.True(shouldIgnore)

[<Fact>]
let ``Implements wildcard start directory pattern correctly`` () =
    let patterns = transformToGitIgnorePatterns ["**/logs"]
    
    let filesToIgnore = 
        ["logs/debug.log"; "logs/monday/foo.bar"; "build/logs/debug.log"] 
        |> Seq.filter (shouldIgnoreRelativePath patterns)

    Assert.Equal(["logs/debug.log"; "logs/monday/foo.bar"; "build/logs/debug.log"], filesToIgnore)

[<Fact>]
let ``Implements wildcard start file pattern correctly`` () =
    let patterns = transformToGitIgnorePatterns ["**/logs/debug.log"]
    
    let filesToIgnore = 
        ["logs/debug.log"; "build/logs/debug.log"; "logs/build/debug.log"] 
        |> Seq.filter (shouldIgnoreRelativePath patterns)

    Assert.Equal(["logs/debug.log"; "build/logs/debug.log"], filesToIgnore)

[<Fact>]
let ``Implements any filename wildcard pattern correctly`` () =
    let patterns = transformToGitIgnorePatterns ["*.log"]
    
    let filesToIgnore = 
        ["debug.log"; ".log"; "*.log"; "log.foo"] 
        |> Seq.filter (shouldIgnoreRelativePath patterns)

    Assert.Equal(["debug.log"; ".log"; "*.log"], filesToIgnore)

[<Fact>]
let ``Implements wildcard and negating patterns together correctly`` () =
    let patterns = transformToGitIgnorePatterns ["*.log"; "!important.log"]
    
    let filesToIgnore = 
        ["debug.log"; "trace.log"; "important.log"; "logs/important.log"] 
        |> Seq.filter (shouldIgnoreRelativePath patterns)

    Assert.Equal(["debug.log"; "trace.log";], filesToIgnore)

[<Fact>]
let ``Patterns after a negating pattern re-ignore negated files`` () =
    let patterns = transformToGitIgnorePatterns ["*.log"; "!important/*.log"; "trace.*"]
    
    let filesToIgnore = 
        ["debug.log"; "important/trace.log"; "important/debug.log"] 
        |> Seq.filter (shouldIgnoreRelativePath patterns)

    Assert.Equal(["debug.log"; "important/trace.log"], filesToIgnore)

[<Fact>]
let ``Prepending a slash matches files only in the repository root`` () =
    let patterns = transformToGitIgnorePatterns ["/debug.log"]
    
    let filesToIgnore = 
        ["debug.log"; "logs/debug.log"] 
        |> Seq.filter (shouldIgnoreRelativePath patterns)

    Assert.Equal(["debug.log"], filesToIgnore)

[<Fact>]
let ``By default, patterns match files in any directory`` () =
    let patterns = transformToGitIgnorePatterns ["debug.log"]
    
    let filesToIgnore = 
        ["debug.log"; "logs/debug.log"] 
        |> Seq.filter (shouldIgnoreRelativePath patterns)

    Assert.Equal(["debug.log";"logs/debug.log"], filesToIgnore)
