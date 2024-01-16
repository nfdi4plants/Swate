import human_readable_ids from "human-readable-ids";
import { split, remove, substring, join } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { map } from "../../../fable_modules/fable-library.4.9.0/Array.js";

export const hri = human_readable_ids.hri;

export function tableName() {
    return join("", map((word) => (substring(word, 0, 1).toLocaleUpperCase() + remove(word, 0, 1)), split(hri.random(), ["-"], void 0, 1)));
}

//# sourceMappingURL=HumanReadableIds.js.map
