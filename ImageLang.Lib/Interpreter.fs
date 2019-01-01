module internal ImageLang.Lib.Interpreter

open Ast
open System.Collections.Generic


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


type Context(bitmap: IBitmap, parent: Context option) =
    let idents = new Dictionary<string, value>()
    member this.GetIdent ident =
        let ok, v = idents.TryGetValue(ident)
        if ok then Some v
        else
            match parent with
            | Some ctx -> ctx.GetIdent ident
            | None -> None
    member this.SetIdent ident value = idents.[ident] <- value
    member this.HasLocalIdent ident = idents.ContainsKey ident
    member this.Bitmap = bitmap


let toi f = int (f + 0.5)

let toColorByte f =
    if f > 0.0 && f <= 1.0
    then byte(f * 255.0 + 0.5)
    else byte(f + 0.5)

let walkRect x y width height =
    let nx, ny, nw, nh = toi x, toi y, toi width, toi height
    seq {
        for y in ny .. (ny + nh) do
            for x in nx .. (nx + nw) do
                yield Pos(float x, float y) }


let rec interpret program bitmap =
    let ctx = new Context(bitmap, None)
    ctx.SetIdent "W" (Number <| float bitmap.Width)
    ctx.SetIdent "H" (Number <| float bitmap.Height)
    ctx.SetIdent "IMAGE" (Rect(0.0, 0.0, float bitmap.Width, float bitmap.Height))
    ctx.SetIdent "BLACK" (Color <| ColorArgb.FromArgb(255uy, 0uy, 0uy, 0uy))
    ctx.SetIdent "WHITE" (Color <| ColorArgb.FromArgb(255uy, 255uy, 255uy, 255uy))
    ctx.SetIdent "TRANSPARENT" (Color <| ColorArgb.FromArgb(0uy, 255uy, 255uy, 255uy))
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
            ctx.SetIdent ident v
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
        let positions =
            match evalExpr collExpr ctx with
            | Rect(x, y, width, height) -> walkRect x y width height
            | other -> failwithf "ForIn expects a rect expression but found '%A'" other
        for pos in positions do
            ctx.SetIdent ident pos
            evalStmtList stmts ctx
    | ForInRange(ident, lowerExpr, upperExpr, stmts) ->
        let lower, upper =
            match evalExpr lowerExpr ctx, evalExpr upperExpr ctx with
            | Number l, Number u -> toi l, toi u
            | l, u -> failwithf "ForInRange expects a lower Number and an upper Number but found '%A' and '%A" l u
        for n in lower .. upper do
            ctx.SetIdent ident (Number <| float n)
            evalStmtList stmts ctx
    | Yield(expr) -> ()

and evalExpr expr ctx =
    match expr with
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
            Color <| ColorArgb.FromArgb(c.ScA, c.ScR + n, c.ScG + n, c.ScB + n)
        | Color c, Number n ->
            Color <| ColorArgb.FromArgb(c.ScA, c.ScR + n, c.ScG + n, c.ScB + n)
        | Color l, Color r ->
            Color <| ColorArgb.FromArgb(l.ScA + r.ScA, l.ScR + r.ScR, l.ScG + r.ScG, l.ScB + r.ScB)
        | l, r -> failwithf "+ expects colors or numbers but found %A and %A" l r
    | Minus(lexpr, rexpr) ->
        match evalExpr lexpr ctx, evalExpr rexpr ctx with
        | Number l, Number r -> Number (l - r)
        | Color c, Number n ->
            Color <| ColorArgb.FromArgb(c.ScA, c.ScR - n, c.ScG - n, c.ScB - n)
        | Color l, Color r ->
            Color <| ColorArgb.FromArgb(l.ScA - r.ScA, l.ScR - r.ScR, l.ScG - r.ScG, l.ScB - r.ScB)
        | l, r -> failwithf "- expects colors or numbers but found %A and %A" l r
    | Mul(lexpr, rexpr) ->
        match evalExpr lexpr ctx, evalExpr rexpr ctx with
        | Number l, Number r -> Number (l * r)
        | Number n, Color c ->
            Color <| ColorArgb.FromArgb(c.ScA, c.ScR * n, c.ScG * n, c.ScB * n)
        | Color c, Number n ->
            Color <| ColorArgb.FromArgb(c.ScA, c.ScR * n, c.ScG * n, c.ScB * n)
        | Color l, Color r ->
            Color <| ColorArgb.FromArgb(l.ScA, l.ScR * r.ScR, l.ScG * r.ScG, l.ScB * r.ScB)
        | l, r -> failwithf "* expects colors or numbers but found %A and %A" l r
    | Div(lexpr, rexpr) ->
        match evalExpr lexpr ctx, evalExpr rexpr ctx with
        | Number l, Number r -> Number (l / r)
        | Color c, Number n ->
            Color <| ColorArgb.FromArgb(c.ScA, c.ScR / n, c.ScG / n, c.ScB / n)
        | Color l, Color r ->
            Color <| ColorArgb.FromArgb(l.ScA, l.ScR / r.ScR, l.ScG / r.ScG, l.ScB / r.ScB)
        | l, r -> failwithf "/ expects colors or numbers but found %A and %A" l r
    | Mod(lexpr, rexpr) ->
        match evalExpr lexpr ctx, evalExpr rexpr ctx with
        | Number l, Number r -> Number (l % r)
        | Color c, Number n ->
            Color <| ColorArgb.FromArgb(c.ScA, c.ScR % n, c.ScG % n, c.ScB % n)
        | Color l, Color r ->
            Color <| ColorArgb.FromArgb(l.ScA, l.ScR % r.ScR, l.ScG % r.ScG, l.ScB % r.ScB)
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
        (evalExpr expr ctx).AssertBoolean "%" |> not |> Boolean
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
    | ColorRgb(rexpr, gexpr, bexpr) ->
        Color <| ColorArgb.FromArgb(
                    255uy,
                    (evalExpr rexpr ctx).AssertNumber "red" |> toColorByte,
                    (evalExpr gexpr ctx).AssertNumber "green" |> toColorByte,
                    (evalExpr bexpr ctx).AssertNumber "blue" |> toColorByte)
    | ColorRgba(rexpr, gexpr, bexpr, aexpr) ->
        Color <| ColorArgb.FromArgb(
                    (evalExpr aexpr ctx).AssertNumber "alpha" |> toColorByte,
                    (evalExpr rexpr ctx).AssertNumber "red" |> toColorByte,
                    (evalExpr gexpr ctx).AssertNumber "green" |> toColorByte,
                    (evalExpr bexpr ctx).AssertNumber "blue" |> toColorByte)
    | expr.Kernel(exprs) ->
        let length = sqrt <| float exprs.Length
        if round(length) <> length
        then failwithf "The number of kernel elements must be quadratic, but is '%A'" length
        else
            let values =
                exprs
                |> List.map (fun expr -> (evalExpr expr ctx).AssertNumber "Kernel")
                |> List.toArray
            Kernel(int length, (int length) / 2, values)
    | expr.Rect(xyposExpr, whposExpr) ->
        let x,y = (evalExpr xyposExpr ctx).AssertPos "Rect"
        let w,h = (evalExpr whposExpr ctx).AssertPos "Rect"
        Rect(x, y, w, h)
    | Convolute(kernelExpr, posExpr) ->
        match evalExpr kernelExpr ctx, evalExpr posExpr ctx with
        | Kernel(length, radius, values), Pos(x, y) ->
            ctx.Bitmap.Convolute(int x, int y, int radius, int length, values)
            |> Color
        | l, r -> failwithf "%A %A" l r
