import { ByteArrayExtensions_SaveFileAs_5EF83E14 } from "../../../fable_modules/Fable.Remoting.Client.7.30.0/Extensions.fs.js";
import { Model as Model_2, Spreadsheet_Model__Model_fromSessionStorage_Static_Z524259A4, Model__NextPositionIsValid_Z524259A4, Model__SaveSessionSnapshot_6DDB2EDA, Spreadsheet_Model__Model_SaveToLocalStorage } from "../States/LocalHistory.js";
import { int32ToString, equals } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Swatehost } from "../Host.js";
import { send } from "../ARCitect/ARCitect.js";
import { Msg } from "../States/ARCitect.js";
import { Model__updateByJsonExporterModel_70759DCE, curry, Msg as Msg_1, DevMsg, Model } from "../Messages.js";
import { Cmd_OfPromise_either, Cmd_none } from "../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { createTable, insertTerm_IntoSelected, addBuildingBlocks, addBuildingBlock } from "../Spreadsheet/Sidebar.Controller.js";
import { Msg as Msg_2, Model__get_ActiveTable, Model_init, Model as Model_1 } from "../States/Spreadsheet.js";
import { fillColumnWithCell, deleteColumn, deleteRows, deleteRow, resetTableState, addRows, updateTableOrder, renameTable, removeTable } from "../Spreadsheet/Table.Controller.js";
import { pasteSelectedCell, pasteCell, cutSelectedCell, cutCell, copySelectedCell, copyCell } from "../Spreadsheet/Clipboard.Controller.js";
import { FSharpSet__get_IsEmpty } from "../../../fable_modules/fable-library.4.9.0/Set.js";
import { readFromBytes } from "../Spreadsheet/IO.js";
import { Msg as Msg_3, Model as Model_3 } from "../States/JsonExporterState.js";
import { now, toUniversalTime, toString } from "../../../fable_modules/fable-library.4.9.0/Date.js";
import { ArcInvestigation, ArcAssay, ArcStudy } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/ArcTypes.fs.js";
import { ARCtrl_ISA_ArcStudy__ArcStudy_toFsWorkbook_Static_Z2A9662E9 } from "../../../fable_modules/ARCtrl.ISA.Spreadsheet.1.0.4/ArcStudy.fs.js";
import { toFsWorkbook } from "../../../fable_modules/ARCtrl.ISA.Spreadsheet.1.0.4/ArcAssay.fs.js";
import { ARCtrl_Template_Template__Template_get_FileName } from "../../Shared/ARCtrl.Helper.js";
import { Template_toFsWorkbook } from "../../../fable_modules/ARCtrl.1.0.4/Templates/Template.Spreadsheet.fs.js";
import { toFsWorkbook as toFsWorkbook_1 } from "../../../fable_modules/ARCtrl.ISA.Spreadsheet.1.0.4/ArcInvestigation.fs.js";
import { Xlsx } from "../../../fable_modules/FsSpreadsheet.Exceljs.5.0.2/Xlsx.fs.js";

export function Helper_download(filename, bytes) {
    ByteArrayExtensions_SaveFileAs_5EF83E14(bytes, filename);
}

/**
 * This function will store the information correctly.
 * Can return save information to local storage (persistent between browser sessions) and session storage.
 * It works based of exlusion. As it specifies certain messages not triggering history update.
 */
export function Helper_updateHistoryStorageMsg(msg, state, model, cmd) {
    switch (msg.tag) {
        case 2:
        case 7:
        case 19:
        case 3:
        case 12:
        case 15: {
            Spreadsheet_Model__Model_SaveToLocalStorage(state);
            return [state, model, cmd];
        }
        default: {
            Spreadsheet_Model__Model_SaveToLocalStorage(state);
            const nextHistory = Model__SaveSessionSnapshot_6DDB2EDA(model.History, state);
            if (equals(model.PersistentStorageState.Host, new Swatehost(2, []))) {
                const matchValue = model.SpreadsheetModel.ArcFile;
                let matchResult, assay, study;
                if (matchValue != null) {
                    switch (matchValue.tag) {
                        case 3: {
                            matchResult = 0;
                            assay = matchValue.fields[0];
                            break;
                        }
                        case 2: {
                            matchResult = 1;
                            study = matchValue.fields[0];
                            break;
                        }
                        default:
                            matchResult = 2;
                    }
                }
                else {
                    matchResult = 2;
                }
                switch (matchResult) {
                    case 0: {
                        send(new Msg(2, [assay]));
                        break;
                    }
                    case 1: {
                        send(new Msg(3, [study]));
                        break;
                    }
                }
            }
            return [state, new Model(model.PageState, model.PersistentStorageState, model.DebouncerState, model.DevState, model.TermSearchState, model.AdvancedSearchState, model.ExcelState, model.ApiState, model.FilePickerState, model.ProtocolState, model.AddBuildingBlockState, model.ValidationState, model.BuildingBlockDetailsState, model.SettingsXmlState, model.JsonExporterModel, model.TemplateMetadataModel, model.DagModel, model.CytoscapeModel, model.SpreadsheetModel, nextHistory), cmd];
        }
    }
}

export function update(state, model, msg) {
    let msg_4, bind$0040, bind$0040_1, bind$0040_2, bind$0040_3;
    try {
        let tupledArg_2;
        const state_1 = state;
        const model_1 = model;
        const msg_1 = msg;
        switch (msg_1.tag) {
            case 22: {
                tupledArg_2 = [addBuildingBlock(msg_1.fields[0], state_1), model_1, Cmd_none()];
                break;
            }
            case 23: {
                tupledArg_2 = [addBuildingBlocks(msg_1.fields[0], state_1), model_1, Cmd_none()];
                break;
            }
            case 24: {
                tupledArg_2 = [new Model_1(state_1.ActiveView, state_1.SelectedCells, msg_1.fields[0], state_1.Clipboard), model_1, Cmd_none()];
                break;
            }
            case 25: {
                tupledArg_2 = [(bind$0040 = Model_init(), new Model_1(bind$0040.ActiveView, bind$0040.SelectedCells, msg_1.fields[0], bind$0040.Clipboard)), model_1, Cmd_none()];
                break;
            }
            case 26: {
                tupledArg_2 = [insertTerm_IntoSelected(msg_1.fields[0], state_1), model_1, Cmd_none()];
                break;
            }
            case 27: {
                throw new Error("InsertOntologyTerms not implemented in Spreadsheet.Update");
                tupledArg_2 = [state_1, model_1, Cmd_none()];
                break;
            }
            case 0: {
                const index = msg_1.fields[0];
                tupledArg_2 = [(Model__get_ActiveTable(state_1).UpdateCellAt(index[0], index[1], msg_1.fields[1]), new Model_1(state_1.ActiveView, state_1.SelectedCells, state_1.ArcFile, state_1.Clipboard)), model_1, Cmd_none()];
                break;
            }
            case 1: {
                tupledArg_2 = [(Model__get_ActiveTable(state_1).UpdateHeader(msg_1.fields[0], msg_1.fields[1]), new Model_1(state_1.ActiveView, state_1.SelectedCells, state_1.ArcFile, state_1.Clipboard)), model_1, Cmd_none()];
                break;
            }
            case 2: {
                tupledArg_2 = [new Model_1(msg_1.fields[0], state_1.SelectedCells, state_1.ArcFile, state_1.Clipboard), model_1, Cmd_none()];
                break;
            }
            case 4: {
                tupledArg_2 = [removeTable(msg_1.fields[0], state_1), model_1, Cmd_none()];
                break;
            }
            case 5: {
                tupledArg_2 = [renameTable(msg_1.fields[0], msg_1.fields[1], state_1), model_1, Cmd_none()];
                break;
            }
            case 6: {
                tupledArg_2 = [updateTableOrder(msg_1.fields[0], msg_1.fields[1], state_1), model_1, Cmd_none()];
                break;
            }
            case 7: {
                const newPosition = msg_1.fields[0] | 0;
                let patternInput;
                if (!Model__NextPositionIsValid_Z524259A4(model_1.History, newPosition)) {
                    patternInput = [state_1, model_1];
                }
                else {
                    const nextState_12 = Spreadsheet_Model__Model_fromSessionStorage_Static_Z524259A4(newPosition);
                    sessionStorage.setItem("swate_session_history_position", int32ToString(newPosition));
                    patternInput = [nextState_12, new Model(model_1.PageState, model_1.PersistentStorageState, model_1.DebouncerState, model_1.DevState, model_1.TermSearchState, model_1.AdvancedSearchState, model_1.ExcelState, model_1.ApiState, model_1.FilePickerState, model_1.ProtocolState, model_1.AddBuildingBlockState, model_1.ValidationState, model_1.BuildingBlockDetailsState, model_1.SettingsXmlState, model_1.JsonExporterModel, model_1.TemplateMetadataModel, model_1.DagModel, model_1.CytoscapeModel, model_1.SpreadsheetModel, (bind$0040_1 = model_1.History, new Model_2(bind$0040_1.HistoryItemCountLimit, newPosition, bind$0040_1.HistoryExistingItemCount, bind$0040_1.HistoryOrder)))];
                }
                tupledArg_2 = [patternInput[0], patternInput[1], Cmd_none()];
                break;
            }
            case 8: {
                tupledArg_2 = [addRows(msg_1.fields[0], state_1), model_1, Cmd_none()];
                break;
            }
            case 19: {
                const patternInput_1 = resetTableState();
                tupledArg_2 = [patternInput_1[1], new Model(model_1.PageState, model_1.PersistentStorageState, model_1.DebouncerState, model_1.DevState, model_1.TermSearchState, model_1.AdvancedSearchState, model_1.ExcelState, model_1.ApiState, model_1.FilePickerState, model_1.ProtocolState, model_1.AddBuildingBlockState, model_1.ValidationState, model_1.BuildingBlockDetailsState, model_1.SettingsXmlState, model_1.JsonExporterModel, model_1.TemplateMetadataModel, model_1.DagModel, model_1.CytoscapeModel, model_1.SpreadsheetModel, patternInput_1[0]), Cmd_none()];
                break;
            }
            case 9: {
                tupledArg_2 = [deleteRow(msg_1.fields[0], state_1), model_1, Cmd_none()];
                break;
            }
            case 10: {
                tupledArg_2 = [deleteRows(msg_1.fields[0], state_1), model_1, Cmd_none()];
                break;
            }
            case 11: {
                tupledArg_2 = [deleteColumn(msg_1.fields[0], state_1), model_1, Cmd_none()];
                break;
            }
            case 3: {
                tupledArg_2 = [new Model_1(state_1.ActiveView, msg_1.fields[0], state_1.ArcFile, state_1.Clipboard), model_1, Cmd_none()];
                break;
            }
            case 15: {
                const index_5 = msg_1.fields[0];
                tupledArg_2 = [copyCell(index_5[0], index_5[1], state_1), model_1, Cmd_none()];
                break;
            }
            case 12: {
                tupledArg_2 = [FSharpSet__get_IsEmpty(state_1.SelectedCells) ? state_1 : copySelectedCell(state_1), model_1, Cmd_none()];
                break;
            }
            case 16: {
                const index_6 = msg_1.fields[0];
                tupledArg_2 = [cutCell(index_6[0], index_6[1], state_1), model_1, Cmd_none()];
                break;
            }
            case 13: {
                tupledArg_2 = [FSharpSet__get_IsEmpty(state_1.SelectedCells) ? state_1 : cutSelectedCell(state_1), model_1, Cmd_none()];
                break;
            }
            case 17: {
                const index_7 = msg_1.fields[0];
                tupledArg_2 = [(state_1.Clipboard.Cell == null) ? state_1 : pasteCell(index_7[0], index_7[1], state_1), model_1, Cmd_none()];
                break;
            }
            case 14: {
                tupledArg_2 = [(FSharpSet__get_IsEmpty(state_1.SelectedCells) ? true : (state_1.Clipboard.Cell == null)) ? state_1 : pasteSelectedCell(state_1), model_1, Cmd_none()];
                break;
            }
            case 18: {
                const index_8 = msg_1.fields[0];
                tupledArg_2 = [fillColumnWithCell(index_8[0], index_8[1], state_1), model_1, Cmd_none()];
                break;
            }
            case 20: {
                tupledArg_2 = [state_1, model_1, Cmd_OfPromise_either(readFromBytes, msg_1.fields[0], (arg) => (new Msg_1(15, [new Msg_2(24, [arg])])), (arg_1) => (new Msg_1(3, [curry((tupledArg) => (new DevMsg(3, [tupledArg[0], tupledArg[1]])), Cmd_none(), arg_1)])))];
                break;
            }
            case 30: {
                throw new Error("ExportsJsonTable is not implemented");
                tupledArg_2 = [state_1, model_1, Cmd_none()];
                break;
            }
            case 31: {
                throw new Error("ExportJsonTables is not implemented");
                tupledArg_2 = [state_1, model_1, Cmd_none()];
                break;
            }
            case 34: {
                throw new Error("ParseTablesToDag is not implemented");
                tupledArg_2 = [state_1, model_1, Cmd_none()];
                break;
            }
            case 32: {
                const arcfile = msg_1.fields[0];
                const nextModel_3 = Model__updateByJsonExporterModel_70759DCE(model_1, (bind$0040_2 = model_1.JsonExporterModel, new Model_3(bind$0040_2.CurrentExportType, bind$0040_2.TableJsonExportType, bind$0040_2.WorkbookJsonExportType, bind$0040_2.XLSXParsingExportType, true, bind$0040_2.ShowTableExportTypeDropdown, bind$0040_2.ShowWorkbookExportTypeDropdown, bind$0040_2.ShowXLSXExportTypeDropdown, bind$0040_2.XLSXByteArray)));
                let patternInput_2;
                const n_1 = toString(toUniversalTime(now()), "yyyyMMdd_hhmmss");
                switch (arcfile.tag) {
                    case 2: {
                        patternInput_2 = [(n_1 + "_") + ArcStudy.FileName, ARCtrl_ISA_ArcStudy__ArcStudy_toFsWorkbook_Static_Z2A9662E9(arcfile.fields[0], arcfile.fields[1])];
                        break;
                    }
                    case 3: {
                        patternInput_2 = [(n_1 + "_") + ArcAssay.FileName, toFsWorkbook(arcfile.fields[0])];
                        break;
                    }
                    case 0: {
                        const t = arcfile.fields[0];
                        patternInput_2 = [(n_1 + "_") + ARCtrl_Template_Template__Template_get_FileName(t), Template_toFsWorkbook(t)];
                        break;
                    }
                    default:
                        patternInput_2 = [(n_1 + "_") + ArcInvestigation.FileName, toFsWorkbook_1(arcfile.fields[0])];
                }
                tupledArg_2 = [state_1, nextModel_3, Cmd_OfPromise_either((wb) => Xlsx.toBytes(wb), patternInput_2[1], (bytes_2) => (new Msg_1(15, [new Msg_2(33, [patternInput_2[0], bytes_2])])), (arg_2) => (new Msg_1(3, [((b_1) => curry((tupledArg_1) => (new DevMsg(3, [tupledArg_1[0], tupledArg_1[1]])), singleton((dispatch) => {
                    dispatch(new Msg_1(11, [new Msg_3(0, [false])]));
                }), b_1))(arg_2)])))];
                break;
            }
            case 33: {
                Helper_download(msg_1.fields[0], msg_1.fields[1]);
                tupledArg_2 = [state_1, Model__updateByJsonExporterModel_70759DCE(model_1, (bind$0040_3 = model_1.JsonExporterModel, new Model_3(bind$0040_3.CurrentExportType, bind$0040_3.TableJsonExportType, bind$0040_3.WorkbookJsonExportType, bind$0040_3.XLSXParsingExportType, false, bind$0040_3.ShowTableExportTypeDropdown, bind$0040_3.ShowWorkbookExportTypeDropdown, bind$0040_3.ShowXLSXExportTypeDropdown, bind$0040_3.XLSXByteArray))), Cmd_none()];
                break;
            }
            case 28: {
                throw new Error("UpdateTermColumns is not implemented yet");
                tupledArg_2 = [state_1, model_1, Cmd_none()];
                break;
            }
            case 29: {
                throw new Error("UpdateTermColumnsResponse is not implemented yet");
                tupledArg_2 = [state_1, model_1, Cmd_none()];
                break;
            }
            default:
                tupledArg_2 = [createTable(msg_1.fields[0], state_1), model_1, Cmd_none()];
        }
        return Helper_updateHistoryStorageMsg(msg, tupledArg_2[0], tupledArg_2[1], tupledArg_2[2]);
    }
    catch (e) {
        return [state, model, (msg_4 = (new Msg_1(3, [new DevMsg(3, [Cmd_none(), e])])), singleton((dispatch_1) => {
            dispatch_1(msg_4);
        }))];
    }
}

//# sourceMappingURL=SpreadsheetUpdate.js.map
