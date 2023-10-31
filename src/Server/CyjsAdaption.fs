module CyjsAdaption

/// The HTML module from the real repo: https://github.com/fslaborg/Cyjs.NET/blob/main/src/Cyjs.NET/Html.fs
/// does not work in asp.net core because of loop exceptions. Therefore we need to add "JsonSerializerSettings" which determine these loops to be serialized.
/// the problem part in the repo is the following: https://github.com/fslaborg/Cyjs.NET/blob/main/src/Cyjs.NET/Html.fs#L85
module MyHTML =

    open System
    open System.IO
    open Newtonsoft.Json
    //open Cyjs.NET
    //open Cyjs.NET.CytoscapeModel

    let doc =
        let newScript = System.Text.StringBuilder()
        newScript.AppendLine("""<!DOCTYPE html>""") |> ignore
        newScript.AppendLine("<html>") |> ignore
        newScript.AppendLine("<head>") |> ignore
        newScript.AppendLine("""<script src="https://cdnjs.cloudflare.com/ajax/libs/cytoscape/3.18.0/cytoscape.min.js"></script>""") |> ignore
        newScript.AppendLine("</head>") |> ignore
        newScript.AppendLine("<body> [GRAPH] </body>") |> ignore
        newScript.AppendLine("""</html>""") |> ignore
        newScript.ToString() 

    //let graphDoc =
    //    let newScript = System.Text.StringBuilder()
    //    newScript.AppendLine("""<style>#[ID] [CANVAS] </style>""") |> ignore
    //    //newScript.AppendLine("""<style>#[ID] { width: [WIDTH]px; height: [HEIGHT]px; display: block }</style>""") |> ignore
    //    //newScript.AppendLine("""<style>#[ID] { width: 100%; height: 100%; position: absolute; top: 0px; left: 0px; }</style>""") |> ignore
    //    newScript.AppendLine("""<div id="[ID]"></div>""") |> ignore
    //    newScript.AppendLine("<script type=\"text/javascript\">") |> ignore
    //    newScript.AppendLine(@"
    //        var renderCyjs_[SCRIPTID] = function() {
    //        var fsharpCyjsRequire = requirejs.config({context:'fsharp-cyjs',paths:{cyjs:'https://cdnjs.cloudflare.com/ajax/libs/cytoscape/3.18.0/cytoscape.min'}}) || require;
    //        fsharpCyjsRequire(['cyjs'], function(Cyjs) {")  |> ignore
    //    newScript.AppendLine(@"
    //        var graphdata = [GRAPHDATA]
    //        var cy = cytoscape( graphdata );
    //        cy.userZoomingEnabled( [ZOOMING] );
    //        ")  |> ignore
    //    newScript.AppendLine("""});
    //        };
    //        if ((typeof(requirejs) !==  typeof(Function)) || (typeof(requirejs.config) !== typeof(Function))) {
    //            var script = document.createElement("script");
    //            script.setAttribute("src", "https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js");
    //            script.onload = function(){
    //                renderCyjs_[SCRIPTID]();
    //            };
    //            document.getElementsByTagName("head")[0].appendChild(script);
    //        }
    //        else {
    //            renderCyjs_[SCRIPTID]();
    //        }""") |> ignore
    //    newScript.AppendLine("</script>") |> ignore
    //    newScript.ToString() 

    ///// Converts a CyGraph to it HTML representation. The div layer has a default size of 600 if not specified otherwise.
    //let toCytoHTML (cy:Cytoscape) =
    //    let guid = Guid.NewGuid().ToString()
    //    let id   = sprintf "e%s" <| Guid.NewGuid().ToString().Replace("-","").Substring(0,10)
    //    cy.container <- PlainJsonString id

    //    let userZoomingEnabled =
    //        match cy.TryGetTypedValue<Zoom> "zoom"  with
    //        | Some z -> 
    //            match z.TryGetTypedValue<bool> "zoomingEnabled" with
    //            | Some t -> t
    //            | None -> false
    //        | None -> false
    //        |> string 
    //        |> fun s -> s.ToLower()

    //    let strCANVAS = // DynamicObj.DynObj.tryGetValue cy "Dims" //tryGetLayoutSize gChart
    //        match cy.TryGetTypedValue<Canvas> "Canvas"  with
    //        |Some c -> c
    //        |None -> Canvas.InitDefault()
    //        //|> fun c -> c?display <-  "block" ; c
    //        |> fun c -> 
    //            c.GetProperties(true)
    //            |> Seq.map (fun k -> sprintf "%s: %O" k.Key k.Value)
    //            |> String.concat "; "
    //            |> sprintf "{ %s }" 
                    
    //    DynamicObj.DynObj.remove cy "Canvas"

    //    /// Create asp.net core able settings
    //    let settings =
    //        let converter = PlainJsonStringConverter() :> JsonConverter
    //        let l = System.Collections.Immutable.ImmutableList.Create<JsonConverter>(converter)
    //        let n = new JsonSerializerSettings()
    //        n.ReferenceLoopHandling <- ReferenceLoopHandling.Serialize
    //        n.Converters <- l
    //        n

    //    let jsonGraph = JsonConvert.SerializeObject (cy,settings)

    //    let html =
    //        graphDoc
    //            .Replace("[CANVAS]", strCANVAS)
    //            .Replace("[ID]", id)
    //            .Replace("[ZOOMING]", userZoomingEnabled)
    //            .Replace("[SCRIPTID]", guid.Replace("-",""))
    //            .Replace("[GRAPHDATA]", jsonGraph)                
    //    html

    ///// Converts a CyGraph to it HTML representation and embeds it into a html page.
    //let toEmbeddedHTML (cy:Cytoscape) =
    //    let graph =
    //        toCytoHTML cy
    //    doc
    //        .Replace("[GRAPH]", graph)
    //        //.Replace("[DESCRIPTION]", "")
