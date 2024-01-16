import { class_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { empty, map, singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { toString } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { value as value_1 } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { printf, toConsole } from "../../../fable_modules/fable-library.4.9.0/String.js";

export class Node$ {
    constructor() {
    }
}

export function Node$_$reflection() {
    return class_type("Cytoscape.JS.Node", void 0, Node$);
}

export function Node_create_Z7BB98F04(id, data, position) {
    return createObj(toList(delay(() => append(singleton(["data", createObj(toList(delay(() => append(singleton(["id", toString(id)]), delay(() => ((data != null) ? map((tupledArg) => [tupledArg[0], tupledArg[1]], value_1(data)) : empty()))))))]), delay(() => {
        if (position != null) {
            const arg_1 = value_1(position);
            toConsole(printf("target accession (%A) %A"))(id)(arg_1);
            return singleton(["position", {
                x: value_1(position).x,
                y: value_1(position).y,
            }]);
        }
        else {
            return empty();
        }
    })))));
}

export class Edge {
    constructor() {
    }
}

export function Edge_$reflection() {
    return class_type("Cytoscape.JS.Edge", void 0, Edge);
}

export function Edge_create_6704E9A6(id, source, target, data) {
    return {
        data: createObj(toList(delay(() => append(singleton(["id", toString(id)]), delay(() => append(singleton(["source", toString(source)]), delay(() => append(singleton(["target", toString(target)]), delay(() => ((data != null) ? map((tupledArg) => [tupledArg[0], tupledArg[1]], value_1(data)) : empty())))))))))),
    };
}

export function createNode(id) {
    return {
        data: {
            id: id,
        },
    };
}

export function createEdge(id, source, target) {
    return {
        data: {
            id: id,
            source: source,
            target: target,
        },
    };
}

//# sourceMappingURL=Cytoscape.js.map
