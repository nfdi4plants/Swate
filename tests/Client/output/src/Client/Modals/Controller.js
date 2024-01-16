import { createRoot } from "react-dom/client";

function createId(name) {
    return "modal_inner_" + name;
}

export function removeModal(name) {
    const id = createId(name);
    const ele = document.getElementById(id);
    if (!(ele == null)) {
        ele.remove();
    }
}

/**
 * Function to add a modal to the html body of the active document. If an object with the same name exists, it is removed first.
 */
export function renderModal(name, reactElement) {
    const parent = document.getElementById("modal-container");
    const id = createId(name);
    const ele = document.getElementById(id);
    if (!(ele == null)) {
        ele.remove();
    }
    const child = document.createElement("div");
    child.id = id;
    parent.appendChild(child);
    const r = createRoot(document.getElementById(id));
    r.render(reactElement((_arg) => {
        r.unmount();
        removeModal(name);
    }));
}

//# sourceMappingURL=Controller.js.map
