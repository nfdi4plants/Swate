module API.Helper

open Fable.Remoting.Server
open Microsoft.AspNetCore.Http

let errorHandler (ex:exn) (routeInfo:RouteInfo<HttpContext>) =
    let msg = sprintf "%A %s @%s." ex.Message System.Environment.NewLine routeInfo.path
    Propagate msg