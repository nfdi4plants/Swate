import { Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, string_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { CSSProp } from "../../../fable_modules/Fable.React.9.3.0/Fable.React.Props.fs.js";
import { ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";

export const Excel_Shade20 = "#004b1c";

export const Excel_Shade10 = "#0e5c2f";

export const Excel_Primary = "#217346";

export const Excel_Tint10 = "#3f8159";

export const Excel_Tint20 = "#4e9668";

export const Excel_Tint30 = "#6eb38a";

export const Excel_Tint40 = "#9fcdb3";

export const Excel_Tint50 = "#e9f5ee";

export const Colorfull_gray180 = "#252423";

export const Colorfull_gray140 = "#484644";

export const Colorfull_gray130 = "#605e5c";

export const Colorfull_gray80 = "#b3b0ad";

export const Colorfull_gray60 = "#c8c6c4";

export const Colorfull_gray50 = "#d2d0ce";

export const Colorfull_gray40 = "#e1dfdd";

export const Colorfull_gray30 = "#edebe9";

export const Colorfull_gray20 = "#f3f2f1";

export const Colorfull_gray10 = "#e5e5e5";

export const Colorfull_white = "#ffffff";

export const Black_Primary = "#000000";

export const Black_gray190 = "#201f1e";

export const Black_gray180 = "#252423";

export const Black_gray170 = "#292827";

export const Black_gray160 = "#323130";

export const Black_gray150 = "#3b3a39";

export const Black_gray140 = "#484644";

export const Black_gray130 = "#605e5c";

export const Black_gray100 = "#979593";

export const Black_gray90 = "#a19f9d";

export const Black_gray70 = "#bebbb8";

export const Black_gray40 = "#e1dfdd";

export const Black_white = "#ffffff";

export class ColorMode extends Record {
    constructor(Name, BodyBackground, BodyForeground, ControlBackground, ControlForeground, ElementBackground, ElementForeground, Text$, Accent, Fade) {
        super();
        this.Name = Name;
        this.BodyBackground = BodyBackground;
        this.BodyForeground = BodyForeground;
        this.ControlBackground = ControlBackground;
        this.ControlForeground = ControlForeground;
        this.ElementBackground = ElementBackground;
        this.ElementForeground = ElementForeground;
        this.Text = Text$;
        this.Accent = Accent;
        this.Fade = Fade;
    }
}

export function ColorMode_$reflection() {
    return record_type("ExcelColors.ColorMode", [], ColorMode, () => [["Name", string_type], ["BodyBackground", string_type], ["BodyForeground", string_type], ["ControlBackground", string_type], ["ControlForeground", string_type], ["ElementBackground", string_type], ["ElementForeground", string_type], ["Text", string_type], ["Accent", string_type], ["Fade", string_type]]);
}

export const darkMode = new ColorMode("Dark", Black_gray180, Black_gray160, Black_gray140, Black_gray100, Black_Primary, Black_gray140, "#FEFEFE", "#FEFEFE", Black_gray70);

export const colorfullMode = new ColorMode("Colorfull", "#FEFEFE", Colorfull_gray20, "#FEFEFE", Colorfull_gray40, Excel_Tint10, "#FEFEFE", Colorfull_gray180, Excel_Primary, "whitesmoke");

export const transparentMode = new ColorMode("Dark_rgb", "transparent", Black_gray160, Black_gray140, Black_gray100, Black_Primary, Black_gray140, "#FEFEFE", "#FEFEFE", Black_gray70);

export function colorElement(mode) {
    return ["style", {
        backgroundColor: mode.ElementBackground,
        borderColor: mode.ElementForeground,
        color: mode.Text,
    }];
}

export function colorElementInArray(mode) {
    return ofArray([new CSSProp(21, [mode.ElementBackground]), new CSSProp(49, [mode.ElementForeground]), new CSSProp(103, [mode.Text])]);
}

export function colorElementInArray_Feliz(mode) {
    return ofArray([["backgroundColor", mode.ElementBackground], ["borderColor", mode.ElementForeground], ["color", mode.Text]]);
}

/**
 * This color control element can be used to assign multiple css props at once.
 * If used as html element this will be overwritten by any other used Style [].
 * If you want to use additional Style [], then use "Style [... yield! colorControlInArray mode]".
 */
export function colorControl(mode) {
    return ["style", {
        backgroundColor: mode.ControlBackground,
        borderColor: mode.ControlForeground,
        color: mode.Text,
    }];
}

/**
 * Use this as "Style [... yield! colorControlInArray mode]" to quickly assign excel directed color control to an element.
 */
export function colorControlInArray(mode) {
    return ofArray([new CSSProp(21, [mode.ControlBackground]), new CSSProp(49, [mode.ControlForeground]), new CSSProp(103, [mode.Text])]);
}

export function colorControlInArray_Feliz(mode) {
    return ofArray([["backgroundColor", mode.ControlBackground], ["borderColor", mode.ControlForeground], ["color", mode.Text]]);
}

//# sourceMappingURL=ExcelColors.js.map
