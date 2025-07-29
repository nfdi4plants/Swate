namespace Swate.Components

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type Icons =

    static member BuildingBlock() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 15 15"><path fill="none" stroke="currentColor" d="M7.5 4v7M4 7.5h7m-3.5 7a7 7 0 1 1 0-14a7 7 0 0 1 0 14Z" stroke-width="1"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><g fill="none"><path d="m12.593 23.258l-.011.002l-.071.035l-.02.004l-.014-.004l-.071-.035q-.016-.005-.024.005l-.004.01l-.017.428l.005.02l.01.013l.104.074l.015.004l.012-.004l.104-.074l.012-.016l.004-.017l-.017-.427q-.004-.016-.017-.018m.265-.113l-.013.002l-.185.093l-.01.01l-.003.011l.018.43l.005.012l.008.007l.201.093q.019.005.029-.008l.004-.014l-.034-.614q-.005-.018-.02-.022m-.715.002a.02.02 0 0 0-.027.006l-.006.014l-.034.614q.001.018.017.024l.015-.002l.201-.093l.01-.008l.004-.011l.017-.43l-.003-.012l-.01-.01z"/><path fill="currentColor" d="M19 3a2 2 0 0 1 1.995 1.85L21 5v14a2 2 0 0 1-1.85 1.995L19 21H5a2 2 0 0 1-1.995-1.85L3 19V5a2 2 0 0 1 1.85-1.995L5 3zm-8 12H5v4h6zm8 0h-6v4h6zm0-5h-6v3h6zm-8 0H5v3h6zm8-5h-6v3h6zm-8 0H5v3h6z"/></g></svg>""" ]
            ]
        ]

    static member FilePicker() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 8V2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-4.35l-2.862 2.861a3.7 3.7 0 0 1-1.712.97l-1.83.457a2.08 2.08 0 0 1-2.29-.938H7.75a.75.75 0 0 1 0-1.5h3.293l.021-.093l.458-1.83c.162-.648.497-1.24.97-1.712L16.355 10H14a2 2 0 0 1-2-2m6.695-.305q-.156.123-.301.267l-.538.538H14a.5.5 0 0 1-.5-.5V2.5zm.405.974l-5.903 5.903a2.7 2.7 0 0 0-.706 1.247l-.458 1.831a1.087 1.087 0 0 0 1.319 1.318l1.83-.457a2.7 2.7 0 0 0 1.248-.707l5.902-5.902A2.286 2.286 0 0 0 19.1 8.669"/></svg>""" ]

    static member DataAnnotator() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M16 10h-2V8a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v5a1 1 0 0 0 1 1h2v2a1 1 0 0 0 1 1h5a1 1 0 0 0 1-1v-5a1 1 0 0 0-1-1m-6 1v1H9V9h3v1h-1a1 1 0 0 0-1 1m5 4h-3v-3h3Zm6 3.28V5.72A2 2 0 1 0 18.28 3H5.72A2 2 0 1 0 3 5.72v12.56A2 2 0 1 0 5.72 21h12.56A2 2 0 1 0 21 18.28m-2 0a1.9 1.9 0 0 0-.72.72H5.72a1.9 1.9 0 0 0-.72-.72V5.72A1.9 1.9 0 0 0 5.72 5h12.56a1.9 1.9 0 0 0 .72.72Z"/></svg>""" ]

    static member FileImport() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M20 14V8l-6-6H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-4h-7v3l-5-4l5-4v3zM13 4l5 5h-5z"/></svg>""" ]

    static member FileExport() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M18 22a2 2 0 0 0 2-2v-5l-5 4v-3H8v-2h7v-3l5 4V8l-6-6H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2zM13 4l5 5h-5z"/></svg>""" ]

    static member Terms() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20"><g fill="currentColor"><path d="M9 6a.75.75 0 0 1 .75.75v1.5h1.5a.75.75 0 0 1 0 1.5h-1.5v1.5a.75.75 0 0 1-1.5 0v-1.5h-1.5a.75.75 0 0 1 0-1.5h1.5v-1.5A.75.75 0 0 1 9 6"/><path fill-rule="evenodd" d="M2 9a7 7 0 1 1 12.452 4.391l3.328 3.329a.75.75 0 1 1-1.06 1.06l-3.329-3.328A7 7 0 0 1 2 9m7-5.5a5.5 5.5 0 1 0 0 11a5.5 5.5 0 0 0 0-11" clip-rule="evenodd"/></g></svg>""" ]

    static member Templates() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 15 15"><path fill="none" stroke="currentColor" d="M7.5 4v7M4 7.5h7m-3.5 7a7 7 0 1 1 0-14a7 7 0 0 1 0 14Z" stroke-width="1"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M6 5h11a3 3 0 0 1 3 3v9a3 3 0 0 1-3 3H6a3 3 0 0 1-3-3V8a3 3 0 0 1 3-3M4 17a2 2 0 0 0 2 2h5v-3H4zm7-5H4v3h7zm6 7a2 2 0 0 0 2-2v-1h-7v3zm2-7h-7v3h7zM4 11h7V8H4zm8 0h7V8h-7z"/></svg>""" ]
            ]
        ]

    static member Settings() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="m9.387 17.548l.371 1.482c.133.533.692.97 1.242.97h1c.55 0 1.109-.437 1.242-.97l.371-1.482a.96.96 0 0 1 1.203-.694l1.467.42c.529.151 1.188-.114 1.462-.591l.5-.867c.274-.477.177-1.179-.219-1.562l-1.098-1.061a.96.96 0 0 1 .001-1.39l1.096-1.061c.396-.382.494-1.084.22-1.561l-.501-.867c-.275-.477-.933-.742-1.461-.591l-1.467.42a.963.963 0 0 1-1.204-.694l-.37-1.48C13.109 5.437 12.55 5 12 5h-1c-.55 0-1.109.437-1.242.97l-.37 1.48a.964.964 0 0 1-1.204.695l-1.467-.42c-.529-.152-1.188.114-1.462.59l-.5.867c-.274.477-.177 1.179.22 1.562l1.096 1.059a.965.965 0 0 1 0 1.391l-1.098 1.061c-.395.383-.494 1.085-.219 1.562l.501.867c.274.477.933.742 1.462.591l1.467-.42a.96.96 0 0 1 1.203.693M11.5 10.5a2 2 0 1 1 0 4a2 2 0 0 1 0-4"/></svg>""" ]

    static member About() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 20a8 8 0 1 0 0-16a8 8 0 0 0 0 16m0-11.25c-.69 0-1.25.56-1.25 1.25v.107a.75.75 0 1 1-1.5 0V10A2.75 2.75 0 0 1 12 7.25h.116a2.634 2.634 0 0 1 1.714 4.633l-.77.66a.9.9 0 0 0-.31.674v.533a.75.75 0 0 1-1.5 0v-.533c0-.697.304-1.359.833-1.812l.771-.66a1.134 1.134 0 0 0-.738-1.995zM13 16a1 1 0 1 1-2 0a1 1 0 0 1 2 0"/></svg>""" ]

    static member PrivacyPolicy() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a28.1 28.1 0 0 1-1.091 9M7.231 4.37a8.994 8.994 0 0 1 12.88 3.73M2.958 15S3 14.577 3 12a8.95 8.95 0 0 1 1.735-5.307m12.84 3.088A6 6 0 0 1 18 12a30 30 0 0 1-.464 6.232M6 12a6 6 0 0 1 9.352-4.974M4 21a5.96 5.96 0 0 1 1.01-3.328a5.15 5.15 0 0 0 .786-1.926m8.66 2.486a14 14 0 0 1-.962 2.683M7.5 19.336C9 17.092 9 14.845 9 12a3 3 0 1 1 6 0c0 .749 0 1.521-.031 2.311M12 12c0 3 0 6-2 9"/></svg>""" ]

    static member Docs() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M7 2a4 4 0 0 0-4 4v12a4 4 0 0 0 4 4h14V2zm4 3h7v2h-7zM5 18a2 2 0 0 1 2-2h12v4H7a2 2 0 0 1-2-2"/></svg>""" ]

    static member Contact() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M14.5 8.25c-3.268 0-6 2.419-6 5.5s2.732 5.5 6 5.5a6.5 6.5 0 0 0 2.192-.378l2.616 1.09a.5.5 0 0 0 .674-.594l-.644-2.363A5.18 5.18 0 0 0 20.5 13.75c0-3.081-2.732-5.5-6-5.5"/><path fill="currentColor" d="M4 9.5c0 1.186.454 2.276 1.214 3.133l-.499 1.828c-.069.253-.103.38-.07.46c.03.07.09.122.163.142c.084.024.205-.027.447-.128l2.044-.851q.105.042.215.08a6 6 0 0 1-.014-.414c0-3.692 3.221-6.457 6.913-6.5C13.508 5.62 11.648 4.5 9.5 4.5C6.462 4.5 4 6.739 4 9.5"/></svg>""" ]

    static member Save() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><g fill="currentColor" fill-rule="evenodd" clip-rule="evenodd"><path d="M5 3a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V7.414A2 2 0 0 0 20.414 6L18 3.586A2 2 0 0 0 16.586 3zm3 11a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v6H8zm1-7V5h6v2a1 1 0 0 1-1 1h-4a1 1 0 0 1-1-1"/><path d="M14 17h-4v-2h4z"/></g></svg>""" ]

    static member Delete() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20"><g fill="currentColor"><path d="M11.937 4.5H8.062A2.003 2.003 0 0 1 10 2a2.003 2.003 0 0 1 1.937 2.5"/><path d="M4.5 5.5a1 1 0 0 1 0-2h11a1 1 0 1 1 0 2z"/><path fill-rule="evenodd" d="M14.5 18.5a1 1 0 0 0 1-1V7a1 1 0 0 0-1-1h-9a1 1 0 0 0-1 1v10.5a1 1 0 0 0 1 1zm-2-10a.5.5 0 0 1 1 0v7a.5.5 0 0 1-1 0zM10 8a.5.5 0 0 0-.5.5v7a.5.5 0 0 0 1 0v-7A.5.5 0 0 0 10 8m-3.5.5a.5.5 0 0 1 1 0v7a.5.5 0 0 1-1 0z" clip-rule="evenodd"/></g></svg>""" ]

    static member Forward() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="currentColor" d="M16 7V3l-1.1 1.1C13.6 1.6 11 0 8 0C3.6 0 0 3.6 0 8s3.6 8 8 8c2.4 0 4.6-1.1 6-2.8l-1.5-1.3C11.4 13.2 9.8 14 8 14c-3.3 0-6-2.7-6-6s2.7-6 6-6c2.4 0 4.5 1.5 5.5 3.5L12 7z"/></svg>""" ]

    static member Backward() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16"><path fill="currentColor" d="M8 0C5 0 2.4 1.6 1.1 4.1L0 3v4h4L2.5 5.5C3.5 3.5 5.6 2 8 2c3.3 0 6 2.7 6 6s-2.7 6-6 6c-1.8 0-3.4-.8-4.5-2.1L2 13.2C3.4 14.9 5.6 16 8 16c4.4 0 8-3.6 8-8s-3.6-8-8-8"/></svg>""" ]

    static member BuildingBlockInformation() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M10.97 8.265a1.45 1.45 0 0 0-.487.57a.75.75 0 0 1-1.341-.67c.2-.402.513-.826.997-1.148C10.627 6.69 11.244 6.5 12 6.5c.658 0 1.369.195 1.934.619a2.45 2.45 0 0 1 1.004 2.006c0 1.033-.513 1.72-1.027 2.215c-.19.183-.399.358-.579.508l-.147.123a4 4 0 0 0-.435.409v1.37a.75.75 0 1 1-1.5 0v-1.473c0-.237.067-.504.247-.736c.22-.28.486-.517.718-.714l.183-.153l.001-.001c.172-.143.324-.27.47-.412c.368-.355.569-.676.569-1.136a.95.95 0 0 0-.404-.806C12.766 8.118 12.384 8 12 8c-.494 0-.814.121-1.03.265M13 17a1 1 0 1 1-2 0a1 1 0 0 1 2 0"/><path fill="currentColor" d="M12 1c6.075 0 11 4.925 11 11s-4.925 11-11 11S1 18.075 1 12S5.925 1 12 1M2.5 12a9.5 9.5 0 0 0 9.5 9.5a9.5 9.5 0 0 0 9.5-9.5A9.5 9.5 0 0 0 12 2.5A9.5 9.5 0 0 0 2.5 12"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><g fill="none"><path d="m12.593 23.258l-.011.002l-.071.035l-.02.004l-.014-.004l-.071-.035q-.016-.005-.024.005l-.004.01l-.017.428l.005.02l.01.013l.104.074l.015.004l.012-.004l.104-.074l.012-.016l.004-.017l-.017-.427q-.004-.016-.017-.018m.265-.113l-.013.002l-.185.093l-.01.01l-.003.011l.018.43l.005.012l.008.007l.201.093q.019.005.029-.008l.004-.014l-.034-.614q-.005-.018-.02-.022m-.715.002a.02.02 0 0 0-.027.006l-.006.014l-.034.614q.001.018.017.024l.015-.002l.201-.093l.01-.008l.004-.011l.017-.43l-.003-.012l-.01-.01z"/><path fill="currentColor" d="M19 3a2 2 0 0 1 1.995 1.85L21 5v14a2 2 0 0 1-1.85 1.995L19 21H5a2 2 0 0 1-1.995-1.85L3 19V5a2 2 0 0 1 1.85-1.995L5 3zm-8 12H5v4h6zm8 0h-6v4h6zm0-5h-6v3h6zm-8 0H5v3h6zm8-5h-6v3h6zm-8 0H5v3h6z"/></g></svg>""" ]
            ]
        ]

    static member RemoveBuildingBlock() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32"><path fill="currentColor" d="M16 3C8.832 3 3 8.832 3 16s5.832 13 13 13s13-5.832 13-13S23.168 3 16 3m0 2c6.087 0 11 4.913 11 11s-4.913 11-11 11S5 22.087 5 16S9.913 5 16 5m-6 10v2h12v-2z"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><g fill="none"><path d="m12.593 23.258l-.011.002l-.071.035l-.02.004l-.014-.004l-.071-.035q-.016-.005-.024.005l-.004.01l-.017.428l.005.02l.01.013l.104.074l.015.004l.012-.004l.104-.074l.012-.016l.004-.017l-.017-.427q-.004-.016-.017-.018m.265-.113l-.013.002l-.185.093l-.01.01l-.003.011l.018.43l.005.012l.008.007l.201.093q.019.005.029-.008l.004-.014l-.034-.614q-.005-.018-.02-.022m-.715.002a.02.02 0 0 0-.027.006l-.006.014l-.034.614q.001.018.017.024l.015-.002l.201-.093l.01-.008l.004-.011l.017-.43l-.003-.012l-.01-.01z"/><path fill="currentColor" d="M19 3a2 2 0 0 1 1.995 1.85L21 5v14a2 2 0 0 1-1.85 1.995L19 21H5a2 2 0 0 1-1.995-1.85L3 19V5a2 2 0 0 1 1.85-1.995L5 3zm-8 12H5v4h6zm8 0h-6v4h6zm0-5h-6v3h6zm-8 0H5v3h6zm8-5h-6v3h6zm-8 0H5v3h6z"/></g></svg>""" ]
            ]
        ]

    static member RectifyOntologyTerms(reactElement: ReactElement) =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20"><path fill="currentColor" d="M15.84 2.76c.25 0 .49.04.71.11c.23.07.44.16.64.25l.35-.81c-.52-.26-1.08-.39-1.69-.39c-.58 0-1.09.13-1.52.37q-.645.375-.99 1.08C13.11 3.83 13 4.38 13 5c0 .99.23 1.75.7 2.28s1.15.79 2.02.79c.6 0 1.13-.09 1.6-.26v-.84c-.26.08-.51.14-.74.19c-.24.05-.49.08-.74.08c-.59 0-1.04-.19-1.34-.57c-.32-.37-.47-.93-.47-1.66q0-1.05.48-1.65c.33-.4.77-.6 1.33-.6M6.5 8h1.04L5.3 2H4.24L2 8h1.03l.58-1.66H5.9zM8 2v6h2.17c.67 0 1.19-.15 1.57-.46c.38-.3.56-.72.56-1.26c0-.4-.1-.72-.3-.95c-.19-.24-.5-.39-.93-.47v-.04c.35-.06.6-.21.78-.44c.18-.24.28-.53.28-.88c0-.52-.19-.9-.56-1.14c-.36-.24-.96-.36-1.79-.36zm.98 2.48V2.82h.85c.44 0 .77.06.97.19c.21.12.31.33.31.61c0 .31-.1.53-.29.66c-.18.13-.48.2-.89.2zM5.64 5.5H3.9l.54-1.56c.14-.4.25-.76.32-1.1l.15.52c.07.23.13.4.17.51zm3.34-.23h.99c.44 0 .76.08.98.23c.21.15.32.38.32.69c0 .34-.11.59-.32.75s-.52.24-.93.24H8.98zM4 13l5 5l9-8l-1-1l-8 6l-4-3z"/></svg>""" ]
                reactElement
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="m11.4 18.161l7.396-7.396a10.3 10.3 0 0 1-3.326-2.234a10.3 10.3 0 0 1-2.235-3.327L5.839 12.6c-.577.577-.866.866-1.114 1.184a6.6 6.6 0 0 0-.749 1.211c-.173.364-.302.752-.56 1.526l-1.362 4.083a1.06 1.06 0 0 0 1.342 1.342l4.083-1.362c.775-.258 1.162-.387 1.526-.56q.647-.308 1.211-.749c.318-.248.607-.537 1.184-1.114m9.448-9.448a3.932 3.932 0 0 0-5.561-5.561l-.887.887l.038.111a8.75 8.75 0 0 0 2.092 3.32a8.75 8.75 0 0 0 3.431 2.13z"/></svg>""" ]
            ]
        ]

    static member AutoformatTable() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="m5.77 8.16l3.61.64h.01l-1.73 1.73l-4.81-.85L2 4.87l1.73-1.72l.63 3.61l1.12-1.12a8.98 8.98 0 0 1 12.72 0q.059.061.11.125q.051.065.11.125c.15.16.3.33.44.5c.09.11.18.23.27.35c.16.22.3.44.44.67l.037.057q.033.047.063.103a9 9 0 0 1 .49 1.02l.02.05c.35.88.56 1.79.63 2.72l-2.04-.29a7.2 7.2 0 0 0-.63-2.1l-.06-.12a7 7 0 0 0-.5-.84q-.026-.029-.05-.065a6.4 6.4 0 0 0-.75-.905a6.995 6.995 0 0 0-9.89 0zm10.25 5.31l4.81.85l.01.01l.85 4.81l-1.73 1.73l-.64-3.61l-1.12 1.12c-3.52 3.51-9.22 3.51-12.73 0l-.24-.27a8.96 8.96 0 0 1-2.37-5.46l2.04.29c.2 1.47.86 2.89 1.99 4.02c.23.23.48.44.73.63c2.74 2.07 6.66 1.87 9.16-.63l1.12-1.12l-3.61-.64z"/></svg>""" ]

    static member CreateAnnotationTable() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 15 15"><path fill="none" stroke="currentColor" d="M7.5 4v7M4 7.5h7m-3.5 7a7 7 0 1 1 0-14a7 7 0 0 1 0 14Z" stroke-width="1"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M6 5h11a3 3 0 0 1 3 3v9a3 3 0 0 1-3 3H6a3 3 0 0 1-3-3V8a3 3 0 0 1 3-3M4 17a2 2 0 0 0 2 2h5v-3H4zm7-5H4v3h7zm6 7a2 2 0 0 0 2-2v-1h-7v3zm2-7h-7v3h7zM4 11h7V8H4zm8 0h7V8h-7z"/></svg>""" ]
            ]
        ]

    static member CreateMetadata() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 15 15"><path fill="none" stroke="currentColor" d="M7.5 4v7M4 7.5h7m-3.5 7a7 7 0 1 1 0-14a7 7 0 0 1 0 14Z" stroke-width="1"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M11 17h2v-6h-2zm1-8q.425 0 .713-.288T13 8t-.288-.712T12 7t-.712.288T11 8t.288.713T12 9m0 13q-2.075 0-3.9-.788t-3.175-2.137T2.788 15.9T2 12t.788-3.9t2.137-3.175T8.1 2.788T12 2t3.9.788t3.175 2.137T21.213 8.1T22 12t-.788 3.9t-2.137 3.175t-3.175 2.138T12 22m0-2q3.35 0 5.675-2.325T20 12t-2.325-5.675T12 4T6.325 6.325T4 12t2.325 5.675T12 20m0-8"/></svg>""" ]
            ]
        ]

    static member Back() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M20 11H7.83l5.59-5.59L12 4l-8 8l8 8l1.41-1.41L7.83 13H20z"/></svg>""" ]

    static member GreenCheckMark() =
        Html.i [
            prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 48 48"><path fill="#43a047" d="M40.6 12.1L17 35.7l-9.6-9.6L4.6 29L17 41.3l26.4-26.4z"/></svg>"""
        ]