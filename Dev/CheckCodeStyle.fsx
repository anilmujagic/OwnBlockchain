#r "System.IO.FileSystem"
#r "System.Text.RegularExpressions"

open System
open System.IO
open System.Text.RegularExpressions

////////////////////////////////////////////////////////////////////////////////////////////////////
// Config
////////////////////////////////////////////////////////////////////////////////////////////////////

Directory.SetCurrentDirectory __SOURCE_DIRECTORY__

let sourceDirs =
    [
        "../Source"
    ]

let patternsToSkip =
    [
        @"\/bin\/"
        @"\\bin\\"
        @"\/obj\/"
        @"\\obj\\"
        @"\/Release\/"
        @"\\Release\\"
    ]

////////////////////////////////////////////////////////////////////////////////////////////////////
// Helpers
////////////////////////////////////////////////////////////////////////////////////////////////////

type String with
    member this.IsEmpty
        with get () =
            String.IsNullOrWhiteSpace(this)
    member this.Indentation
        with get () =
            this.Length - this.TrimStart([|' '|]).Length

////////////////////////////////////////////////////////////////////////////////////////////////////
// Rules
////////////////////////////////////////////////////////////////////////////////////////////////////

type Rule = string option * string option -> string option
let createRule fn : Rule = fn

let rules =
    [
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Empty lines
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        createRule <| function
            | None, Some line when line.IsEmpty ->
                Some "There should be no empty lines at the top of the file."
            | _ -> None

        createRule <| function
            | Some line, None when line.IsEmpty ->
                Some "There should be no empty lines at the bottom of the file."
            | _ -> None

        createRule <| function
            | Some line1, Some line2 when (line1.IsEmpty) && (line2.IsEmpty) ->
                Some "There should be no multiple consecutive empty lines."
            | _ -> None

        createRule <| function
            | Some line1, Some line2 when
                line2.StartsWith("open ")
                && line1.Trim() <> ""
                && not (line1.StartsWith("open "))
                && not (line1.StartsWith("//"))
                ->
                Some "There should be an empty line before open statements."
            | _ -> None

        createRule <| function
            | Some line1, Some line2 when
                line1.StartsWith("open ")
                && line2.Trim() <> ""
                && not (line2.StartsWith("open "))
                ->
                Some "There should be an empty line after open statements."
            | _ -> None

        createRule <| function
            | Some line1, Some line2 when
                line2.Trim().StartsWith("module ")
                && not line1.IsEmpty
                && not (line1.StartsWith("[<"))
                && not (line1.StartsWith("//"))
                ->
                Some "There should be an empty line before module declaration."
            | _ -> None

        createRule <| function
            | Some line1, Some line2 when line1.Trim().StartsWith("module ") && not line2.IsEmpty ->
                Some "There should be an empty line after module declaration."
            | _ -> None

        createRule <| function
            | Some line1, Some line2 when line1.Trim().StartsWith("let ") && line2.Trim() = "=" ->
                Some "Equal sign should be on a separate line only in multiline function declaration."
            | Some line1, Some line2 when line1.Trim() = "=" && not line2.IsEmpty ->
                Some "There should be an empty line after multiline function declaration."
            | _ -> None

        createRule <| function
            | Some line1, Some line2 when line1.Trim().EndsWith("->") && line2.IsEmpty ->
                Some "There should be no empty lines after function arrow."
            | _ -> None

        createRule <| function
            | Some line1, Some line2 when
                line1.Trim().EndsWith("=")
                && line1.Trim().StartsWith("let ")
                && line2.IsEmpty
                ->
                "Code block belonging to 'let' binding or a single line function declaration "
                + "should not start with an empty line."
                |> Some
            | _ -> None

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Spaces
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        createRule <| function
            | _, Some line when line.Trim().Contains("  ") ->
                Some "There should be no multiple consecutive spaces in a line."
            | _ -> None

        createRule <| function
            | _, Some line when line.EndsWith(" ") ->
                Some "There should be no spaces at the end of a line."
            | _ -> None

        createRule <| function
            | _, Some line when line.Contains(" ,") ->
                Some "There should be no space before comma."
            | _ -> None

        createRule <| function
            | _, Some line when line.Contains(" ;") ->
                Some "There should be no space before semicolon."
            | _ -> None

        createRule <| function
            | _, Some line when line.Contains("( ") ->
                Some "There should be no space after open parentheses."
            | _ -> None

        createRule <| function
            | _, Some line when Regex.IsMatch(line, "\,\w") || Regex.IsMatch(line, "\,\(") ->
                Some "There should be a space after comma."
            | _ -> None

        createRule <| function
            | _, Some line when Regex.IsMatch(line, "\;\w") || Regex.IsMatch(line, "\;\(") ->
                Some "There should be a space after semicolon."
            | _ -> None

        createRule <| function
            | _, Some line when Regex.IsMatch(line, "[\w\)]\=") && not (Regex.IsMatch(line, "\"[^\"]+=[^\"]+\"")) ->
                Some "There should be a space before equal sign."
            | _ -> None

        createRule <| function
            | _, Some line when Regex.IsMatch(line, "\=[\w\(]") && not (Regex.IsMatch(line, "\"[^\"]+=[^\"]+\"")) ->
                Some "There should be a space after equal sign."
            | _ -> None

        createRule <| function
            | _, Some line when
                Regex.IsMatch(line, "[^\w][a-z]\w*\(")
                && not (Regex.IsMatch(line, "\"[^\"]*[^\w][a-z]\w*\([^\"]*\"")) // Except in quoted strings
                && not (Regex.IsMatch(line, "\/\/.*[^\w][a-z]\w*\(")) // Except in comments
                ->
                Some "There should be a space between the function and its first argument."
            | _ -> None

        createRule <| function
            | _, Some line when
                Regex.IsMatch(line, " \([a-zA-Z0-9]+\)")
                && not (Regex.IsMatch(line, "\"[^\"]*\([a-zA-Z0-9]+\)[^\"]*\"")) // Except in quoted strings
                && not (Regex.IsMatch(line, "\/\/.* \([a-zA-Z0-9]+\)")) // Except in comments
                ->
                Some "Simple expression should not be enclosed in parentheses."
            | _ -> None

        createRule <| function
            | _, Some line when Regex.IsMatch(line, "\w\-\>") || Regex.IsMatch(line, "\)\-\>") ->
                Some "There should be a space before function arrow."
            | _ -> None

        createRule <| function
            | _, Some line when Regex.IsMatch(line, "\-\>\w") || Regex.IsMatch(line, "\-\>\(") ->
                Some "There should be a space after function arrow."
            | _ -> None

        createRule <| function
            | _, Some line when Regex.IsMatch(line, "\w\<\-") || Regex.IsMatch(line, "\)\<\-") ->
                Some "There should be a space before '<-'."
            | _ -> None

        createRule <| function
            | _, Some line when Regex.IsMatch(line, "\<\-\w") || Regex.IsMatch(line, "\<\-\(") ->
                Some "There should be a space after '<-'."
            | _ -> None

        createRule <| function
            | _, Some line when
                (Regex.IsMatch(line, "\w\:\:") || Regex.IsMatch(line, "\)\:\:"))
                && not (Regex.IsMatch(line, "\"[^\"]*\:\:[^\"]*\"")) // Except in quoted strings
                ->
                Some "There should be a space before the cons '::' operator."
            | _ -> None

        createRule <| function
            | _, Some line when
                (Regex.IsMatch(line, "\:\:\w") || Regex.IsMatch(line, "\:\:\("))
                && not (Regex.IsMatch(line, "\"[^\"]*\:\:[^\"]*\"")) // Except in quoted strings
                ->
                Some "There should be a space after the cons '::' operator."
            | _ -> None

        createRule <| function
            | _, Some line when Regex.IsMatch(line, "(byte|int16|int|int64|decimal|string) \[\]") -> // TODO: Improve
                Some "There should be no space between type and square brackets in array declaration."
            | _ -> None

        (*
        createRule <| function
            | _, Some line when Regex.IsMatch(line, "\w\:") || Regex.IsMatch(line, "\:\w") ->
                Some "There should be a space before and after colon sign."
            | _ -> None

        createRule <| function
            | _, Some line when line.Trim().EndsWith(";") ->
                Some "There should be no semicolon at the end of the line."
            | _ -> None
        *)

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Intentation
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        createRule <| function
            | _, Some line when line.Contains("\t") ->
                Some "Don't use tabs, use four spaces instead."
            | _ -> None

        createRule <| function
            | _, Some line when (line.Indentation % 4) <> 0 ->
                Some "Use four spaces per indentation level."
            | _ -> None

        createRule <| function
            | Some line1, Some line2 when
                not (line1.IsEmpty)
                && line2.Indentation > line1.Indentation
                && line2.Indentation - line1.Indentation <> 4
                ->
                Some "Don't indent for more than one level at a time."
            | _ -> None

        createRule <| function
            | Some line1, Some line2 when
                line2.Trim().StartsWith("|>")
                && line2.Indentation > line1.Indentation
                && not (line1.Trim().StartsWith("do!"))
                ->
                Some "If a line starts with pipe operator |>, it should not be indented more than the preciding line."
            | _ -> None

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Length
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        createRule <| function
            | _, Some line when line.Length > 120 ->
                Some "Line should not be longer than 120 characters."
            | _ -> None

        createRule <| function
            | _, Some line when line.Contains("////") && line.Trim().Length <> 100 ->
                Some "Section header should have 100 slashes."
            | _ -> None

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        // Comments
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        createRule <| function
            | _, Some line when Regex.IsMatch(line, "\/\/\w") && not (Regex.IsMatch(line, "\:\/\/\w")) ->
                Some "There should be a space between comment slashes and comment text."
            | _ -> None

        (*
        createRule <| function
            | _, Some line when Regex.IsMatch(line, "\/\/ [a-z]") || Regex.IsMatch(line, "\/\/[a-z]") ->
                Some "Comment text should start with capital letter."
            | _ -> None
        *)
    ]

////////////////////////////////////////////////////////////////////////////////////////////////////
// Execution
////////////////////////////////////////////////////////////////////////////////////////////////////

let checkRules file =
    let lines =
        file
        |> File.ReadAllLines
        |> Seq.toList

    match lines with
    | [] -> []
    | l1 :: _ ->
        let lines =
            lines
            |> List.pairwise
            |> List.map (fun (l1, l2) -> Some l1, Some l2)
            |> fun pairs -> (None, Some l1) :: pairs // Add an entry for the first line
            |> List.mapi (fun i p -> i + 1, p)

        let eof =
            lines
            |> List.last
            |> fun (lineNo, (_, line)) -> (lineNo, (line, None))

        let lines = lines @ [eof]

        [
            for lineNumber, pair in lines do
                match pair with
                | _, Some l when l.EndsWith "IgnoreCodeStyle" -> ()
                | _ ->
                    for rule in rules do
                        match rule pair with
                        | Some error -> yield (file, lineNumber, error)
                        | None -> ()
        ]

let shouldSkip path =
    patternsToSkip
    |> Seq.exists (fun pattern -> Regex.IsMatch (path, pattern))

let issues =
    sourceDirs
    |> Seq.collect (fun d -> Directory.EnumerateFiles (d, "*.fs", SearchOption.AllDirectories))
    |> Seq.filter (shouldSkip >> not)
    |> Seq.map (fun file -> async { return checkRules file })
    |> Async.Parallel
    |> Async.RunSynchronously
    |> List.concat

for file, lineNumber, error in issues do
    printfn "%s (%i): %s" file lineNumber error

printfn "%i issues" issues.Length

issues.Length // Return number of issues
