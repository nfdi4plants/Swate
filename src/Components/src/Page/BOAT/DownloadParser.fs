namespace Components

open ARCtrl
open Fable.Remoting.Client
open Types

module Templates =
    let userTablewithColumns (annoState: Annotation list, fileName: string) =
        let userTable = ArcTable.init(fileName) // possible userinput to change table name
        for a in annoState do
            let header =
                match a.Search.KeyType with
                |CompositeHeaderDiscriminate.Component -> CompositeHeader.Component a.Search.Key
                |CompositeHeaderDiscriminate.Characteristic -> CompositeHeader.Characteristic a.Search.Key
                |CompositeHeaderDiscriminate.Parameter -> CompositeHeader.Parameter a.Search.Key
                |CompositeHeaderDiscriminate.Factor -> CompositeHeader.Factor a.Search.Key
                |_ -> CompositeHeader.OfHeaderString (a.Search.KeyType.ToString())
                
            userTable.AddColumn(
                header,
                ResizeArray[a.Search.Body]
            )
        userTable
    let userTemplate (fileName: string, annoState: Annotation list) =
        Template.create(
            System.Guid.NewGuid(),
            userTablewithColumns (annoState, fileName),
            fileName,
            lastUpdated = System.DateTime.UtcNow
            
        )

open FsSpreadsheet.Js
open ARCtrl.Json


module DownloadParser =

    let private download(filename, bytes:byte []) = bytes.SaveFileAs(filename)

    let private downloadFromString(filename, content:string) =
        let bytes = System.Text.Encoding.UTF8.GetBytes(content)
        bytes.SaveFileAs(filename)

    let downloadXlsxProm(fileName, annoState) =
        promise {
            let! bytes =
                Spreadsheet.Template.toFsWorkbook 
                    (Templates.userTemplate(fileName, annoState))
                    |> Xlsx.toXlsxBytes 
            download(fileName + "Table" + ".xlsx", bytes)
        }


    let downloadJsonProm(fileName, annoState) =
        promise {
            let jsonString = Template.toJsonString 0 (Templates.userTemplate(fileName, annoState))
            downloadFromString(fileName + "Table" + ".json", jsonString)
        }
