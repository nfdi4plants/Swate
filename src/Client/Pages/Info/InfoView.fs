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

open Fable
open Fable.Core

let swateHeader model dispatch =
    Heading.h3 [
        Heading.Option.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]
    ][
        str "SWATE"
    ]

let introductionElement model dispatch =
    p [Style [Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Justify]][
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
        str "xcel. This tool provides an easy way to annotate experimental data in an excel application that every wet lab scientist is familiar with. If you are interested check out the full "
        a [Href Shared.URLs.SwateWiki; Target "_blank"][str "documentation"]
        str " 📚."
    ]


let getInContactElement (model:Model) dispatch =
    Content.content [Content.Props [Style [Color model.SiteStyleState.ColorMode.Text; TextAlign TextAlignOptions.Justify]]] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Get In Contact With Us"]

        p [][
            h5 [] [str "Swate is part of the DataPLANT organisation."]
            a [Href "https://nfdi4plants.de/"; Target "_Blank"; Title "DataPLANT"; Class "nfdiIcon"; Style [Float FloatOptions.Right; MarginLeft "2em"]][
                img [Src "https://raw.githubusercontent.com/nfdi4plants/Branding/138420e3b6f9ec9e125c1ca8840874b2be2a1262/logos/DataPLANT_logo_minimal_square_bg_darkblue.svg"; Style [Width "54px"]]
            ]
            str "Services and infrastructures to support "
            a [Href "https://twitter.com/search?q=%23FAIRData&src=hashtag_click"][ str "#FAIRData" ]
            str " science and good data management practices within the plant basic research community. "
            a [Href "https://twitter.com/search?q=%23NFDI&src=hashtag_click"] [ str "#NFDI" ]
        ]

        p [Style [MarginBottom "2.5em"]][
            str "Got a good idea or just want to get in touch? "
            a [Href Shared.URLs.Helpdesk.Url;Target "_Blank"][str "Reach out to us!"]
        ]

        p [Style [MarginBottom "2.5em"]][
            a [Href Shared.URLs.NFDITwitterUrl; Target "_Blank"; Style [Float FloatOptions.Right; MarginLeft "2em"]; Title "@nfdi4plants on Twitter"][
                Fa.i [
                    Fa.Size Fa.Fa2x
                    Fa.Brand.Twitter
                    Fa.CustomClass "myFaBrand myFaTwitter"
                ][]
            ]
            str "Follow us on Twitter for the more up-to-date information about research data management! "
            a [Href Shared.URLs.NFDITwitterUrl; Target "_Blank";] [str "@nfdi4plants"]
        ]   

        p [][
            a [Href Shared.URLs.SwateRepo; Target "_Blank"; Style [Float FloatOptions.Right; MarginLeft "2em"]; Title "Swate on GitHub"][
                Fa.i [
                    Fa.Size Fa.Fa2x
                    Fa.Brand.Github
                    Fa.CustomClass "myFaBrand myFaGithub"
                ][]
            ]
            str "You can find the Swate source code  "
            a [Href Shared.URLs.SwateRepo; Target "_Blank"][str "here"]
            str ". Our developers are always happy to get in contact with you! If you don't have a GitHub account but want to reach out or want to snitch on some nasty bugs 🐛 you can tell us "
            a [Href Shared.URLs.Helpdesk.UrlSwateTopic; Target "_Blank"][str "here"]
            str "."
        ]
    ]

let infoComponent (model : Model) (dispatch : Msg -> unit) =
    Content.content [ ] [
        Field.div [][
            swateHeader model dispatch
        ]
        Field.div [][
            introductionElement model dispatch
        ]
        Field.div [][
            getInContactElement model dispatch
        ]
    ]