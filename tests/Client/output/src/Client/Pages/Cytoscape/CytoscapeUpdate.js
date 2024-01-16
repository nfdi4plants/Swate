import { Model_init_6DFDD678, Msg, Model } from "../../States/CytoscapeState.js";
import { createCy } from "./CytoscapeGraph.js";
import { Cmd_ofEffect, Cmd_batch, Cmd_none } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { Cmd_OfAsync_start, Cmd_OfAsyncWith_either } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { api } from "../../Api.js";
import { DevMsg, curry, Msg as Msg_1 } from "../../Messages.js";
import { renderModal } from "../../Modals/Controller.js";
import { view } from "../../Modals/CytoscapeView.js";
import { ofArray } from "../../../../fable_modules/fable-library.4.9.0/List.js";

export function update(msg, currentState, currentModel) {
    if (msg.tag === 1) {
        const nextState_1 = new Model(currentState.TargetAccession, msg.fields[0]);
        createCy(nextState_1);
        return [nextState_1, currentModel, Cmd_none()];
    }
    else {
        const accession = msg.fields[0];
        const cmd = Cmd_OfAsyncWith_either((x) => {
            Cmd_OfAsync_start(x);
        }, api.getTreeByAccession, accession, (arg) => (new Msg_1(14, [new Msg(1, [arg])])), (arg_1) => (new Msg_1(3, [curry((tupledArg) => (new DevMsg(3, [tupledArg[0], tupledArg[1]])), Cmd_none(), arg_1)])));
        return [Model_init_6DFDD678(accession), currentModel, Cmd_batch(ofArray([Cmd_ofEffect((_arg) => {
            renderModal("CytoscapeView", view);
        }), cmd]))];
    }
}

//# sourceMappingURL=CytoscapeUpdate.js.map
