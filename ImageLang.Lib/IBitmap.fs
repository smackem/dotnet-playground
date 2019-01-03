namespace ImageLang.Lib

open System.Runtime.InteropServices

#nowarn "9" // explicit struct layout might lead to inverifiable IL code

module internal Helpers =
    let clampFloat f =
        match f with
        | f when f < 0.0 -> 0uy
        | f when f > 255.0 -> 255uy
        | _ -> byte (f + 0.5)

    let clampInt n =
        match n with
        | n when n < 0 -> 0uy
        | n when n > 255 -> 255uy
        | _ -> byte n

    let toi f = int (f + 0.5)

    let toSColorByte f =
        if f > 0.0 && f <= 1.0
        then byte(f * 255.0 + 0.5)
        else clampFloat f

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

    static member public FromSargb(scA, scR, scG, scB) =
        new ColorArgb(A = clampFloat(scA * 255.0), R = clampFloat(scR * 255.0), G = clampFloat(scG * 255.0), B = clampFloat(scB * 255.0))

    member this.ScA = (float this.A) / 255.0
    member this.ScR = (float this.R) / 255.0
    member this.ScG = (float this.G) / 255.0
    member this.ScB = (float this.B) / 255.0
    member this.Intensity = (0.299 * (float this.R) + 0.587 * (float this.G) + 0.114 * (float this.B)) * (float this.A) / 255.0;
    member this.ScIntensity = (0.299 * (float this.R) + 0.587 * (float this.G) + 0.114 * (float this.B)) * (float this.A) / (255.0 * 255.0);

    override this.ToString() = sprintf "#%02x%02x%02x@%02x" this.R this.G this.B this.A


type IBitmap =
    abstract member GetPixel : x:int * y:int -> ColorArgb
    abstract member SetPixel : x:int * y:int * argb:ColorArgb -> unit
    abstract member Width : int
    abstract member Height : int
    abstract member Convolute : x:int * y:int * radius:int * length:int * kernel:double array -> ColorArgb
    abstract member Blt : x:int * y:int * width:int * height:int -> unit
