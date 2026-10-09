import GeneratedTree from "../../dist/Composite/Tree/Tree.fs";
import { nativeOptionValue } from "../../dist/Composite/Tree/Types.fs";
import { bindTree } from "./TreePublicApi";

export default bindTree(GeneratedTree, nativeOptionValue);
