import { defaultOf } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { ArcAssay_fromArcJsonString, ArcAssay_toArcJsonString } from "../../../fable_modules/ARCtrl.ISA.Json.1.0.4/ArcTypes/ArcAssay.fs.js";
import { ArcStudy_fromArcJsonString, ArcStudy_toArcJsonString } from "../../../fable_modules/ARCtrl.ISA.Json.1.0.4/ArcTypes/ArcStudy.fs.js";
import { getUnionFields, name } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { IEventHandler, Msg_$reflection } from "../States/ARCitect.js";
import { Cmd_none } from "../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { Msg, DevMsg } from "../Messages.js";
import { log } from "../Helper.js";
import { ARCtrlHelper_ArcFiles } from "../../Shared/ARCtrl.Helper.js";
import { Msg as Msg_1 } from "../States/Spreadsheet.js";
import { empty } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { some } from "../../../fable_modules/fable-library.4.9.0/Option.js";

export function send(msg) {
    const data = (msg.tag === 4) ? defaultOf() : ((msg.tag === 2) ? ArcAssay_toArcJsonString(msg.fields[0]) : ((msg.tag === 3) ? ArcStudy_toArcJsonString(msg.fields[0]) : ((msg.tag === 1) ? msg.fields[0] : "Hello from Swate!")));
    const methodName = name(getUnionFields(msg, Msg_$reflection())[0]);
    window.top.postMessage({
        api: methodName,
        data: data,
        swate: true,
    }, "*");
}

export function EventHandler(dispatch) {
    return new IEventHandler((exn) => {
        dispatch(new Msg(3, [new DevMsg(3, [Cmd_none(), exn])]));
    }, (data) => {
        const assay = ArcAssay_fromArcJsonString(data.ArcAssayJsonString);
        log(`Received Assay ${assay.Identifier} from ARCitect!`);
        dispatch(new Msg(15, [new Msg_1(25, [new ARCtrlHelper_ArcFiles(3, [assay])])]));
    }, (data_1) => {
        const study = ArcStudy_fromArcJsonString(data_1.ArcStudyJsonString);
        dispatch(new Msg(15, [new Msg_1(25, [new ARCtrlHelper_ArcFiles(2, [study, empty()])])]));
        log(`Received Study ${study.Identifier} from ARCitect!`);
        console.log(some(study));
    });
}

//# sourceMappingURL=ARCitect.js.map
