import { replace, create, match } from "../../fable_modules/fable-library.4.9.0/RegExp.js";

export const Pattern_TermAnnotationShortPattern = `(?<${"idspace"}>\\w+?):(?<${"localid"}>\\w+)`;

export const Pattern_TermAnnotationURIPattern = `.*\\/(?<${"idspace"}>\\w+?)[:_](?<${"localid"}>\\w+)`;

/**
 * (|Regex|_|) pattern input
 */
export function Aux_$007CRegex$007C_$007C(pattern, input) {
    const m = match(create(pattern), input);
    if (m != null) {
        return m;
    }
    else {
        return void 0;
    }
}

export function parseSquaredTermNameBrackets(headerStr) {
    const activePatternResult = Aux_$007CRegex$007C_$007C("\\[.*\\]", headerStr);
    if (activePatternResult != null) {
        const value = activePatternResult;
        return replace(value[0].trim().slice(1, (value[0].length - 2) + 1), "#\\d+", "");
    }
    else {
        return void 0;
    }
}

export function parseCoreName(headerStr) {
    const activePatternResult = Aux_$007CRegex$007C_$007C("^[^[(]*", headerStr);
    if (activePatternResult != null) {
        const value = activePatternResult;
        return value[0].trim();
    }
    else {
        return void 0;
    }
}

/**
 * This function can be used to extract `IDSPACE:LOCALID` (or: `Term Accession` from Swate header strings or obofoundry conform URI strings.
 */
export function parseTermAccession(headerStr) {
    const matchValue = headerStr.trim();
    const activePatternResult = Aux_$007CRegex$007C_$007C(Pattern_TermAnnotationShortPattern, matchValue);
    if (activePatternResult != null) {
        const value = activePatternResult;
        return value[0].trim();
    }
    else {
        const activePatternResult_1 = Aux_$007CRegex$007C_$007C(Pattern_TermAnnotationURIPattern, matchValue);
        if (activePatternResult_1 != null) {
            const value_1 = activePatternResult_1;
            return (((value_1.groups && value_1.groups.idspace) || "") + ":") + ((value_1.groups && value_1.groups.localid) || "");
        }
        else {
            return void 0;
        }
    }
}

export function parseDoubleQuotes(headerStr) {
    const activePatternResult = Aux_$007CRegex$007C_$007C("\"(.*?)\"", headerStr);
    if (activePatternResult != null) {
        const value = activePatternResult;
        return value[0].slice(1, (value[0].length - 2) + 1).trim();
    }
    else {
        return void 0;
    }
}

export function getId(headerStr) {
    const activePatternResult = Aux_$007CRegex$007C_$007C("#\\d+", headerStr);
    if (activePatternResult != null) {
        const value = activePatternResult;
        return value[0].trim().slice(1, value[0].trim().length);
    }
    else {
        return void 0;
    }
}

//# sourceMappingURL=Regex.js.map
