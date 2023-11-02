module InfoView

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Browser
open Browser.MediaQueryList
open Browser.MediaQueryListExtensions

open CustomComponents

open Fable
open Fable.Core
open Feliz
open Feliz.Bulma

let introductionElement model dispatch =
    p [Style [TextAlign TextAlignOptions.Justify]] [
        b [] [str "Swate"]
        str " is a "
        b [] [str "S"]
        str "wate "
        b [] [str "W"]
        str "orkflow "
        b [] [str "A"]
        str "nnotation "
        b [] [str "T"]
        str "ool for "
        b [] [str "E"]
        str "xcel. This tool provides an easy way to annotate experimental data in an excel application that every wet lab scientist is familiar with. If you are interested check out the full "
        a [Href Shared.URLs.SwateWiki; Target "_blank"] [str "documentation"]
        str " 📚."
    ]


let iconContainer (left: ReactElement list) (icon) =
    Bulma.field.div [
        prop.className "is-flex"
        prop.children [
            Html.div [
                prop.style [style.marginRight(length.rem 2)]
                prop.children left
            ]
            icon
        ]
    ]

let getInContactElement (model:Model) dispatch =
    Bulma.content [
        prop.style [style.textAlign.justify]
        prop.children [
            Bulma.label "Get In Contact With Us"

            p [] [str "Swate is part of the DataPLANT organisation."]
            p [] [
                a [Href "https://nfdi4plants.de/"; Target "_Blank"; Title "DataPLANT"; Class "nfdiIcon"; Style [Float FloatOptions.Right; MarginLeft "2em"]] [
                    img [Src "https://raw.githubusercontent.com/nfdi4plants/Branding/138420e3b6f9ec9e125c1ca8840874b2be2a1262/logos/DataPLANT_logo_minimal_square_bg_darkblue.svg"; Style [Width "54px"]]
                ]
                str "Services and infrastructures to support "
                a [Href "https://twitter.com/search?q=%23FAIRData&src=hashtag_click"] [ str "#FAIRData" ]
                str " science and good data management practices within the plant basic research community. "
                a [Href "https://twitter.com/search?q=%23NFDI&src=hashtag_click"] [ str "#NFDI" ]
            ]

            p [] [
                str "Got a good idea or just want to get in touch? "
                a [Href Shared.URLs.Helpdesk.Url;Target "_Blank"] [str "Reach out to us!"]
            ]

            iconContainer
                ([
                    Html.span "Follow us on Twitter for the more up-to-date information about research data management! "
                    a [Href Shared.URLs.NFDITwitterUrl; Target "_Blank";] [str "@nfdi4plants"]
                ])
                (Bulma.icon [
                    prop.href Shared.URLs.NFDITwitterUrl;
                    prop.target "_Blank";
                    prop.title "@nfdi4plants on Twitter"
                    Bulma.icon.isLarge;
                    Html.i [prop.classes ["fa-brands fa-twitter"; "myFaBrand myFaTwitter"; "is-size-3"]]
                    |> prop.children
                ])

            iconContainer
                ([
                    str "You can find the Swate source code  "
                    a [Href Shared.URLs.SwateRepo; Target "_Blank"] [str "here"]
                    str ". Our developers are always happy to get in contact with you! If you don't have a GitHub account but want to reach out or want to snitch on some nasty bugs 🐛 you can tell us "
                    a [Href Shared.URLs.Helpdesk.UrlSwateTopic; Target "_Blank"] [str "here"]
                    str "."
                ])
                (Bulma.icon [
                    prop.href Shared.URLs.SwateRepo;
                    prop.target "_Blank";
                    prop.title "Swate on GitHub"
                    Bulma.icon.isLarge;
                    Html.i [prop.classes ["fa-brands fa-github"; "myFaBrand myFaGithub"; "is-size-3"]]
                    |> prop.children
                ])                
        ]
    ]

let infoComponent (model : Model) (dispatch : Msg -> unit) =
    Bulma.content [
        pageHeader "Swate"
        Bulma.field.div [
            introductionElement model dispatch
        ]
        Bulma.field.div [
            div [] [
                Bulma.label "Documentation"

                ul [] [
                    li [] [p [] [ a [Href Shared.URLs.SwateWiki; Target "_blank"] [ str "User documentation"] ] ]
                    li [] [p [] [
                        str "OpenApi docs for "
                        a [Href (Shared.URLs.Docs.OntologyApi Shared.URLs.Docs.Html); Target "_blank"] [ str "IOntologyAPI"]
                        str "." ]
                    ]
                ]
            ]
        ]
        Bulma.field.div [
            getInContactElement model dispatch
        ]
    ]