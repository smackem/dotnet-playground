module mllab.Regression

open Accord.Statistics.Models.Regression.Linear
open Accord.Math.Optimization.Losses
open Accord.Statistics.Models.Regression

type regression = {
    func: SimpleLinearRegression;
    rmse: float;
    r2: float }

let simpleLinear x y =
    let reg = SimpleLinearRegression.FromData(x, y)
    let output = reg.Transform(x)
    let rmse = SquareLoss(y, Root = true, Mean = true).Loss(output)
    let r2 = reg.CoefficientOfDetermination(x, y)
    { func = reg; rmse = rmse; r2 = r2 }
