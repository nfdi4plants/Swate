module OfficeInterop.CustomXmlFunctions

open Fable.Core
open Fable.Core.JsInterop
open ExcelJS.Fable
open Excel
open GlobalBindings
open System.Collections.Generic
open System.Text.RegularExpressions

open Shared.OfficeInteropTypes
open Shared

open System
open Fable.SimpleXml
open Fable.SimpleXml.Generator

let getActiveTableXml (tableName:string) (completeCustomXmlParsed:XmlElement) =
    let tablexml=
        completeCustomXmlParsed
        |> SimpleXml.findElementsByName "SwateTable"
        |> List.tryFind (fun swateTableXml ->
            swateTableXml.Attributes.["Table"] = tableName
        )
    if tablexml.IsSome then
        tablexml.Value |> Some
    else
        None

module Validation =

    let getSwateValidationForCurrentTable tableName (xmlParsed:XmlElement) =
        let activeTableXml = getActiveTableXml tableName xmlParsed
        if activeTableXml.IsNone then
            None
        else
            let v = SimpleXml.findElementsByName CustomXmlTypes.Validation.ValidationXmlRoot activeTableXml.Value
            if v.Length > 1 then failwith $"Swate found multiple '<{CustomXmlTypes.Validation.ValidationXmlRoot}>' xml elements. Please contact the developer." 
            if v.Length = 0 then
                None
            else
                let tableXmlAsString = activeTableXml.Value |> ofXmlElement |> serializeXml
                CustomXmlTypes.Validation.TableValidation.ofXml tableXmlAsString |> Some

    /// Use the 'remove' parameter to remove any Swate table validation xml for the worksheet annotation table name combination in 'tableValidation'
    let private updateRemoveSwateValidation (tableValidation:CustomXmlTypes.Validation.TableValidation) (previousCompleteCustomXml:XmlElement) (remove:bool) =
    
        let currentTableXml = getActiveTableXml tableValidation.AnnotationTable previousCompleteCustomXml
    
        let nextTableXml =
            let newValidationXml = tableValidation.toXml |> SimpleXml.parseElement
            if currentTableXml.IsSome then
                // Filter SwateTable xml children, to remove all ValidationXml
                let filteredChildren =
                    currentTableXml.Value.Children
                    |> List.filter (fun x -> x.Name <> CustomXmlTypes.Validation.ValidationXmlRoot )
                {
                    currentTableXml.Value with
                        Children =
                            if remove then
                                filteredChildren
                            else
                                newValidationXml::filteredChildren
                }
            else
                let initNewSwateTableXml = $"""<SwateTable Table="{tableValidation.AnnotationTable}"></SwateTable>""" 
                let swateTableXmlEle = initNewSwateTableXml |> SimpleXml.parseElement
                {
                    swateTableXmlEle with
                        Children = [newValidationXml]
                }
        // Filter customXml children, to remove all info for this SwateTable
        let filteredPrevTableFromRootChildren =
            previousCompleteCustomXml.Children
            |> List.filter (fun x ->
                let isUpatedTable = x.Name = "SwateTable" && x.Attributes.["Table"] = tableValidation.AnnotationTable
                // Only keep the xml SwateTables which are not added in the next step
                isUpatedTable |> not
            )
        {previousCompleteCustomXml with
            Children = nextTableXml::filteredPrevTableFromRootChildren
        }

    let updateSwateValidation (tableValidation:CustomXmlTypes.Validation.TableValidation) (previousCompleteCustomXml:XmlElement) =
        updateRemoveSwateValidation tableValidation previousCompleteCustomXml false

    let removeSwateValidation (tableValidation:CustomXmlTypes.Validation.TableValidation) (previousCompleteCustomXml:XmlElement) =
        updateRemoveSwateValidation tableValidation previousCompleteCustomXml true