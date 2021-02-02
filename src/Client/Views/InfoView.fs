module InfoView

open Fable.React
open Fable.React.Props
open Fulma
open ExcelColors
open Model
open Messages
open Browser
open Browser.MediaQueryList
open Browser.MediaQueryListExtensions

open CustomComponents

open Fulma.Extensions.Wikiki
open Fable.FontAwesome

let swateHeader model dispatch =
    div [Style [Width "100%"; TextAlign TextAlignOptions.Center]][
        Heading.h3 [
            Heading.Option.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]
        ][
            str "SWATE"
        ]
    ]

let introductionElement model dispatch =
    div [Style [Color model.SiteStyleState.ColorMode.Text; Margin "0 auto"; MaxWidth "80%"]][
        div [Class "myflexText"][
            b [][str "SWATE"]
            str " is a "
            b [][str "S"]
            str "wate "
            b [][str "W"]
            str "orkflow "
            b [][str "A"]
            str "nnotation "
            b [][str "T"]
            str "ool for "
            b [][str "E"]
            str "xcel."
        ]
        div [Class "myflexText"][
            str "This tool provides an easy way to annotate experimental data in an excel application that every wet lab scientist is familiar with. If you are interested check out the full documentation "
            a [Href Shared.URLs.DocsFeatureUrl; Target "_blank"][str "here"]
            str "."
        ]
    ]



let getInContactElement (model:Model) dispatch =
    [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent; MarginLeft "7%"]]] [str "Get In Contact With Us"]

        Columns.columns [Columns.Props [Style [MaxWidth "80%"; Margin "0 auto"]]][
            Column.column [][
                Heading.h5 [Heading.IsSubtitle; Heading.Props [Style [Color model.SiteStyleState.ColorMode.Text]]][
                    str "Swate is part of the dataPLANT organisation."
                ]
                Content.content [][
                    blockquote [][
                        str "Services and infrastructures to support "
                        a [Href "https://twitter.com/search?q=%23FAIRData&src=hashtag_click"][ str "#FAIRData" ]
                        str " science and good data management practices within the plant basic research community. "
                        a [Href "https://twitter.com/search?q=%23NFDI&src=hashtag_click"] [ str "#NFDI" ]
                        str "."
                    ]
                ]
            ]
            Column.column [Column.Width (Screen.All, Column.IsNarrow); Column.CustomClass "flexToCentered"][
                a [Href "https://nfdi4plants.de/"; Target "_Blank"; Class "nfdiIcon" ] [
                    Fulma.Image.image [][
                        img [Src "https://raw.githubusercontent.com/nfdi4plants/Branding/138420e3b6f9ec9e125c1ca8840874b2be2a1262/logos/DataPLANT_logo_minimal_square_bg_darkblue.svg"]
                    ]
                ]
            ]
        ]

        Columns.columns [Columns.Props [Style [MaxWidth "80%"; Margin "0 auto"]]][
            Column.column [][
                Heading.h5 [Heading.IsSubtitle; Heading.Props [Style [Color model.SiteStyleState.ColorMode.Text]]][
                    str "Got a good idea or just want to get in touch? Reach out to us! "
                    div [] [a [Href Shared.URLs.NFDITwitterUrl; Target "_Blank"][str "#Twitter @nfdi4plants"]]
                ]
            ]
            Column.column [Column.Width (Screen.All, Column.IsNarrow); Column.CustomClass "flexToCentered"][
                a [Href Shared.URLs.NFDITwitterUrl; Target "_Blank"][
                    Fa.i [
                        Fa.Size Fa.Fa4x
                        Fa.Brand.Twitter
                        Fa.CustomClass "myFaBrand myFaTwitter"
                    ][]
                ]
            ]
        ]

        //Columns.columns [
        //    Columns.Props [Style [MaxWidth "80%"; Margin "0 auto"]]
        //    Columns.CustomClass "reverseCols"
        //][
        //    Column.column [
        //        Column.Width (Screen.All, Column.IsNarrow)
        //        Column.CustomClass "flexToCentered"
        //    ][
        //        a [
        //            Href Shared.URLs.CSBWebsiteUrl; Target "_Blank"
        //        ][
        //            Fa.i [
        //                Fa.CustomClass "myFaBrand myFaCSB"
        //            ][
        //                str "CSB"
        //            ]
        //        ]
        //    ]
        //    Column.column [][
        //        Heading.h5 [Heading.IsSubtitle; Heading.Props [Style [Color model.SiteStyleState.ColorMode.Text]]][
        //            str "If you are interested in our other work, check out our "
        //            b [][str "website"]
        //            str "!"
        //        ]
        //    ]
        //]
        
        Columns.columns [Columns.Props [Style [MaxWidth "80%"; Margin "0 auto"]]][
            Column.column [][
                Heading.h5 [Heading.IsSubtitle; Heading.Props [Style [Color model.SiteStyleState.ColorMode.Text]]][
                    str "Should you find any bugs please make sure to report them, by opening a "
                    b [][str "GitHub issue"]
                    str "!"
        
                ]
            ]
            Column.column [Column.Width (Screen.All, Column.IsNarrow); Column.CustomClass "flexToCentered"][
                a [Href "https://github.com/nfdi4plants/Swate/issues"; Target "_Blank"][
                    Fa.i [
                        Fa.Brand.Github
                        Fa.CustomClass "myFaBrand myFaGithub"
                    ][]
                ]
            ]
        ]
    ]

let infoComponent (model : Model) (dispatch : Msg -> unit) =
    div [][
        // Header
        swateHeader model dispatch

        br []

        introductionElement model dispatch

        br []

        // Get In Contact element
        
        yield! getInContactElement model dispatch

        // Get In Contact element
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent; MarginLeft "7%"]]][ str "Api Docs"]

        div [Style [Color model.SiteStyleState.ColorMode.Text; Margin "0 auto"; MaxWidth "80%"]][
            div [Class "myflexText"][
                str "To make it easier for developers to start working with Swate, we provide a documentation of our APIs. These can be viewed  "
                a [Href Shared.URLs.DocsApiUrl; Target "_Blank"][str "here"]
                str " and "
                a [Href Shared.URLs.DocsApiUrl2; Target "_Blank"][str "here"]
                str "."
            ]
        ]
    ]