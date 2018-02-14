// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

#r "../packages/Accord.3.8.0/lib/net462/Accord.dll"
#r "../packages/Accord.Math.3.8.0/lib/net462/Accord.Math.Core.dll"
#r "../packages/Accord.Math.3.8.0/lib/net462/Accord.Math.dll"
#r "../packages/Accord.Statistics.3.8.0/lib/net462/Accord.Statistics.dll"

#load "Regression.fs"

open Accord.Statistics.Models.Regression.Linear
open Accord.Math.Optimization.Losses
open mllab

let x = [| 0.0; 1.0; 2.0; 3.0; 4.0; 5.0; 6.0; 6.0; 7.0; 8.0; 9.0; 10.0; 11.0; 12.0; 13.0 |]
let y = [| 0.0; 1.0; 2.0; 3.0; 5.0; 5.0; 6.0; 7.0; 8.0; 20.0; 9.0; 10.0; 11.0; 13.0; 15.0 |]

let reg = Regression.simpleLinear x y

let quality =
    let meanY = y |> Seq.average
    let rmseRatio = reg.rmse / meanY
    (1.0 - rmseRatio) * reg.r2 * 100.0
sprintf "%.2f%%" quality
