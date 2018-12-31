module internal ImageLang.Lib.Ast

type program =
    | Statements of statement list

and statement =
    | Declaration of string * expr
    | Assignment of string * expr
    | PixelAssign of expr * expr
    | IfThen of expr * statement list
    | IfThenElse of expr * statement list * statement list
    | ForIn of string * expr * statement list
    | ForInRange of string * expr * expr * statement list
    | Yield of expr

and expr =
    | Or of expr * expr
    | And of expr * expr
    | Eq of expr * expr
    | Neq of expr * expr
    | Gt of expr * expr
    | Ge of expr * expr
    | Lt of expr * expr
    | Le of expr * expr
    | Plus of expr * expr
    | Minus of expr * expr
    | Mul of expr * expr
    | Div of expr * expr
    | Mod of expr * expr
    | Neg of expr
    | Not of expr
    | MemberInvoke of expr * string
    | At of expr
    | Pos of expr * expr
    | Ident of string
    | Number of double
    | String of string
    | Boolean of bool
    | ColorRgb of expr * expr * expr
    | ColorRgba of expr * expr * expr * expr
    | Kernel of expr list
    | Convolute of expr * expr
    | Rect of expr * expr
