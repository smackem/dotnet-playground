module internal ImageLang.Lib.Interpreter

open Ast
open System.Collections.Generic
open System.IO
open System.Text
open Helpers


type value =
    | Number of double
    | Boolean of bool
    | String of string
    | Pos of double * double
    | Kernel of int * int * double array // radius,length,values
    | Color of ColorArgb
    | Rect of double * double * double * double // x,y,width,height
    member this.AssertNumber caller =
        match this with
        | Number n -> n
        | _ -> failwithf "%A expects Number but found '%A'" caller this
    member this.AssertBoolean caller =
        match this with
        | Boolean b -> b
        | _ -> failwithf "%A expects Boolean but found '%A'" caller this
    member this.AssertPos caller =
        match this with
        | Pos(x, y) -> x, y
        | _ -> failwithf "%A expects Pos but found '%A'" caller this
    member this.AssertKernel caller =
        match this with
        | Kernel(radius, length, ns) -> radius, length, ns
        | _ -> failwithf "%A expects Kernel but found '%A'" caller this
    member this.AssertColor caller =
        match this with
        | Color argb -> argb
        | _ -> failwithf "%A expects Color but found '%A'" caller this
    member this.AssertRect caller =
        match this with
        | Rect(x, y, width, height) -> x, y, width, height
        | _ -> failwithf "%A expects Rect but found '%A'" caller this
    member this.InvokeProperty memberName =
        match this with
        | Color argb ->
            match memberName with
            | "r" -> Number <| float argb.R
            | "g" -> Number <| float argb.G
            | "b" -> Number <| float argb.B
            | "a" -> Number <| float argb.A
            | "i" -> Number <| argb.Intensity
            | "scr" -> Number <| argb.ScR
            | "scg" -> Number <| argb.ScG
            | "scb" -> Number <| argb.ScB
            | "sca" -> Number <| argb.ScA
            | "sci" -> Number <| argb.ScIntensity
            | _ -> failwithf "Unknown %A member %s" this memberName
        | Pos(x, y) ->
            match memberName with
            | "x" -> Number x
            | "y" -> Number y
            | _ -> failwithf "Unknown %A member %s" this memberName
        | Rect(x, y, w, h) ->
            match memberName with
            | "x" -> Number x
            | "y" -> Number y
            | "w" -> Number w
            | "h" -> Number h
            | "left" -> Number x
            | "top" -> Number y
            | "right" -> Number (x + w)
            | "bottom" -> Number (y + h)
            | _ -> failwithf "Unknown %A member %s" this memberName
        | _ -> failwithf "Unknown %A member %s" this memberName
    member this.Print() =
        match this with
        | Number d -> d.ToString()
        | Boolean b -> b.ToString()
        | String s -> s
        | Color argb -> argb.ToString()
        | _ -> sprintf "'%A'" this

let lastRectIdent = "<@last_rect>"

type Context(bitmap: IBitmap, parent: Context option) =
    let idents = new Dictionary<string, value>()
    member this.GetIdent ident =
        let ok, v = idents.TryGetValue(ident)
        if ok then Some v
        else
            match parent with
            | Some ctx -> ctx.GetIdent ident
            | None -> None
    member this.SetIdent ident value =
        if this.HasLocalIdent ident then idents.[ident] <- value
        else
            match parent with
            | Some ctx -> ctx.SetIdent ident value
            | None -> ()
    member this.NewIdent ident value = idents.[ident] <- value
    member this.HasLocalIdent ident = idents.ContainsKey ident
    member this.Bitmap = bitmap


let walkRect x y width height =
    let nx, ny, nw, nh = toi x, toi y, toi width, toi height
    seq {
        for y in ny .. (ny + nh) do
            for x in nx .. (nx + nw) do
                yield Pos(float x, float y) }


let rec interpret program bitmap =
    let ctx = new Context(bitmap, None)
    ctx.NewIdent lastRectIdent (Rect(0.0, 0.0, 0.0, 0.0))
    ctx.NewIdent "W" (Number <| float bitmap.Width)
    ctx.NewIdent "H" (Number <| float bitmap.Height)
    ctx.NewIdent "IMAGE" (Rect(0.0, 0.0, float bitmap.Width, float bitmap.Height))
    ctx.NewIdent "BLACK" (Color <| ColorArgb.FromArgb(255uy, 0uy, 0uy, 0uy))
    ctx.NewIdent "WHITE" (Color <| ColorArgb.FromArgb(255uy, 255uy, 255uy, 255uy))
    ctx.NewIdent "TRANSPARENT" (Color <| ColorArgb.FromArgb(0uy, 255uy, 255uy, 255uy))
    match program with
    | Statements stmts -> evalStmtList stmts ctx

and evalStmtList stmts ctx =
    let ctx = new Context(ctx.Bitmap, Some ctx)
    stmts |> List.iter (fun stmt -> evalStmt stmt ctx)

and evalStmt stmt ctx =
    match stmt with
    | Declaration(ident, rexpr) ->
        if ctx.HasLocalIdent ident
        then failwithf "Identifier %s is already declared" ident
        else
            let v = evalExpr rexpr ctx
            ctx.NewIdent ident v
    | Assignment(ident, rexpr) ->
        match ctx.GetIdent ident with
        | None -> failwithf "Identifier %s not found" ident
        | Some _ ->
            let v = evalExpr rexpr ctx
            ctx.SetIdent ident v
    | PixelAssign(posExpr, rexpr) ->
        let pos = evalExpr posExpr ctx
        let color = evalExpr rexpr ctx
        match pos, color with
        | Pos(x, y), Color(argb) -> ctx.Bitmap.SetPixel(int(x + 0.5), int(y + 0.5), argb)
        | _ -> failwithf "Pixel assignment expects POS <- COLOR but found '%A' <- '%A'!" pos color
    | IfThen(expr, stmts) ->
        match evalExpr expr ctx with
        | Boolean true -> evalStmtList stmts ctx
        | Boolean false -> ()
        | other -> failwithf "If expects a boolean expression but found '%A'" other
    | IfThenElse(expr, ifStmts, elseStmts) ->
        match evalExpr expr ctx with
        | Boolean true -> evalStmtList ifStmts ctx
        | Boolean false -> evalStmtList elseStmts ctx
        | other -> failwithf "If expects a boolean expression but found '%A'" other
    | ForIn(ident, collExpr, stmts) ->
        let rect, positions =
            match evalExpr collExpr ctx with
            | Rect(x, y, width, height) as rect -> rect, walkRect x y width height
            | other -> failwithf "ForIn expects a rect expression but found '%A'" other
        for pos in positions do
            ctx.NewIdent ident pos
            evalStmtList stmts ctx
        ctx.SetIdent lastRectIdent rect
    | ForInRange(ident, lowerExpr, upperExpr, stmts) ->
        let lower, upper =
            match evalExpr lowerExpr ctx, evalExpr upperExpr ctx with
            | Number l, Number u -> toi l, toi u
            | l, u -> failwithf "ForInRange expects a lower Number and an upper Number but found '%A' and '%A" l u
        for n in lower .. upper do
            ctx.NewIdent ident (Number <| float n)
            evalStmtList stmts ctx
    | Yield expr -> ()
    | Log exprs ->
        let buffer = new StringBuilder()
        exprs |> List.iter (fun expr ->
            let v = evalExpr expr ctx
            buffer.Append(v.Print()).Append(" ") |> ignore)
        printfn "%s" <| buffer.ToString()
    | Blt exprOpt ->
        let x,y,w,h =
            match exprOpt with
            | Some expr -> (evalExpr expr ctx).AssertRect("Blt")
            | None -> match ctx.GetIdent(lastRectIdent) with Some(r) -> r.AssertRect("Blt") | None -> failwithf "%s not found" lastRectIdent
        ctx.Bitmap.Blt(toi x, toi y, toi w, toi h)

and evalExpr expr ctx =
    match expr with
    | Conditional(condExpr, ifExpr, elseExpr) ->
        if (evalExpr condExpr ctx).AssertBoolean("Conditional")
        then evalExpr ifExpr ctx
        else evalExpr elseExpr ctx
    | Or(lexpr, rexpr) ->
        ((evalExpr lexpr ctx).AssertBoolean "Or" || (evalExpr rexpr ctx).AssertBoolean "Or") |> Boolean
    | And(lexpr, rexpr) ->
        ((evalExpr lexpr ctx).AssertBoolean "And" && (evalExpr rexpr ctx).AssertBoolean "And") |> Boolean
    | Eq(lexpr, rexpr) ->
        (evalExpr lexpr ctx).Equals(evalExpr rexpr ctx) |> Boolean
    | Neq(lexpr, rexpr) ->
        (evalExpr lexpr ctx).Equals(evalExpr rexpr ctx) |> not |> Boolean
    | Gt(lexpr, rexpr) ->
        ((evalExpr lexpr ctx).AssertNumber ">" > (evalExpr rexpr ctx).AssertNumber "Gt") |> Boolean
    | Ge(lexpr, rexpr) ->
        ((evalExpr lexpr ctx).AssertNumber ">=" >= (evalExpr rexpr ctx).AssertNumber "Ge") |> Boolean
    | Lt(lexpr, rexpr) ->
        ((evalExpr lexpr ctx).AssertNumber "<" < (evalExpr rexpr ctx).AssertNumber "<") |> Boolean
    | Le(lexpr, rexpr) ->
        ((evalExpr lexpr ctx).AssertNumber "<=" <= (evalExpr rexpr ctx).AssertNumber "<=") |> Boolean
    | Plus(lexpr, rexpr) ->
        match evalExpr lexpr ctx, evalExpr rexpr ctx with
        | Number l, Number r -> Number (l + r)
        | Number n, Color c ->
            Color <| ColorArgb.FromArgb(c.A, clampFloat(float c.R + n), clampFloat(float c.G + n), clampFloat(float c.B + n))
        | Color c, Number n ->
            Color <| ColorArgb.FromArgb(c.A, clampFloat(float c.R + n), clampFloat(float c.G + n), clampFloat(float c.B + n))
        | Color l, Color r ->
            Color <| ColorArgb.FromArgb(l.A, clampInt(int l.R + int r.R), clampInt(int l.G + int r.G), clampInt(int l.B + int r.B))
        | l, r -> failwithf "+ expects colors or numbers but found %A and %A" l r
    | Minus(lexpr, rexpr) ->
        match evalExpr lexpr ctx, evalExpr rexpr ctx with
        | Number l, Number r -> Number (l - r)
        | Color c, Number n ->
            Color <| ColorArgb.FromArgb(c.A, clampFloat(float c.R - n), clampFloat(float c.G - n), clampFloat(float c.B - n))
        | Color l, Color r ->
            Color <| ColorArgb.FromArgb(l.A, clampInt(int l.R - int r.R), clampInt(int l.G - int r.G), clampInt(int l.B - int r.B))
        | l, r -> failwithf "- expects colors or numbers but found %A and %A" l r
    | Mul(lexpr, rexpr) ->
        match evalExpr lexpr ctx, evalExpr rexpr ctx with
        | Number l, Number r -> Number (l * r)
        | Number n, Color c ->
            Color <| ColorArgb.FromSargb(c.ScA, c.ScR * n, c.ScG * n, c.ScB * n)
        | Color c, Number n ->
            Color <| ColorArgb.FromSargb(c.ScA, c.ScR * n, c.ScG * n, c.ScB * n)
        | Color l, Color r ->
            Color <| ColorArgb.FromSargb(l.ScA, l.ScR * r.ScR, l.ScG * r.ScG, l.ScB * r.ScB)
        | l, r -> failwithf "* expects colors or numbers but found %A and %A" l r
    | Div(lexpr, rexpr) ->
        match evalExpr lexpr ctx, evalExpr rexpr ctx with
        | Number l, Number r -> Number (l / r)
        | Color c, Number n ->
            Color <| ColorArgb.FromSargb(c.ScA, c.ScR / n, c.ScG / n, c.ScB / n)
        | Color l, Color r ->
            Color <| ColorArgb.FromSargb(l.ScA, l.ScR / r.ScR, l.ScG / r.ScG, l.ScB / r.ScB)
        | l, r -> failwithf "/ expects colors or numbers but found %A and %A" l r
    | Mod(lexpr, rexpr) ->
        match evalExpr lexpr ctx, evalExpr rexpr ctx with
        | Number l, Number r -> Number (l % r)
        | Color c, Number n ->
            Color <| ColorArgb.FromArgb(c.A, clampFloat(float c.R % n), clampFloat(float c.G % n), clampFloat(float c.B % n))
        | Color l, Color r ->
            Color <| ColorArgb.FromArgb(l.A, l.R % r.R, l.G % r.G, l.B % r.B)
        | l, r -> failwithf "%% expects colors or numbers but found %A and %A" l r
    | In(lexpr, rexpr) ->
        match evalExpr lexpr ctx, evalExpr rexpr ctx with
        | Pos(x, y), Rect(rx, ry, rw, rh) -> Boolean (x >= rx && x < rx + rw && y >= ry && y < ry + rh)
        | l, r -> failwithf "In expects POS in RECT but found %A and %A" l r
    | Neg expr ->
        match evalExpr expr ctx with
        | Number n -> Number -n
        | Color argb -> Color <| ColorArgb.FromArgb(argb.A, 255uy - argb.R, 255uy - argb.G, 255uy - argb.B)
        | other -> failwithf "Negation expected Number or Color but found %A" other
    | Not expr ->
        (evalExpr expr ctx).AssertBoolean "Not" |> not |> Boolean
    | MemberInvoke(recvExpr, memberName) ->
        let recv = evalExpr recvExpr ctx
        recv.InvokeProperty memberName
    | At posExpr ->
        let x, y = (evalExpr posExpr ctx).AssertPos "@"
        ctx.Bitmap.GetPixel(toi x, toi y) |> Color
    | Ident name ->
        match ctx.GetIdent name with
        | Some v -> v
        | None -> failwithf "Identifier '%s' not found" name
    | expr.Number num ->
        Number num
    | expr.String str ->
        String str
    | expr.Pos(xexpr, yexpr) ->
        ((evalExpr xexpr ctx).AssertNumber ",", (evalExpr yexpr ctx).AssertNumber ",") |> Pos
    | expr.Boolean b ->
        Boolean b
    | FunctionInvoke(ident, exprs) ->
        invokeFunc ident exprs ctx
    | expr.Kernel(exprs) ->
        createKernel exprs ctx
    | expr.Rect(xyposExpr, whposExpr) ->
        let x,y = (evalExpr xyposExpr ctx).AssertPos "Rect"
        let w,h = (evalExpr whposExpr ctx).AssertPos "Rect"
        Rect(x, y, w, h)

and invokeFunc ident exprs ctx =
    match ident with
    | "rgb" ->
        match exprs with
        | rexpr :: gexpr :: bexpr :: [] ->
            Color <| ColorArgb.FromArgb(
                        255uy,
                        (evalExpr rexpr ctx).AssertNumber "red" |> clampFloat,
                        (evalExpr gexpr ctx).AssertNumber "green" |> clampFloat,
                        (evalExpr bexpr ctx).AssertNumber "blue" |> clampFloat)
        | _ -> failwith "rgb needs three parameters"
    | "rgba" ->
        match exprs with
        | rexpr :: gexpr :: bexpr :: aexpr :: [] ->
            Color <| ColorArgb.FromArgb(
                        (evalExpr aexpr ctx).AssertNumber "alpha" |> clampFloat,
                        (evalExpr rexpr ctx).AssertNumber "red" |> clampFloat,
                        (evalExpr gexpr ctx).AssertNumber "green" |> clampFloat,
                        (evalExpr bexpr ctx).AssertNumber "blue" |> clampFloat)
        | _ -> failwith "rgba needs four parameters"
    | "srgb" ->
        match exprs with
        | rexpr :: gexpr :: bexpr :: [] ->
            Color <| ColorArgb.FromArgb(
                        255uy,
                        (evalExpr rexpr ctx).AssertNumber "red" |> toSColorByte,
                        (evalExpr gexpr ctx).AssertNumber "green" |> toSColorByte,
                        (evalExpr bexpr ctx).AssertNumber "blue" |> toSColorByte)
        | _ -> failwith "srgb needs four parameters"
    | "srgba" ->
        match exprs with
        | rexpr :: gexpr :: bexpr :: aexpr :: [] ->
            Color <| ColorArgb.FromArgb(
                        (evalExpr aexpr ctx).AssertNumber "alpha" |> toSColorByte,
                        (evalExpr rexpr ctx).AssertNumber "red" |> toSColorByte,
                        (evalExpr gexpr ctx).AssertNumber "green" |> toSColorByte,
                        (evalExpr bexpr ctx).AssertNumber "blue" |> toSColorByte)
        | _ -> failwith "rgba needs four parameters"
    | "kernel" ->
        createKernel exprs ctx
    | "convolute" ->
        match exprs with
        | posExpr :: kernelExpr :: [] ->
            match evalExpr kernelExpr ctx, evalExpr posExpr ctx with
            | Kernel(length, radius, values), Pos(x, y) ->
                ctx.Bitmap.Convolute(int x, int y, int radius, int length, values)
                |> Color
            | l, r -> failwithf "%A %A" l r
        | _ -> failwith "convolute needs 2 parameters"
    | "rect" ->
        match exprs with
        | xyposExpr :: whposExpr :: [] ->
            let x,y = (evalExpr xyposExpr ctx).AssertPos "Rect"
            let w,h = (evalExpr whposExpr ctx).AssertPos "Rect"
            Rect(x, y, w, h)
        | _ -> failwith "rect needs 2 parameters"
    | "sin" ->
        match exprs with
        | expr :: [] -> (evalExpr expr ctx).AssertNumber("Sin") |> sin |> Number
        | _ -> failwith "sin needs 1 parameter"
    | "cos" ->
        match exprs with
        | expr :: [] -> (evalExpr expr ctx).AssertNumber("Cos") |> cos |> Number
        | _ -> failwith "sin needs 1 parameter"
    | "tan" ->
        match exprs with
        | expr :: [] -> (evalExpr expr ctx).AssertNumber("Tan") |> tan |> Number
        | _ -> failwith "tan needs 1 parameter"
    | "asin" ->
        match exprs with
        | expr :: [] -> (evalExpr expr ctx).AssertNumber("Asin") |> asin |> Number
        | _ -> failwith "asin needs 1 parameter"
    | "acos" ->
        match exprs with
        | expr :: [] -> (evalExpr expr ctx).AssertNumber("Acos") |> acos |> Number
        | _ -> failwith "acos needs 1 parameter"
    | "atan" ->
        match exprs with
        | expr :: [] -> (evalExpr expr ctx).AssertNumber("Atan") |> atan |> Number
        | _ -> failwith "atan needs 1 parameter"
    | "atan2" ->
        match exprs with
        | aexpr :: bexpr:: [] ->
            let a = (evalExpr aexpr ctx).AssertNumber("Atan2")
            let b = (evalExpr bexpr ctx).AssertNumber("Atan2")
            Number <| System.Math.Atan2(a, b)
        | _ -> failwith "atan2 needs 2 parameters"
    | "abs" ->
        match exprs with
        | expr :: [] -> (evalExpr expr ctx).AssertNumber("Abs") |> abs |> Number
        | _ -> failwith "abs needs 1 parameter"
    | "sqrt" ->
        match exprs with
        | expr :: [] ->
            match evalExpr expr ctx with
            | Number n -> Number <| sqrt n
            | Color argb -> Color <| ColorArgb.FromSargb(argb.ScA, sqrt argb.ScR, sqrt argb.ScG, sqrt argb.ScB)
            | other -> failwithf "Sqrt expected Number or Color but found %A" other
        | _ -> failwith "sqrt needs 1 parameter"
    | other -> failwithf "Unkown function '%s'" other

and createKernel exprs ctx =
    let length = sqrt <| float exprs.Length
    if round(length) <> length
    then failwithf "The number of kernel elements must be quadratic, but is '%A'" length
    else
        let values =
            exprs
            |> List.map (fun expr -> (evalExpr expr ctx).AssertNumber "Kernel")
            |> List.toArray
        Kernel(int length, (int length) / 2, values)

