namespace Swate.Components

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type Icons =

    static member BuildingBlock() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M256 512a256 256 0 1 0 0-512a256 256 0 1 0 0 512m-24-168v-64h-64c-13.3 0-24-10.7-24-24s10.7-24 24-24h64v-64c0-13.3 10.7-24 24-24s24 10.7 24 24v64h64c13.3 0 24 10.7 24 24s-10.7 24-24 24h-64v64c0 13.3-10.7 24-24 24s-24-10.7-24-24"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M0 96c0-35.3 28.7-64 64-64h320c35.3 0 64 28.7 64 64v320c0 35.3-28.7 64-64 64H64c-35.3 0-64-28.7-64-64zm64 64v256h128V160zm320 0H256v256h128z"/></svg>""" ]
            ]
        ]

    static member FilePicker() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="16" viewBox="0 0 640 512"><path fill="currentColor" d="M64.1 64c0-35.3 28.7-64 64-64h149.5c17 0 33.3 6.7 45.3 18.7l106.4 106.6c12 12 18.7 28.3 18.7 45.3v97.5l-132 132h-42.1l-16.1-53.6c-4.7-15.7-19.1-26.4-35.5-26.4c-11.3 0-21.9 5.1-28.9 13.9l-60.1 75c-8.3 10.3-6.6 25.5 3.7 33.7s25.5 6.6 33.7-3.8l47.1-58.8l15.2 50.7c3 10.2 12.4 17.1 23 17.1h31.5c-.9 3.1-1.7 6.3-2.3 9.5l-10.9 54.5H128.1c-35.3 0-64-28.7-64-64v-384zm208-5.5V152c0 13.3 10.7 24 24 24h93.5zm60.2 408.4c2.5-12.4 8.6-23.8 17.5-32.7l118.9-118.9l80 80l-118.9 118.9c-8.9 8.9-20.3 15-32.7 17.5l-59.6 11.9c-.9.2-1.9.3-2.9.3c-8 0-14.6-6.5-14.6-14.6c0-1 .1-1.9.3-2.9l11.9-59.6zm267.8-123l-28.8 28.8l-80-80l28.8-28.8c22.1-22.1 57.9-22.1 80 0s22.1 57.9 0 80"/></svg>""" ]

    static member DataAnnotator() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"><path fill="currentColor" d="M16 10h-2V8a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v5a1 1 0 0 0 1 1h2v2a1 1 0 0 0 1 1h5a1 1 0 0 0 1-1v-5a1 1 0 0 0-1-1m-6 1v1H9V9h3v1h-1a1 1 0 0 0-1 1m5 4h-3v-3h3Zm6 3.28V5.72A2 2 0 1 0 18.28 3H5.72A2 2 0 1 0 3 5.72v12.56A2 2 0 1 0 5.72 21h12.56A2 2 0 1 0 21 18.28m-2 0a1.9 1.9 0 0 0-.72.72H5.72a1.9 1.9 0 0 0-.72-.72V5.72A1.9 1.9 0 0 0 5.72 5h12.56a1.9 1.9 0 0 0 .72.72Z"/></svg>""" ]

    static member FileImport() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"><path fill="currentColor" d="M20 14V8l-6-6H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-4h-7v3l-5-4l5-4v3zM13 4l5 5h-5z"/></svg>""" ]

    static member FileExport() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"><path fill="currentColor" d="M18 22a2 2 0 0 0 2-2v-5l-5 4v-3H8v-2h7v-3l5 4V8l-6-6H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2zM13 4l5 5h-5z"/></svg>""" ]

    static member MagnifyingClass() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M416 208c0 45.9-14.9 88.3-40 122.7l126.6 126.7c12.5 12.5 12.5 32.8 0 45.3s-32.8 12.5-45.3 0L330.7 376c-34.4 25.1-76.8 40-122.7 40C93.1 416 0 322.9 0 208S93.1 0 208 0s208 93.1 208 208m-208-96c-13.3 0-24 10.7-24 24v48h-48c-13.3 0-24 10.7-24 24s10.7 24 24 24h48v48c0 13.3 10.7 24 24 24s24-10.7 24-24v-48h48c13.3 0 24-10.7 24-24s-10.7-24-24-24h-48v-48c0-13.3-10.7-24-24-24"/></svg>""" ]

    static member Templates() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M256 512a256 256 0 1 0 0-512a256 256 0 1 0 0 512m-24-168v-64h-64c-13.3 0-24-10.7-24-24s10.7-24 24-24h64v-64c0-13.3 10.7-24 24-24s24 10.7 24 24v64h64c13.3 0 24 10.7 24 24s-10.7 24-24 24h-64v64c0 13.3-10.7 24-24 24s-24-10.7-24-24"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M256 160v96h128v-96zm-64 0H64v96h128zM0 320V96c0-35.3 28.7-64 64-64h320c35.3 0 64 28.7 64 64v320c0 35.3-28.7 64-64 64H64c-35.3 0-64-28.7-64-64zm384 0H256v96h128zm-192 96v-96H64v96z"/></svg>""" ]
            ]
        ]

    static member Settings() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M195.1 9.5c3-14.8 16.1-25.5 31.3-25.5h59.8c15.2 0 28.3 10.7 31.3 25.5l14.5 70c14.1 6 27.3 13.7 39.3 22.8l67.8-22.5c14.4-4.8 30.2 1.2 37.8 14.4l29.9 51.8c7.6 13.2 4.9 29.8-6.5 39.9L447 233.3c.9 7.4 1.3 15 1.3 22.7s-.5 15.3-1.3 22.7l53.4 47.5c11.4 10.1 14 26.8 6.5 39.9L477 417.9c-7.6 13.1-23.4 19.2-37.8 14.4l-67.8-22.5c-12.1 9.1-25.3 16.7-39.3 22.8l-14.4 69.9c-3.1 14.9-16.2 25.5-31.3 25.5h-59.8c-15.2 0-28.3-10.7-31.3-25.5l-14.4-69.9c-14.1-6-27.2-13.7-39.3-22.8l-68.1 22.5c-14.4 4.8-30.2-1.2-37.8-14.4L5.8 366.1c-7.6-13.2-4.9-29.8 6.5-39.9l53.4-47.5c-.9-7.4-1.3-15-1.3-22.7s.5-15.3 1.3-22.7l-53.4-47.5C.9 175.7-1.7 159 5.8 145.9l29.9-51.8c7.6-13.2 23.4-19.2 37.8-14.4l67.8 22.5c12.1-9.1 25.3-16.7 39.3-22.8zM256.3 336a80 80 0 1 0-.6-160a80 80 0 1 0 .6 160"/></svg>""" ]

    static member About() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"><path fill="currentColor" fill-rule="evenodd" d="M22 12c0 5.523-4.477 10-10 10S2 17.523 2 12S6.477 2 12 2s10 4.477 10 10M12 7.75c-.621 0-1.125.504-1.125 1.125a.75.75 0 0 1-1.5 0a2.625 2.625 0 1 1 4.508 1.829q-.138.142-.264.267a7 7 0 0 0-.571.617c-.22.282-.298.489-.298.662V13a.75.75 0 0 1-1.5 0v-.75c0-.655.305-1.186.614-1.583c.229-.294.516-.58.75-.814q.106-.105.193-.194A1.125 1.125 0 0 0 12 7.75M12 17a1 1 0 1 0 0-2a1 1 0 0 0 0 2" clip-rule="evenodd"/></svg>""" ]

    static member PrivacyPolicy() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M48 256c0-114.9 93.1-208 208-208c63.1 0 119.6 28.1 157.8 72.5c8.6 10.1 23.8 11.2 33.8 2.6s11.2-23.8 2.6-33.8C403.3 34.6 333.7 0 256 0C114.6 0 0 114.6 0 256v40c0 13.3 10.7 24 24 24s24-10.7 24-24zm458.5-52.9c-2.7-13-15.5-21.3-28.4-18.5s-21.3 15.5-18.5 28.4c2.9 13.9 4.5 28.3 4.5 43.1v40c0 13.3 10.7 24 24 24s24-10.7 24-24v-40c0-18.1-1.9-35.8-5.5-52.9zM256 80c-19 0-37.4 3-54.5 8.6c-15.2 5-18.7 23.7-8.3 35.9c7.1 8.3 18.8 10.8 29.4 7.9s21.8-4.4 33.4-4.4c70.7 0 128 57.3 128 128v24.9c0 25.2-1.5 50.3-4.4 75.3c-1.7 14.6 9.4 27.8 24.2 27.8c11.8 0 21.9-8.6 23.3-20.3c3.3-27.4 5-55 5-82.7v-24.9c0-97.2-78.8-176-176-176zm-105.3 68.7c-9.1-10.6-25.3-11.4-33.9-.4C93.7 178.1 80 215.4 80 256v24.9c0 24.2-2.6 48.4-7.8 71.9c-3.4 15.6 7.9 31.1 23.9 31.1c10.5 0 19.9-7 22.2-17.3c6.4-28.1 9.7-56.8 9.7-85.8v-24.9c0-27.2 8.5-52.4 22.9-73.1c7.2-10.4 8-24.6-.2-34.2zM256 160c-53 0-96 43-96 96v24.9c0 35.9-4.6 71.5-13.8 106.1c-3.8 14.3 6.7 29 21.5 29c9.5 0 17.9-6.2 20.4-15.4c10.5-39 15.9-79.2 15.9-119.7V256c0-28.7 23.3-52 52-52s52 23.3 52 52v24.9c0 36.3-3.5 72.4-10.4 107.9c-2.7 13.9 7.7 27.2 21.8 27.2c10.2 0 19-7 21-17c7.7-38.8 11.6-78.3 11.6-118.1V256c0-53-43-96-96-96m24 96c0-13.3-10.7-24-24-24s-24 10.7-24 24v24.9c0 59.9-11 119.3-32.5 175.2l-5.9 15.3c-4.8 12.4 1.4 26.3 13.8 31s26.3-1.4 31-13.8l5.9-15.3A536.2 536.2 0 0 0 280 280.9z"/></svg>""" ]

    static member Docs() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M384 512H96c-53 0-96-43-96-96V96C0 43 43 0 96 0h304c26.5 0 48 21.5 48 48v288c0 20.9-13.4 38.7-32 45.3V448c17.7 0 32 14.3 32 32s-14.3 32-32 32zM96 384c-17.7 0-32 14.3-32 32s14.3 32 32 32h256v-64zm32-232c0 13.3 10.7 24 24 24h176c13.3 0 24-10.7 24-24s-10.7-24-24-24H152c-13.3 0-24 10.7-24 24m24 72c-13.3 0-24 10.7-24 24s10.7 24 24 24h176c13.3 0 24-10.7 24-24s-10.7-24-24-24z"/></svg>""" ]

    static member Contact() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="18" viewBox="0 0 576 512"><path fill="currentColor" d="M384 144c0 97.2-86 176-192 176c-26.7 0-52.1-5-75.2-14l-81.6 43.2c-9.3 4.9-20.7 3.2-28.2-4.2s-9.2-18.9-4.2-28.2l35.6-67.2C14.3 220.2 0 183.6 0 144C0 46.8 86-32 192-32s192 78.8 192 176m0 368c-94.1 0-172.4-62.1-188.8-144c120-1.5 224.3-86.9 235.8-202.7c83.3 19.2 145 88.3 145 170.7c0 39.6-14.3 76.2-38.4 105.6l35.6 67.2c4.9 9.3 3.2 20.7-4.2 28.2s-18.9 9.2-28.2 4.2L459.2 498c-23.1 9-48.5 14-75.2 14"/></svg>""" ]

    static member Save() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M64 32C28.7 32 0 60.7 0 96v320c0 35.3 28.7 64 64 64h320c35.3 0 64-28.7 64-64V173.3c0-17-6.7-33.3-18.7-45.3L352 50.7c-12-12-28.3-18.7-45.3-18.7zm32 96c0-17.7 14.3-32 32-32h160c17.7 0 32 14.3 32 32v64c0 17.7-14.3 32-32 32H128c-17.7 0-32-14.3-32-32zm128 160a64 64 0 1 1 0 128a64 64 0 1 1 0-128"/></svg>""" ]

    static member Delete() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M136.7 5.9C141.1-7.2 153.3-16 167.1-16H281c13.8 0 26 8.8 30.4 21.9L320 32h96c17.7 0 32 14.3 32 32s-14.3 32-32 32H32C14.3 96 0 81.7 0 64s14.3-32 32-32h96zM32 144h384v304c0 35.3-28.7 64-64 64H96c-35.3 0-64-28.7-64-64zm88 64c-13.3 0-24 10.7-24 24v192c0 13.3 10.7 24 24 24s24-10.7 24-24V232c0-13.3-10.7-24-24-24m104 0c-13.3 0-24 10.7-24 24v192c0 13.3 10.7 24 24 24s24-10.7 24-24V232c0-13.3-10.7-24-24-24m104 0c-13.3 0-24 10.7-24 24v192c0 13.3 10.7 24 24 24s24-10.7 24-24V232c0-13.3-10.7-24-24-24"/></svg>""" ]

    static member Forward() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M488 192H344c-9.7 0-18.5-5.8-22.2-14.8s-1.7-19.3 5.2-26.2l46.7-46.7C298.4 45.7 189.4 51 120.2 120.2c-75 75-75 196.5 0 271.5s196.5 75 271.5 0q12.3-12.3 21.9-26.1c10.1-14.5 30.1-18 44.6-7.9s18 30.1 7.9 44.6c-8.5 12.2-18.2 23.8-29.1 34.7c-100 100-262.1 100-362 0S-25 175 75 75c94.3-94.3 243.7-99.6 344.3-16.2L471 7c6.9-6.9 17.2-8.9 26.2-5.2S512 14.3 512 24v144c0 13.3-10.7 24-24 24"/></svg>""" ]

    static member Backward() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M24 192h144c9.7 0 18.5-5.8 22.2-14.8s1.7-19.3-5.2-26.2l-46.7-46.7c75.3-58.6 184.3-53.3 253.5 15.9c75 75 75 196.5 0 271.5s-196.5 75-271.5 0c-10.2-10.2-19-21.3-26.4-33c-9.5-14.9-29.3-19.3-44.2-9.8s-19.3 29.3-9.8 44.2c9.8 15.6 21.5 30.4 35.1 43.9c100 100 262 100 362 0s100-262 0-362C342.8-19.3 193.3-24.7 92.7 58.8L41 7C34.1.2 23.8-1.9 14.8 1.8S0 14.3 0 24v144c0 13.3 10.7 24 24 24"/></svg>""" ]

    static member BuildingBlockInformation() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"><path fill="currentColor" d="M10.97 8.265a1.45 1.45 0 0 0-.487.57a.75.75 0 0 1-1.341-.67c.2-.402.513-.826.997-1.148C10.627 6.69 11.244 6.5 12 6.5c.658 0 1.369.195 1.934.619a2.45 2.45 0 0 1 1.004 2.006c0 1.033-.513 1.72-1.027 2.215c-.19.183-.399.358-.579.508l-.147.123a4 4 0 0 0-.435.409v1.37a.75.75 0 1 1-1.5 0v-1.473c0-.237.067-.504.247-.736c.22-.28.486-.517.718-.714l.183-.153l.001-.001c.172-.143.324-.27.47-.412c.368-.355.569-.676.569-1.136a.95.95 0 0 0-.404-.806C12.766 8.118 12.384 8 12 8c-.494 0-.814.121-1.03.265M13 17a1 1 0 1 1-2 0a1 1 0 0 1 2 0"/><path fill="currentColor" d="M12 1c6.075 0 11 4.925 11 11s-4.925 11-11 11S1 18.075 1 12S5.925 1 12 1M2.5 12a9.5 9.5 0 0 0 9.5 9.5a9.5 9.5 0 0 0 9.5-9.5A9.5 9.5 0 0 0 12 2.5A9.5 9.5 0 0 0 2.5 12"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M0 96c0-35.3 28.7-64 64-64h320c35.3 0 64 28.7 64 64v320c0 35.3-28.7 64-64 64H64c-35.3 0-64-28.7-64-64zm64 64v256h128V160zm320 0H256v256h128z"/></svg>""" ]
            ]
        ]

    static member RemoveBuildingBlock() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M0 256c0-17.7 14.3-32 32-32h384c17.7 0 32 14.3 32 32s-14.3 32-32 32H32c-17.7 0-32-14.3-32-32"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M0 96c0-35.3 28.7-64 64-64h320c35.3 0 64 28.7 64 64v320c0 35.3-28.7 64-64 64H64c-35.3 0-64-28.7-64-64zm64 64v256h128V160zm320 0H256v256h128z"/></svg>""" ]
            ]
        ]

    static member RectifyOntologyTerms(reactElement: ReactElement) =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="18" viewBox="0 0 576 512"><path fill="currentColor" d="M120 32c-48.6 0-88 39.4-88 88v168c0 17.7 14.3 32 32 32s32-14.3 32-32v-64h64v64c0 17.7 14.3 32 32 32s32-14.3 32-32V120c0-48.6-39.4-88-88-88zm40 128H96v-40c0-13.3 10.7-24 24-24h16c13.3 0 24 10.7 24 24zM304 32c-17.7 0-32 14.3-32 32v224c0 17.7 14.3 32 32 32h72c48.6 0 88-39.4 88-88c0-23.6-9.3-45-24.4-60.8c10.3-14.4 16.4-32.1 16.4-51.2c0-48.6-39.4-88-88-88zm64 112h-32V96h32c13.3 0 24 10.7 24 24s-10.7 24-24 24m-32 112v-48h40c13.3 0 24 10.7 24 24s-10.7 24-24 24zm233 84c11-13.8 8.8-33.9-5-45s-33.9-8.8-45 5L413.3 432.1l-38.7-38.7c-12.5-12.5-32.8-12.5-45.3 0s-12.5 32.8 0 45.3l64 64c6.4 6.4 15.3 9.8 24.4 9.3s17.5-4.9 23.2-12z"/></svg>""" ]
                reactElement
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M352.9 21.2L308 66.1L445.9 204l44.9-44.9c13.6-13.5 21.2-31.9 21.2-51.1s-7.6-37.6-21.2-51.1l-35.7-35.7C441.6 7.6 423.2 0 404 0s-37.6 7.6-51.1 21.2M274.1 100L58.9 315.1c-10.7 10.7-18.5 24.1-22.6 38.7L.9 481.6c-2.3 8.3 0 17.3 6.2 23.4s15.1 8.5 23.4 6.2l127.8-35.5c14.6-4.1 27.9-11.8 38.7-22.6l215-215.2z"/></svg>""" ]
            ]
        ]

    static member AutoformatTable() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M480.1 192h7.9c13.3 0 24-10.7 24-24V24c0-9.7-5.8-18.5-14.8-22.2S477.9.2 471 7l-51.7 51.8C375 22.1 318 0 256 0C127 0 20.3 95.4 2.6 219.5c-2.5 17.5 9.6 33.7 27.1 36.2s33.7-9.7 36.2-27.1C79.2 135.5 159.3 64 256 64c44.4 0 85.2 15 117.7 40.3L327 151c-6.9 6.9-8.9 17.2-5.2 26.2S334.3 192 344 192zm29.4 100.5c2.5-17.5-9.7-33.7-27.1-36.2s-33.7 9.7-36.2 27.1c-13.3 93-93.4 164.5-190.1 164.5c-44.4 0-85.2-15-117.7-40.3L185 361c6.9-6.9 8.9-17.2 5.2-26.2S177.7 320 168 320H24c-13.3 0-24 10.7-24 24v144c0 9.7 5.8 18.5 14.8 22.2s19.3 1.6 26.2-5.2l51.8-51.8C137 489.9 194 512 256 512c129 0 235.7-95.4 253.4-219.5z"/></svg>""" ]

    static member CreateAnnotationTable() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M256 64c0-17.7-14.3-32-32-32s-32 14.3-32 32v160H32c-17.7 0-32 14.3-32 32s14.3 32 32 32h160v160c0 17.7 14.3 32 32 32s32-14.3 32-32V288h160c17.7 0 32-14.3 32-32s-14.3-32-32-32H256z"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M256 160v96h128v-96zm-64 0H64v96h128zM0 320V96c0-35.3 28.7-64 64-64h320c35.3 0 64 28.7 64 64v320c0 35.3-28.7 64-64 64H64c-35.3 0-64-28.7-64-64zm384 0H256v96h128zm-192 96v-96H64v96z"/></svg>""" ]
            ]
        ]

    static member CreateMetadata() =
        Html.span [
            prop.style [ style.display.flex; style.alignItems.center]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M256 64c0-17.7-14.3-32-32-32s-32 14.3-32 32v160H32c-17.7 0-32 14.3-32 32s14.3 32 32 32h160v160c0 17.7 14.3 32 32 32s32-14.3 32-32V288h160c17.7 0 32-14.3 32-32s-14.3-32-32-32H256z"/></svg>""" ]
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="7" height="20" viewBox="0 0 192 512"><path fill="currentColor" d="M48 48a48 48 0 1 1 96 0a48 48 0 1 1-96 0M0 192c0-17.7 14.3-32 32-32h64c17.7 0 32 14.3 32 32v256h32c17.7 0 32 14.3 32 32s-14.3 32-32 32H32c-17.7 0-32-14.3-32-32s14.3-32 32-32h32V224H32c-17.7 0-32-14.3-32-32"/></svg>""" ]
            ]
        ]

    static member Back() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M9.4 233.4c-12.5 12.5-12.5 32.8 0 45.3l160 160c12.5 12.5 32.8 12.5 45.3 0s12.5-32.8 0-45.3L109.3 288H480c17.7 0 32-14.3 32-32s-14.3-32-32-32H109.3l105.4-105.4c12.5-12.5 12.5-32.8 0-45.3s-32.8-12.5-45.3 0l-160 160z"/></svg>""" ]

    static member Check() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="17" height="20" viewBox="0 0 448 512"><path fill="currentColor" d="M434.8 70.1c14.3 10.4 17.5 30.4 7.1 44.7l-256 352c-5.5 7.6-14 12.3-23.4 13.1s-18.5-2.7-25.1-9.3l-128-128c-12.5-12.5-12.5-32.8 0-45.3s32.8-12.5 45.3 0l101.5 101.5l234-321.7c10.4-14.3 30.4-17.5 44.7-7.1z"/></svg>""" ]

    static member ArrorRightLeft() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 16 16"><path fill="currentColor" fill-rule="evenodd" d="M13.78 3.72a.75.75 0 0 1 0 1.06l-3 3a.75.75 0 1 1-1.06-1.06L11.44 5H2.75a.75.75 0 1 1 0-1.5h8.69L9.72 1.78A.75.75 0 0 1 10.78.72zM2 11.75a.75.75 0 0 1 .22-.53l3-3a.75.75 0 1 1 1.06 1.06L4.56 11h8.69a.75.75 0 0 1 0 1.5H4.56l1.72 1.72a.75.75 0 1 1-1.06 1.06l-3-3a.75.75 0 0 1-.22-.53" clip-rule="evenodd"/></svg>"""]

    static member Pen() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M352.9 21.2L308 66.1L445.9 204l44.9-44.9c13.6-13.5 21.2-31.9 21.2-51.1s-7.6-37.6-21.2-51.1l-35.7-35.7C441.6 7.6 423.2 0 404 0s-37.6 7.6-51.1 21.2M274.1 100L58.9 315.1c-10.7 10.7-18.5 24.1-22.6 38.7L.9 481.6c-2.3 8.3 0 17.3 6.2 23.4s15.1 8.5 23.4 6.2l127.8-35.5c14.6-4.1 27.9-11.8 38.7-22.6l215-215.2z"/></svg>""" ]

    static member DeleteLeft() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="16" viewBox="0 0 640 512"><path fill="currentColor" d="M576 128c0-35.3-28.7-64-64-64H205.3c-17 0-33.3 6.7-45.3 18.7L9.4 233.4c-6 6-9.4 14.1-9.4 22.6s3.4 16.6 9.4 22.6L160 429.3c12 12 28.3 18.7 45.3 18.7H512c35.3 0 64-28.7 64-64zm-291.9 60.1c9.4-9.4 24.6-9.4 33.9 0l33.9 33.9l33.9-33.9c9.4-9.4 24.6-9.4 33.9 0s9.4 24.6 0 33.9l-33.9 33.9l33.9 33.9c9.4 9.4 9.4 24.6 0 33.9s-24.6 9.4-33.9 0l-33.9-33.9l-33.9 33.9c-9.4 9.4-24.6 9.4-33.9 0s-9.4-24.6 0-33.9l33.9-33.9l-33.9-33.9c-9.4-9.4-9.4-24.6 0-33.9"/></svg>"""  ]

    static member DeleteDown() =
        Html.div [
            prop.style [ style.transform.rotate 270]
            prop.children [
                Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="16" viewBox="0 0 640 512"><path fill="currentColor" d="M576 128c0-35.3-28.7-64-64-64H205.3c-17 0-33.3 6.7-45.3 18.7L9.4 233.4c-6 6-9.4 14.1-9.4 22.6s3.4 16.6 9.4 22.6L160 429.3c12 12 28.3 18.7 45.3 18.7H512c35.3 0 64-28.7 64-64zm-291.9 60.1c9.4-9.4 24.6-9.4 33.9 0l33.9 33.9l33.9-33.9c9.4-9.4 24.6-9.4 33.9 0s9.4 24.6 0 33.9l-33.9 33.9l33.9 33.9c9.4 9.4 9.4 24.6 0 33.9s-24.6 9.4-33.9 0l-33.9-33.9l-33.9 33.9c-9.4 9.4-24.6 9.4-33.9 0s-9.4-24.6 0-33.9l33.9-33.9l-33.9-33.9c-9.4-9.4-9.4-24.6 0-33.9"/></svg>"""  ]
            ]
        ]

    static member PenToSquare() =
        Html.i [ prop.dangerouslySetInnerHTML """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 512 512"><path fill="currentColor" d="M471.6 21.7c-21.9-21.9-57.3-21.9-79.2 0L368 46.1l97.9 97.9l24.4-24.4c21.9-21.9 21.9-57.3 0-79.2zm-299.2 220c-6.1 6.1-10.8 13.6-13.5 21.9l-29.6 88.8c-2.9 8.6-.6 18.1 5.8 24.6s15.9 8.7 24.6 5.8l88.8-29.6c8.2-2.7 15.7-7.4 21.9-13.5L432 177.9L334.1 80zM96 64c-53 0-96 43-96 96v256c0 53 43 96 96 96h256c53 0 96-43 96-96v-96c0-17.7-14.3-32-32-32s-32 14.3-32 32v96c0 17.7-14.3 32-32 32H96c-17.7 0-32-14.3-32-32V160c0-17.7 14.3-32 32-32h96c17.7 0 32-14.3 32-32s-14.3-32-32-32z"/></svg>""" ]
