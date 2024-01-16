import { Protocol_Model } from "../../Model.js";
import { Cmd_none } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { Cmd_OfAsync_start, Cmd_OfAsyncWith_either } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { templateApi } from "../../Api.js";
import { DevMsg, curry, Msg, Protocol_Msg } from "../../Messages.js";
import { map } from "../../../../fable_modules/fable-library.4.9.0/Array.js";
import { Template_fromJsonString } from "../../../../fable_modules/ARCtrl.1.0.4/Templates/Template.Json.fs.js";
import { Route } from "../../Routing.js";
import { singleton } from "../../../../fable_modules/fable-library.4.9.0/List.js";

export function update(fujMsg, currentState) {
    switch (fujMsg.tag) {
        case 1:
            return [new Protocol_Model(false, fujMsg.fields[0], currentState.ProtocolSelected, currentState.ProtocolsAll), Cmd_none()];
        case 3:
            return [new Protocol_Model(true, currentState.UploadedFileParsed, currentState.ProtocolSelected, currentState.ProtocolsAll), Cmd_OfAsyncWith_either((x) => {
                Cmd_OfAsync_start(x);
            }, templateApi.getTemplates, void 0, (arg) => (new Msg(10, [new Protocol_Msg(4, [arg])])), (arg_1) => (new Msg(3, [curry((tupledArg) => (new DevMsg(3, [tupledArg[0], tupledArg[1]])), Cmd_none(), arg_1)])))];
        case 4:
            return [new Protocol_Model(false, currentState.UploadedFileParsed, currentState.ProtocolSelected, map(Template_fromJsonString, fujMsg.fields[0])), Cmd_none()];
        case 5:
            return [new Protocol_Model(currentState.Loading, currentState.UploadedFileParsed, fujMsg.fields[0], currentState.ProtocolsAll), singleton((dispatch) => {
                dispatch(new Msg(19, [new Route(5, [])]));
            })];
        case 6: {
            throw new Error("ParseUploadedFileRequest IS NOT IMPLEMENTED YET");
            return [currentState, Cmd_none()];
        }
        case 8:
            return [new Protocol_Model(fujMsg.fields[0], currentState.UploadedFileParsed, currentState.ProtocolSelected, currentState.ProtocolsAll), Cmd_none()];
        case 7:
            return [new Protocol_Model(currentState.Loading, currentState.UploadedFileParsed, void 0, currentState.ProtocolsAll), Cmd_none()];
        case 2:
            return [new Protocol_Model(currentState.Loading, new Array(0), currentState.ProtocolSelected, currentState.ProtocolsAll), Cmd_none()];
        default: {
            throw new Error("ParseUploadedFileRequest IS NOT IMPLEMENTED YET");
            return [new Protocol_Model(true, currentState.UploadedFileParsed, currentState.ProtocolSelected, currentState.ProtocolsAll), Cmd_none()];
        }
    }
}

//# sourceMappingURL=ProtocolState.js.map
