import { Test_testCase, Test_testList } from "../fable_modules/Fable.Mocha.2.17.0/Mocha.fs.js";
import { assertEqual } from "../fable_modules/fable-library.4.9.0/Util.js";
import { singleton } from "../fable_modules/fable-library.4.9.0/List.js";

export const example_tests = Test_testList("example", singleton(Test_testCase("One", () => {
    assertEqual(1, 1, "");
})));

export const shared = Test_testList("Shared", singleton(example_tests));

//# sourceMappingURL=Shared.Tests.js.map
