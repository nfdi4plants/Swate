namespace Pages

open ExcelColors
open Model
open Messages
open Feliz
open Feliz.Bulma

module private InfoHelper =
    
    let IntroductionElement =
        Bulma.field.div [
            Bulma.content [
                Html.p [
                    Html.b "Swate"
                    Html.text " is a "
                    Html.b "S"
                    Html.text "wate "
                    Html.b "W"
                    Html.text "orkflow "
                    Html.b "A"
                    Html.text "nnotation "
                    Html.b "T"
                    Html.text "ool for "
                    Html.b "E"
                    Html.text "xcel. This tool provides an easy way to annotate experimental data in an excel application that every wet lab scientist is familiar with. If you are interested check out the full "
                    Html.a [prop.href Shared.URLs.SwateWiki; prop.target.blank; prop.text "documentation"]
                    Html.text " 📚."
                ]
            ]
        ]


    let MediaContainer (content: ReactElement list) (imageSrc: string) (imageUrl: string)=
        Bulma.media [
            Bulma.content content
            Bulma.mediaRight [
                Html.a [
                    prop.href imageUrl
                    prop.target.blank
                    prop.children [
                        Bulma.image [
                            prop.className "bg-white p-2 rounded transition hover:scale-110 shadow-md hover:shadow-cyan-500/50"
                            image.is64x64
                            prop.children [
                                Html.img [prop.src imageSrc]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let GetInContactElements =
        Bulma.field.div [
            Bulma.title [
                title.is5
                prop.text "Get In Contact With Us"
            ]
            MediaContainer
                [
                    Html.strong "DataPLANT"
                    Html.br []
                    Html.p "Swate is part of the DataPLANT organisation."
                    Html.p [
                        Html.text "Services and infrastructures to support "
                        Html.a [prop.href "https://twitter.com/search?q=%23FAIRData&src=hashtag_click"; prop.target.blank; prop.text "#FAIRData" ]
                        Html.text " science and good data management practices within the plant basic research community. "
                        Html.a [prop.href "https://twitter.com/search?q=%23NFDI&src=hashtag_click"; prop.target.blank; prop.text "#NFDI" ]
                    ]

                    Html.p [
                        Html.text "Got a good idea or just want to get in touch? "
                        Html.a [prop.href Shared.URLs.Helpdesk.Url; prop.target.blank; prop.text "Reach out to us!"]
                    ]
                ]
                "https://raw.githubusercontent.com/nfdi4plants/Branding/master/logos/DataPLANT/DataPLANT_logo_minimal_rounded_bg_transparent.svg"
                Shared.URLs.NfdiWebsite

            MediaContainer
                [
                    Html.strong "X - @nfdi4plants"
                    Html.br []
                    Html.span "Follow us on X for more up-to-date information about research data management! "
                    Html.a [prop.href Shared.URLs.NFDITwitterUrl; prop.target.blank; prop.text "@nfdi4plants"]
                ]
                "/x-logo-black.png"
                Shared.URLs.NFDITwitterUrl

            MediaContainer
                [
                    Html.strong "GitHub"
                    Html.br []
                    Html.text "You can find the Swate source code  "
                    Html.a [prop.href Shared.URLs.SwateRepo; prop.target.blank; prop.text "here"]
                    Html.text ". Our developers are always happy to get in contact with you! If you don't have a GitHub account but want to reach out or want to snitch on some nasty bugs 🐛 you can tell us "
                    Html.a [prop.href Shared.URLs.Helpdesk.UrlSwateTopic; prop.target.blank; prop.text "here"]
                    Html.text "."
                ]
                "/github-mark.png"
                Shared.URLs.SwateRepo
    ]

type Info =
    static member Main =
        Html.div [
            pageHeader "Swate"
            InfoHelper.IntroductionElement
            InfoHelper.GetInContactElements
        ]