import { createAtom } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { some, value as value_3 } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { append, map, delay, toArray } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { Edge_create_6704E9A6, Node_create_Z7BB98F04 } from "../../JsBindings/Cytoscape.js";
import cytoscape from "cytoscape";

export let cy = createAtom(void 0);

export function updateLayout() {
    const layout = value_3(cy()).layout({
        boundingBox: {
            h: 300,
            w: 600,
            x1: 0,
            y1: 0,
        },
        name: "cose",
        nodeDimensionsIncludeLabels: true,
        nodeOverlap: 20,
    });
    layout.run();
}

export function centerOn(accession) {
    const mainNode = value_3(cy()).nodes(`[accession = "${accession}"]`);
    value_3(cy()).center(mainNode);
}

export function createClickEvent(ev) {
    const objectArg = value_3(cy());
    objectArg.bind("click", "node", ev);
}

export function createCy(model) {
    const tree = value_3(model.CyTermTree);
    const element = document.getElementById("cy");
    const nodes = toArray(delay(() => map((node) => Node_create_Z7BB98F04(node.NodeId, [["accession", node.Term.Accession], ["name", node.Term.Name], ["ontology", node.Term.FK_Ontology]]), tree.Nodes)));
    const rlts = toArray(delay(() => map((rlt) => Edge_create_6704E9A6(rlt.RelationshipId, rlt.StartNodeId, rlt.EndNodeId, [["type", rlt.Type]]), tree.Relationships)));
    const cy_ele = cytoscape({
        container: element,
        elements: toArray(delay(() => append(nodes, delay(() => rlts)))),
        style: [{
            selector: "node",
            style: {
                label: "data(name)",
            },
        }, {
            selector: `[accession = "${model.TargetAccession}"]`,
            style: {
                "background-color": "red",
            },
        }, {
            selector: "edge",
            style: {
                width: 3,
                "line-color": "#ccc",
                "target-arrow-color": "black",
                "target-arrow-shape": "triangle",
                "curve-style": "bezier",
                label: "data(type)",
            },
        }, {
            selector: "[type = \"is_a\"]",
            style: {
                "line-color": "orange",
            },
        }],
        wheelSensitivity: 0.5,
    });
    cy(cy_ele);
    centerOn(model.TargetAccession);
    createClickEvent((e) => {
        console.log(some(e.target.position()));
    });
    updateLayout();
}

//# sourceMappingURL=CytoscapeGraph.js.map
