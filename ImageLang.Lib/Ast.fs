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
    | Log of expr list
    | Blt of expr

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
    | In of expr * expr
    | Neg of expr
    | Not of expr
    | MemberInvoke of expr * string
    | At of expr
    | Pos of expr * expr
    | Ident of string
    | Number of double
    | String of string
    | Boolean of bool
    | FunctionInvoke of string * expr list
    | Kernel of expr list
    | Rect of expr * expr
    | Conditional of expr * expr * expr
