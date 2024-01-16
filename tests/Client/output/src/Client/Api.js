import { Remoting_buildProxy_64DC51C } from "../../fable_modules/Fable.Remoting.Client.7.30.0/Remoting.fs.js";
import { RemotingModule_createApi, RemotingModule_withRouteBuilder } from "../../fable_modules/Fable.Remoting.Client.7.30.0/Remoting.fs.js";
import { IExportAPIv1_$reflection, ITestAPI_$reflection, ISwateJsonAPIv1_$reflection, IISADotNetCommonAPIv1_$reflection, IServiceAPIv1_$reflection, IDagAPIv1_$reflection, ITemplateAPIv1_$reflection, IOntologyAPIv2_$reflection, Route_builder } from "../Shared/Shared.js";

export const api = Remoting_buildProxy_64DC51C(RemotingModule_withRouteBuilder(Route_builder, RemotingModule_createApi()), IOntologyAPIv2_$reflection());

export const templateApi = Remoting_buildProxy_64DC51C(RemotingModule_withRouteBuilder(Route_builder, RemotingModule_createApi()), ITemplateAPIv1_$reflection());

export const dagApi = Remoting_buildProxy_64DC51C(RemotingModule_withRouteBuilder(Route_builder, RemotingModule_createApi()), IDagAPIv1_$reflection());

export const serviceApi = Remoting_buildProxy_64DC51C(RemotingModule_withRouteBuilder(Route_builder, RemotingModule_createApi()), IServiceAPIv1_$reflection());

export const isaDotNetCommonApi = Remoting_buildProxy_64DC51C(RemotingModule_withRouteBuilder(Route_builder, RemotingModule_createApi()), IISADotNetCommonAPIv1_$reflection());

export const swateJsonAPIv1 = Remoting_buildProxy_64DC51C(RemotingModule_withRouteBuilder(Route_builder, RemotingModule_createApi()), ISwateJsonAPIv1_$reflection());

export const testAPIv1 = Remoting_buildProxy_64DC51C(RemotingModule_withRouteBuilder(Route_builder, RemotingModule_createApi()), ITestAPI_$reflection());

export const exportApi = Remoting_buildProxy_64DC51C(RemotingModule_withRouteBuilder(Route_builder, RemotingModule_createApi()), IExportAPIv1_$reflection());

//# sourceMappingURL=Api.js.map
