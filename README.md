# code-stats

A small command line utility for generating some simple statistics for any codebase.
Currently only counts numbers of files for each file extension.

## Technologies

.NET Core, `F#`, `xunit`

## Why?

This is for me to play with `F#` and some cool functional concepts (like <mark>TRAVERSE</mark>), understand `Async` a bit deeper, learn how to write some unit tests better etc.

## Structure

There are three projects:

- the core logic (`CodeStats`), includes implementing gitignore-pattern on top of file globbing to exclude ignored files from a directory, and calculating number of files per each unique file extension,
- unit tests (`CodeStats.Tests`), testing pure logic of the core project,
- console app (`App`) to read path to a directory, call the stats functions and print them in the console.
