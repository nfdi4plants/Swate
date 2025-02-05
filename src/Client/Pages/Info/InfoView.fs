namespace Pages

open Model
open Messages
open Feliz
open Feliz.DaisyUI

module private AboutHelper =

    let IntroductionElement =
        Html.div [

            prop.children [
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
                    Html.text "veryone. This tool provides an easy way to annotate experimental data in an excel application that every wet lab scientist is familiar with. If you are interested check out the full "
                    Html.a [prop.href Shared.URLs.SWATE_WIKI; prop.target.blank; prop.text "documentation"]
                    Html.text " 📚."
                ]
            ]
        ]


    let MediaContainer (content: ReactElement, imageSrc: string, imageHref: string)=
        Html.div [
            prop.className "hero"
            prop.children [
                Html.div [
                    prop.className "hero-content flex-col"
                    prop.children [
                        Html.div [
                            prop.className "prose"
                            prop.children content
                        ]
                        Html.div [
                            prop.className "not-prose"
                            prop.children [
                                Daisy.button.a [
                                    prop.href imageHref
                                    button.square
                                    button.primary
                                    button.lg
                                    prop.className [

                                    ]
                                    prop.children [
                                        Html.img [prop.src imageSrc; prop.className "size-12"]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let GetInContactElements =
        React.fragment [
            MediaContainer (
                React.fragment [
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
                ],
                "https://raw.githubusercontent.com/nfdi4plants/Branding/refs/heads/master/logos/DataPLANT/DataPLANT_logo_minimal_rounded_bg_black.svg",
                Shared.URLs.NfdiWebsite
            )

            MediaContainer (
                React.fragment [
                    Html.strong "X - @nfdi4plants"
                    Html.br []
                    Html.span "Follow us on X for more up-to-date information about research data management! "
                    Html.a [prop.href Shared.URLs.NFDITwitterUrl; prop.target.blank; prop.text "@nfdi4plants"]
                ],
                "/x-logo-black.png",
                Shared.URLs.NFDITwitterUrl
            )

            MediaContainer (
                React.fragment [
                    Html.strong "GitHub"
                    Html.br []
                    Html.text "You can find the Swate source code  "
                    Html.a [prop.href Shared.URLs.SwateRepo; prop.target.blank; prop.text "here"]
                    Html.text ". Our developers are always happy to get in contact with you! If you don't have a GitHub account but want to reach out or want to snitch on some nasty bugs 🐛 you can tell us "
                    Html.a [prop.href Shared.URLs.Helpdesk.UrlSwateTopic; prop.target.blank; prop.text "here"]
                    Html.text "."
                ],
                "/github-mark.png",
                Shared.URLs.SwateRepo
            )
    ]

type About =
    static member Main =
        Html.div [
            prop.className "prose-sm md:prose lg:prose-lg divide-y-2 gap-y-2 py-1 lg:py-4"
            prop.children [
                Html.h1 "Swate"
                AboutHelper.IntroductionElement
                AboutHelper.GetInContactElements
            ]
        ]