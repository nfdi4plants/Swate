import { Union } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { union_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";

export class Swatehost extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Browser", "Excel", "ARCitect"];
    }
}

export function Swatehost_$reflection() {
    return union_type("Host.Swatehost", [], Swatehost, () => [[], [], []]);
}

export function Swatehost_ofQueryParam_71136F3F(queryInteger) {
    let matchResult;
    if (queryInteger != null) {
        switch (queryInteger) {
            case 1: {
                matchResult = 0;
                break;
            }
            case 2: {
                matchResult = 1;
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
        case 0:
            return new Swatehost(2, []);
        case 1:
            return new Swatehost(1, []);
        default:
            return new Swatehost(0, []);
    }
}

//# sourceMappingURL=Host.js.map
