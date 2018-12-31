namespace ImageLang.Lib

open System.Runtime.InteropServices

#nowarn "9" // explicit struct layout might lead to inverifiable IL code

module internal Helpers =
    let clampToByte f =
        match f with
        | f when f < 0.0 -> 0uy
        | f when f > 255.0 -> 255uy
        | _ -> byte (f + 0.5)

open Helpers

[<Struct>]
[<StructLayoutAttribute(LayoutKind.Explicit)>]
type ColorArgb =
    [<DefaultValue>]
    [<FieldOffset(0)>]
    val mutable public B : byte

    [<DefaultValue>]
    [<FieldOffset(1)>]
    val mutable public G : byte

    [<DefaultValue>]
    [<FieldOffset(2)>]
    val mutable public R : byte

    [<DefaultValue>]
    [<FieldOffset(3)>]
    val mutable public A : byte

    [<DefaultValue>]
    [<FieldOffset(0)>]
    val mutable public Argb : int

    static member public FromArgb(a, r, g, b) =
        new ColorArgb(A = a, R = r, G = g, B = b)

    static member public FromArgb(argb) =
        new ColorArgb(Argb = argb)

    static member public FromArgb(scA, scR, scG, scB) =
        new ColorArgb(A = clampToByte(scA * 255.0), R = clampToByte(scR * 255.0), G = clampToByte(scG * 255.0), B = clampToByte(scB * 255.0))

    member this.ScA = (float this.A) / 255.0
    member this.ScR = (float this.R) / 255.0
    member this.ScG = (float this.G) / 255.0
    member this.ScB = (float this.B) / 255.0
    member this.Intensity = (0.299 * (float this.R) + 0.587 * (float this.G) + 0.114 * (float this.B)) * (float this.A) / 255.0;
    member this.ScIntensity = (0.299 * (float this.R) + 0.587 * (float this.G) + 0.114 * (float this.B)) * (float this.A) / (255.0 * 255.0);


type IBitmap =
    abstract member GetPixel : int * int -> ColorArgb
    abstract member SetPixel : int * int * ColorArgb -> unit
    abstract member Width : int
    abstract member Height : int
