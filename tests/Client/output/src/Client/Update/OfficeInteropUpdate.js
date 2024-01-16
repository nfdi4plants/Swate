import { BuildingBlockDetailsMsg, ApiMsg, ApiRequestMsg, TermSearch_Msg, Msg, DevMsg, curry, Model__updateByExcelState_Z26496641 } from "../Messages.js";
import { FillHiddenColsState, Msg as Msg_1, Model } from "../OfficeInterop/OfficeInteropState.js";
import { Cmd_batch, Cmd_OfPromise_either, Cmd_none } from "../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { autoFitTable, exampleExcelFunction2, exampleExcelFunction1, getAnnotationBlockDetails, insertFileNamesFromFilePicker, UpdateTableByTermsSearchable, getAllAnnotationBlockDetails, getParentTerm, createAnnotationTable, updateUnitForCells, removeSelectedAnnotationBlock, addAnnotationBlocksInNewSheets, addAnnotationBlocks, addAnnotationBlockHandler, insertOntologyTerm } from "../OfficeInterop/OfficeInterop.js";
import { ofArray, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { LogIdentifier, Msg_create } from "../OfficeInterop/InteropLogging.js";
import { RequestBuildingBlockInfoStates } from "../Model.js";

export function update(currentModel, excelInteropMsg) {
    switch (excelInteropMsg.tag) {
        case 2:
            return [Model__updateByExcelState_Z26496641(currentModel, new Model(excelInteropMsg.fields[0].tag === 0, currentModel.ExcelState.FillHiddenColsStateStore)), Cmd_none()];
        case 3:
            return [currentModel, Cmd_OfPromise_either(insertOntologyTerm, excelInteropMsg.fields[0], (arg_2) => (new Msg(3, [curry((tupledArg_2) => (new DevMsg(1, [tupledArg_2[0], tupledArg_2[1]])), Cmd_none(), arg_2)])), (arg_3) => (new Msg(3, [curry((tupledArg_3) => (new DevMsg(3, [tupledArg_3[0], tupledArg_3[1]])), Cmd_none(), arg_3)])))];
        case 4:
            return [currentModel, Cmd_OfPromise_either(addAnnotationBlockHandler, excelInteropMsg.fields[0], (arg_4) => (new Msg(3, [curry((tupledArg_4) => (new DevMsg(2, [tupledArg_4[0], tupledArg_4[1]])), Cmd_none(), arg_4)])), (arg_5) => (new Msg(3, [curry((tupledArg_5) => (new DevMsg(3, [tupledArg_5[0], tupledArg_5[1]])), Cmd_none(), arg_5)])))];
        case 5:
            return [currentModel, Cmd_OfPromise_either(addAnnotationBlocks, excelInteropMsg.fields[0], (arg_6) => (new Msg(3, [curry((tupledArg_6) => (new DevMsg(2, [tupledArg_6[0], tupledArg_6[1]])), Cmd_none(), arg_6)])), (arg_7) => (new Msg(3, [curry((tupledArg_7) => (new DevMsg(3, [tupledArg_7[0], tupledArg_7[1]])), Cmd_none(), arg_7)])))];
        case 6:
            return [currentModel, Cmd_OfPromise_either(addAnnotationBlocksInNewSheets, excelInteropMsg.fields[0], (arg_8) => (new Msg(3, [curry((tupledArg_8) => (new DevMsg(2, [tupledArg_8[0], tupledArg_8[1]])), Cmd_none(), arg_8)])), (arg_9) => (new Msg(3, [curry((tupledArg_9) => (new DevMsg(3, [tupledArg_9[0], tupledArg_9[1]])), Cmd_none(), arg_9)])))];
        case 7:
            return [currentModel, Cmd_OfPromise_either(removeSelectedAnnotationBlock, void 0, (arg_10) => (new Msg(3, [curry((tupledArg_10) => (new DevMsg(2, [tupledArg_10[0], tupledArg_10[1]])), Cmd_none(), arg_10)])), (arg_11) => (new Msg(3, [curry((tupledArg_11) => (new DevMsg(3, [tupledArg_11[0], tupledArg_11[1]])), Cmd_none(), arg_11)])))];
        case 8:
            return [currentModel, Cmd_OfPromise_either(updateUnitForCells, excelInteropMsg.fields[0], (arg_12) => (new Msg(3, [curry((tupledArg_12) => (new DevMsg(2, [tupledArg_12[0], tupledArg_12[1]])), Cmd_none(), arg_12)])), (arg_13) => (new Msg(3, [curry((tupledArg_13) => (new DevMsg(3, [tupledArg_13[0], tupledArg_13[1]])), Cmd_none(), arg_13)])))];
        case 0:
            return [currentModel, Cmd_OfPromise_either((tupledArg_14) => createAnnotationTable(tupledArg_14[0], tupledArg_14[1]), [false, excelInteropMsg.fields[0]], (arg_14) => (new Msg(3, [((b_14) => curry((tupledArg_15) => (new DevMsg(2, [tupledArg_15[0], tupledArg_15[1]])), singleton((dispatch) => {
                dispatch(new Msg(6, [new Msg_1(1, [])]));
            }), b_14))(arg_14)])), (arg_15) => (new Msg(3, [curry((tupledArg_16) => (new DevMsg(3, [tupledArg_16[0], tupledArg_16[1]])), Cmd_none(), arg_15)])))];
        case 1:
            return [Model__updateByExcelState_Z26496641(currentModel, new Model(true, currentModel.ExcelState.FillHiddenColsStateStore)), Cmd_none()];
        case 10:
            return [currentModel, Cmd_OfPromise_either(getParentTerm, void 0, (arg_16) => (new Msg(4, [new TermSearch_Msg(4, [arg_16])])), (arg_17) => (new Msg(3, [curry((tupledArg_17) => (new DevMsg(3, [tupledArg_17[0], tupledArg_17[1]])), Cmd_none(), arg_17)])))];
        case 11:
            return [currentModel, Cmd_batch(ofArray([Cmd_OfPromise_either(getAllAnnotationBlockDetails, void 0, (tupledArg_18) => (new Msg(3, [new DevMsg(2, [singleton((dispatch_1) => {
                dispatch_1(new Msg(2, [new ApiMsg(0, [new ApiRequestMsg(5, [tupledArg_18[0]])])]));
            }), tupledArg_18[1]])])), (arg_18) => (new Msg(3, [((b_17) => curry((tupledArg_19) => (new DevMsg(3, [tupledArg_19[0], tupledArg_19[1]])), singleton((dispatch_2) => {
                dispatch_2(new Msg(6, [new Msg_1(13, [new FillHiddenColsState(0, [])])]));
            }), b_17))(arg_18)]))), singleton((dispatch_3) => {
                dispatch_3(new Msg(6, [new Msg_1(13, [new FillHiddenColsState(1, [])])]));
            })]))];
        case 12: {
            const nextState_2 = new Model(currentModel.ExcelState.HasAnnotationTable, new FillHiddenColsState(3, []));
            const cmd_9 = Cmd_OfPromise_either(UpdateTableByTermsSearchable, excelInteropMsg.fields[0], (arg_19) => (new Msg(3, [((b_18) => curry((tupledArg_20) => (new DevMsg(2, [tupledArg_20[0], tupledArg_20[1]])), singleton((dispatch_4) => {
                dispatch_4(new Msg(6, [new Msg_1(13, [new FillHiddenColsState(0, [])])]));
            }), b_18))(arg_19)])), (arg_20) => (new Msg(3, [((b_19) => curry((tupledArg_21) => (new DevMsg(3, [tupledArg_21[0], tupledArg_21[1]])), singleton((dispatch_5) => {
                dispatch_5(new Msg(6, [new Msg_1(13, [new FillHiddenColsState(0, [])])]));
            }), b_19))(arg_20)])));
            return [Model__updateByExcelState_Z26496641(currentModel, nextState_2), cmd_9];
        }
        case 13:
            return [Model__updateByExcelState_Z26496641(currentModel, new Model(currentModel.ExcelState.HasAnnotationTable, excelInteropMsg.fields[0])), Cmd_none()];
        case 15:
            return [currentModel, Cmd_OfPromise_either(insertFileNamesFromFilePicker, excelInteropMsg.fields[0], (arg_21) => (new Msg(3, [curry((tupledArg_22) => (new DevMsg(1, [tupledArg_22[0], tupledArg_22[1]])), Cmd_none(), arg_21)])), (arg_22) => (new Msg(3, [curry((tupledArg_23) => (new DevMsg(3, [tupledArg_23[0], tupledArg_23[1]])), Cmd_none(), arg_22)])))];
        case 14:
            return [currentModel, Cmd_OfPromise_either(getAnnotationBlockDetails, void 0, (x) => {
                const msg_12 = Msg_create(new LogIdentifier(0, []), `${x}`);
                return new Msg(21, [[new Msg(3, [curry((tupledArg_24) => (new DevMsg(2, [tupledArg_24[0], tupledArg_24[1]])), Cmd_none(), singleton(msg_12))]), new Msg(13, [new BuildingBlockDetailsMsg(0, [x])])]]);
            }, (arg_23) => (new Msg(3, [((b_22) => curry((tupledArg_25) => (new DevMsg(3, [tupledArg_25[0], tupledArg_25[1]])), singleton((dispatch_6) => {
                dispatch_6(new Msg(13, [new BuildingBlockDetailsMsg(3, [new RequestBuildingBlockInfoStates(0, [])])]));
            }), b_22))(arg_23)])))];
        case 16:
            return [currentModel, Cmd_OfPromise_either(exampleExcelFunction1, void 0, (arg_24) => (new Msg(3, [curry((tupledArg_26) => (new DevMsg(1, [tupledArg_26[0], tupledArg_26[1]])), Cmd_none(), ["Debug", arg_24])])), (arg_25) => (new Msg(3, [curry((tupledArg_27) => (new DevMsg(3, [tupledArg_27[0], tupledArg_27[1]])), Cmd_none(), arg_25)])))];
        case 17:
            return [currentModel, Cmd_OfPromise_either(exampleExcelFunction2, void 0, (arg_26) => (new Msg(3, [curry((tupledArg_28) => (new DevMsg(1, [tupledArg_28[0], tupledArg_28[1]])), Cmd_none(), ["Debug", arg_26])])), (arg_27) => (new Msg(3, [curry((tupledArg_29) => (new DevMsg(3, [tupledArg_29[0], tupledArg_29[1]])), Cmd_none(), arg_27)])))];
        default:
            return [currentModel, Cmd_OfPromise_either(() => Excel.run((context) => autoFitTable(excelInteropMsg.fields[0], context)), void 0, (arg) => (new Msg(3, [curry((tupledArg) => (new DevMsg(2, [tupledArg[0], tupledArg[1]])), Cmd_none(), arg)])), (arg_1) => (new Msg(3, [curry((tupledArg_1) => (new DevMsg(3, [tupledArg_1[0], tupledArg_1[1]])), Cmd_none(), arg_1)])))];
    }
}

//# sourceMappingURL=OfficeInteropUpdate.js.map
