namespace ImageLang.Lib

open Microsoft.FSharp.Text.Lexing

type internal result<'TSuccess, 'TError> =
    | Success of 'TSuccess
    | Error of 'TError


type Program internal(ast: Ast.program) =
    member this.Run(bitmap) =
        Interpreter.interpret ast bitmap


type CompilationResult internal(program : Program option, errorLine : int, errorMessage : string) =
    member this.Success =
        match program with
        | Some _ -> true
        | None -> false

    member this.Program =
        match program with
        | Some program -> program
        | None -> failwith "Compilation was not successful, no query present"

    member this.Error = (errorLine, errorMessage)


type Compiler() =
    let parse lexbuf =
        try
            Success(Parser.start Lexer.token lexbuf)
        with ex ->
            Error(lexbuf.EndPos, ex)

    member this.Compile(source : string) =
        let lexbuf = LexBuffer<char>.FromString source

        match parse lexbuf with
        | Success(ast) ->
            try
                printfn "%A" ast
                let program = new Program(ast)
                new CompilationResult(Some(program), -1, null)
            with ex ->
                new CompilationResult(None, -1, ex.Message)

        | Error(pos, ex) ->
            new CompilationResult(None, pos.Line, ex.Message)
