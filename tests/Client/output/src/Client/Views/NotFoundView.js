import * as react from "react";

export function notFoundComponent(model, dispatch) {
    let s;
    const children = [(s = "The requested url does not exist in context of this application. Please tell us how you got here so we can fix this together.", s)];
    return react.createElement("div", {}, ...children);
}

//# sourceMappingURL=NotFoundView.js.map
