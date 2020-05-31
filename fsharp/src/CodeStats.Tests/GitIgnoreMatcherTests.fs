module Tests

open System
open Xunit
open CodeStats.GitIgnoreMatcher

module TestData = 
    let gitIgnoreEntries = [
        ""          // empty lines
        "   "       // white spaces
        "# comment shouldn't appear in the result"
        "" 
    ]

// https://www.atlassian.com/git/tutorials/saving-changes/gitignore

[<Fact>]
let ``Implements wildcard start directory pattern correctly`` () =
    let patterns = transformToGitIgnorePatterns ["**/logs"]
    
    let filesToIgnore = 
        ["logs/debug.log"; "logs/monday/foo.bar"; "build/logs/debug.log"] 
        |> Seq.filter (shouldIgnoreFile patterns)

    Assert.Equal(["logs/debug.log"; "logs/monday/foo.bar"; "build/logs/debug.log"], filesToIgnore)

[<Fact>]
let ``Implements wildcard start file pattern correctly`` () =
    let patterns = transformToGitIgnorePatterns ["**/logs/debug.log"]
    
    let filesToIgnore = 
        ["logs/debug.log"; "build/logs/debug.log"; "logs/build/debug.log"] 
        |> Seq.filter (shouldIgnoreFile patterns)

    Assert.Equal(["logs/debug.log"; "build/logs/debug.log"], filesToIgnore)

[<Fact>]
let ``Implements any filename wildcard pattern correctly`` () =
    let patterns = transformToGitIgnorePatterns ["*.log"]
    
    let filesToIgnore = 
        ["debug.log"; ".log"; "*.log"; "log.foo"] 
        |> Seq.filter (shouldIgnoreFile patterns)

    Assert.Equal(["debug.log"; ".log"; "*.log"], filesToIgnore)

[<Fact>]
let ``Implements exact file negating pattern correctly`` () =
    let patterns = transformToGitIgnorePatterns ["!a.log"]
    
    let shouldIgnore = shouldIgnoreFile patterns "a.log"
    Assert.False(shouldIgnore)

[<Fact>]
let ``Implements wildcard and negating patterns together correctly`` () =
    let patterns = transformToGitIgnorePatterns ["*.log"; "!important/*.log"]
    
    let filesToIgnore = 
        ["debug.log"; "trace.log"; "important.log"; "logs/important.log"] 
        |> Seq.filter (shouldIgnoreFile patterns)

    Assert.Equal(["debug.log"; "trace.log"; "important.log"], filesToIgnore)

[<Fact>]
let ``patterns after a negating pattern re-ignore negated files`` () =
    let patterns = transformToGitIgnorePatterns ["*.log"; "!important/*.log"; "trace.*"]
    
    let filesToIgnore = 
        ["debug.log"; "important/trace.log"; "important/debug.log"] 
        |> Seq.filter (shouldIgnoreFile patterns)

    Assert.Equal(["debug.log"; "important/trace.log"], filesToIgnore)
