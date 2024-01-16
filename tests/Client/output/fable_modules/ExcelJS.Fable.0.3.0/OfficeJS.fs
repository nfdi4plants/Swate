// ts2fable 0.7.1
namespace ExcelJS.Fable

open System
open Fable.Core
open Fable.Core.JS
open Browser.Types

type Array<'T> = System.Collections.Generic.IList<'T>
type Error = System.Exception

module rec Office =

    type [<AllowNullLiteral>] IExports =
        abstract Promise: IPromiseConstructor
        abstract IPromiseConstructor: IPromiseConstructorStatic
        abstract context: Context
        /// <summary>Occurs when the runtime environment is loaded and the add-in is ready to start interacting with the application and hosted document. 
        /// 
        /// The reason parameter of the initialize event listener function returns an `InitializationReason` enumeration value that specifies how 
        /// initialization occurred. A task pane or content add-in can be initialized in two ways:
        /// 
        ///   - The user just inserted it from Recently Used Add-ins section of the Add-in drop-down list on the Insert tab of the ribbon in the Office 
        /// host application, or from Insert add-in dialog box.
        /// 
        ///   - The user opened a document that already contains the add-in.
        /// 
        /// *Note*: The reason parameter of the initialize event listener function only returns an `InitializationReason` enumeration value for task pane 
        /// and content add-ins. It does not return a value for Outlook add-ins.</summary>
        /// <param name="reason">Indicates how the app was initialized.</param>
        abstract initialize: reason: InitializationReason -> unit
        /// <summary>Ensures that the Office JavaScript APIs are ready to be called by the add-in. If the framework hasn't initialized yet, the callback or promise 
        /// will wait until the Office host is ready to accept API calls. Note that though this API is intended to be used inside an Office add-in, it can 
        /// also be used outside the add-in. In that case, once Office.js determines that it is running outside of an Office host application, it will call 
        /// the callback and resolve the promise with "null" for both the host and platform.</summary>
        /// <param name="callback">- An optional callback method, that will receive the host and platform info. 
        ///   Alternatively, rather than use a callback, an add-in may simply wait for the Promise returned by the function to resolve.</param>
        abstract onReady: ?callback: (IExportsOnReady -> obj option) -> Promise<IExportsOnReady>
        /// <summary>Toggles on and off the `Office` alias for the full `Microsoft.Office.WebExtension` namespace.</summary>
        /// <param name="useShortNamespace">True to use the shortcut alias; otherwise false to disable it. The default is true.</param>
        abstract useShortNamespace: useShortNamespace: bool -> unit
        abstract addin: Addin
        abstract ribbon: Ribbon
        /// <summary>Returns a promise of an object described in the expression. Callback is invoked only if method fails.</summary>
        /// <param name="expression">The object to be retrieved. Example "bindings#BindingName", retrieves a binding promise for a binding named 'BindingName'</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract select: expression: string * ?callback: (AsyncResult<obj option> -> unit) -> Binding
        abstract TableData: TableDataStatic

    type [<AllowNullLiteral>] IPromiseConstructor =
        /// A reference to the prototype.
        abstract prototype: Promise<obj option>
        /// <summary>Creates a Promise that is resolved with an array of results when all of the provided Promises
        /// resolve, or rejected when any Promise is rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract all: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> * U2<'T6, Promise<'T6>> * U2<'T7, Promise<'T7>> * U2<'T8, Promise<'T8>> * U2<'T9, Promise<'T9>> * U2<'T10, Promise<'T10>> -> Promise<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6 * 'T7 * 'T8 * 'T9 * 'T10>
        /// <summary>Creates a Promise that is resolved with an array of results when all of the provided Promises
        /// resolve, or rejected when any Promise is rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract all: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> * U2<'T6, Promise<'T6>> * U2<'T7, Promise<'T7>> * U2<'T8, Promise<'T8>> * U2<'T9, Promise<'T9>> -> Promise<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6 * 'T7 * 'T8 * 'T9>
        /// <summary>Creates a Promise that is resolved with an array of results when all of the provided Promises
        /// resolve, or rejected when any Promise is rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract all: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> * U2<'T6, Promise<'T6>> * U2<'T7, Promise<'T7>> * U2<'T8, Promise<'T8>> -> Promise<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6 * 'T7 * 'T8>
        /// <summary>Creates a Promise that is resolved with an array of results when all of the provided Promises
        /// resolve, or rejected when any Promise is rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract all: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> * U2<'T6, Promise<'T6>> * U2<'T7, Promise<'T7>> -> Promise<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6 * 'T7>
        /// <summary>Creates a Promise that is resolved with an array of results when all of the provided Promises
        /// resolve, or rejected when any Promise is rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract all: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> * U2<'T6, Promise<'T6>> -> Promise<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6>
        /// <summary>Creates a Promise that is resolved with an array of results when all of the provided Promises
        /// resolve, or rejected when any Promise is rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract all: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> -> Promise<'T1 * 'T2 * 'T3 * 'T4 * 'T5>
        /// <summary>Creates a Promise that is resolved with an array of results when all of the provided Promises
        /// resolve, or rejected when any Promise is rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract all: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> -> Promise<'T1 * 'T2 * 'T3 * 'T4>
        /// <summary>Creates a Promise that is resolved with an array of results when all of the provided Promises
        /// resolve, or rejected when any Promise is rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract all: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> -> Promise<'T1 * 'T2 * 'T3>
        /// <summary>Creates a Promise that is resolved with an array of results when all of the provided Promises
        /// resolve, or rejected when any Promise is rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract all: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> -> Promise<'T1 * 'T2>
        /// <summary>Creates a Promise that is resolved with an array of results when all of the provided Promises
        /// resolve, or rejected when any Promise is rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract all: values: ResizeArray<U2<'T, Promise<'T>>> -> Promise<ResizeArray<'T>>
        /// <summary>Creates a Promise that is resolved or rejected when any of the provided Promises are resolved
        /// or rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract race: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> * U2<'T6, Promise<'T6>> * U2<'T7, Promise<'T7>> * U2<'T8, Promise<'T8>> * U2<'T9, Promise<'T9>> * U2<'T10, Promise<'T10>> -> Promise<obj>
        /// <summary>Creates a Promise that is resolved or rejected when any of the provided Promises are resolved
        /// or rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract race: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> * U2<'T6, Promise<'T6>> * U2<'T7, Promise<'T7>> * U2<'T8, Promise<'T8>> * U2<'T9, Promise<'T9>> -> Promise<obj>
        /// <summary>Creates a Promise that is resolved or rejected when any of the provided Promises are resolved
        /// or rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract race: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> * U2<'T6, Promise<'T6>> * U2<'T7, Promise<'T7>> * U2<'T8, Promise<'T8>> -> Promise<U8<'T1, 'T2, 'T3, 'T4, 'T5, 'T6, 'T7, 'T8>>
        /// <summary>Creates a Promise that is resolved or rejected when any of the provided Promises are resolved
        /// or rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract race: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> * U2<'T6, Promise<'T6>> * U2<'T7, Promise<'T7>> -> Promise<U7<'T1, 'T2, 'T3, 'T4, 'T5, 'T6, 'T7>>
        /// <summary>Creates a Promise that is resolved or rejected when any of the provided Promises are resolved
        /// or rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract race: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> * U2<'T6, Promise<'T6>> -> Promise<U6<'T1, 'T2, 'T3, 'T4, 'T5, 'T6>>
        /// <summary>Creates a Promise that is resolved or rejected when any of the provided Promises are resolved
        /// or rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract race: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> * U2<'T5, Promise<'T5>> -> Promise<U5<'T1, 'T2, 'T3, 'T4, 'T5>>
        /// <summary>Creates a Promise that is resolved or rejected when any of the provided Promises are resolved
        /// or rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract race: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> * U2<'T4, Promise<'T4>> -> Promise<U4<'T1, 'T2, 'T3, 'T4>>
        /// <summary>Creates a Promise that is resolved or rejected when any of the provided Promises are resolved
        /// or rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract race: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> * U2<'T3, Promise<'T3>> -> Promise<U3<'T1, 'T2, 'T3>>
        /// <summary>Creates a Promise that is resolved or rejected when any of the provided Promises are resolved
        /// or rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract race: values: U2<'T1, Promise<'T1>> * U2<'T2, Promise<'T2>> -> Promise<U2<'T1, 'T2>>
        /// <summary>Creates a Promise that is resolved or rejected when any of the provided Promises are resolved
        /// or rejected.</summary>
        /// <param name="values">An array of Promises.</param>
        abstract race: values: ResizeArray<U2<'T, Promise<'T>>> -> Promise<'T>
        /// <summary>Creates a new rejected promise for the provided reason.</summary>
        /// <param name="reason">The reason the promise was rejected.</param>
        abstract reject: reason: obj option -> Promise<obj>
        /// <summary>Creates a new rejected promise for the provided reason.</summary>
        /// <param name="reason">The reason the promise was rejected.</param>
        abstract reject: reason: obj option -> Promise<'T>
        /// <summary>Creates a new resolved promise for the provided value.</summary>
        /// <param name="value">A promise.</param>
        abstract resolve: value: U2<'T, Promise<'T>> -> Promise<'T>
        /// Creates a new resolved promise.
        abstract resolve: unit -> Promise<unit>

    type [<AllowNullLiteral>] IPromiseConstructorStatic =
        /// <summary>Creates a new Promise.</summary>
        /// <param name="executor">A callback used to initialize the promise. This callback is passed two arguments:
        /// a resolve callback used resolve the promise with a value or the result of another promise,
        /// and a reject callback used to reject the promise with a provided reason or error.</param>
        [<Emit "new $0($1...)">] abstract Create: executor: ((U2<'T, Promise<'T>> -> unit) -> (obj -> unit) -> unit) -> IPromiseConstructor

    type [<StringEnum>] [<RequireQualifiedAccess>] StartupBehavior =
        | [<CompiledName "None">] None
        | [<CompiledName "Load">] Load

    type [<StringEnum>] [<RequireQualifiedAccess>] VisibilityMode =
        | [<CompiledName "Hidden">] Hidden
        | [<CompiledName "Taskpane">] Taskpane

    type AsyncResultStatus =
        obj

    type InitializationReason =
        obj

    type HostType =
        obj

    type PlatformType =
        obj

    /// An object which encapsulates the result of an asynchronous request, including status and error information if the request failed.
    /// 
    /// When the function you pass to the `callback` parameter of an "Async" method executes, it receives an AsyncResult object that you can access 
    /// from the `callback` function's only parameter.
    type [<AllowNullLiteral>] AsyncResult<'T> =
        /// Gets the user-defined item passed to the optional `asyncContext` parameter of the invoked method in the same state as it was passed in. 
        /// This returns the user-defined item (which can be of any JavaScript type: String, Number, Boolean, Object, Array, Null, or Undefined) passed 
        /// to the optional `asyncContext` parameter of the invoked method. Returns Undefined, if you didn't pass anything to the asyncContext parameter.
        abstract asyncContext: obj option with get, set
        /// Gets an object that may provide additional information if an {@link Office.Error | error} occurred.
        abstract diagnostics: obj option with get, set
        /// Gets an {@link Office.Error} object that provides a description of the error, if any error occurred.
        abstract error: Office.Error with get, set
        /// Gets the {@link Office.AsyncResultStatus} of the asynchronous operation.
        abstract status: AsyncResultStatus with get, set
        /// Gets the payload or content of this asynchronous operation, if any.
        abstract value: 'T with get, set

    /// Message used in the `onVisibilityModeChanged` invocation.
    type [<AllowNullLiteral>] VisibilityModeChangedMessage =
        /// Visibility changed state.
        abstract visibilityMode: Office.VisibilityMode with get, set

    /// Represents add-in level functionality for operating or configuring various aspects of the add-in.
    type [<AllowNullLiteral>] Addin =
        /// <summary>Sets the startup behavior for the add-in for when the document is opened next time.</summary>
        /// <param name="behavior">- Specifies startup behavior of the add-in.</param>
        abstract setStartupBehavior: behavior: Office.StartupBehavior -> Promise<unit>
        /// Gets the current startup behavior for the add-in.
        abstract getStartupBehavior: unit -> Promise<Office.StartupBehavior>
        /// Shows the task pane associated with the add-in.
        abstract showAsTaskpane: unit -> Promise<unit>
        /// Hides the task pane.
        abstract hide: unit -> Promise<unit>
        /// <summary>Adds a listener for the `onVisibilityModeChanged` event.</summary>
        /// <param name="listener">- The listener function that is called when the event is emitted. This function takes in a message for the receiving component.</param>
        abstract onVisibilityModeChanged: listener: (VisibilityModeChangedMessage -> unit) -> Promise<(unit -> Promise<unit>)>

    /// An interface that contains all the functionality provided to manage the state of the Office ribbon.
    type [<AllowNullLiteral>] Ribbon =
        /// <summary>Registers a custom contextual tab with Office and defines the tab's controls.</summary>
        /// <param name="tabDefinition">- Specifies the tab's properties and child controls and their properties. Pass a JSON string that conforms to the Office dynamic-ribbon JSON schema to `JSON.parse`, and then pass the returned object to this method.</param>
        abstract requestCreateControls: tabDefinition: Object -> Promise<unit>
        /// <summary>Sends a request to Office to update the ribbon.</summary>
        /// <param name="input">- Represents the updates to be made to the ribbon. Note that only the changes specified in the input parameter are made.</param>
        abstract requestUpdate: input: RibbonUpdaterData -> Promise<unit>

    /// Specifies changes to the ribbon, such as the enabled or disabled status of a button.
    type [<AllowNullLiteral>] RibbonUpdaterData =
        /// Collection of tabs whose state is set with the call of `requestUpdate`.
        abstract tabs: ResizeArray<Tab> with get, set

    /// Represents an individual tab and the state it should have. For code examples, see  {@link https://docs.microsoft.com/office/dev/add-ins/design/disable-add-in-commands | Enable and Disable Add-in Commands} and {@link https://docs.microsoft.com/office/dev/add-ins/design/contextual-tabs | Create custom contextual tabs}.
    type [<AllowNullLiteral>] Tab =
        /// Identifier of the tab as specified in the manifest.
        abstract id: string with get, set
        /// Specifies one or more of the controls in the tab, such as menu items, buttons, etc.
        abstract controls: ResizeArray<Control> option with get, set
        /// Specifies whether the tab is visible on the ribbon. Used only with contextual tabs.
        abstract visible: bool option with get, set
        /// Specifies one or more of the control groups on the tab.
        abstract groups: ResizeArray<Group> option with get, set

    /// Represents a group of controls on a ribbon tab.
    /// 
    /// **Requirement set**: Ribbon 1.1
    type [<AllowNullLiteral>] Group =
        /// Identifier of the group as specified in the manifest.
        abstract id: string with get, set
        /// Specifies one or more of the controls in the group, such as menu items, buttons, etc.
        abstract controls: ResizeArray<Control> option with get, set

    /// Represents an individual control or command and the state it should have.
    type [<AllowNullLiteral>] Control =
        /// Identifier of the control as specified in the manifest.
        abstract id: string with get, set
        /// Indicates whether the control should be enabled or disabled. The default is true.
        abstract enabled: bool option with get, set

    /// Represents the runtime environment of the add-in and provides access to key objects of the API. 
    /// The current context exists as a property of Office. It is accessed using `Office.context`.
    type [<AllowNullLiteral>] Context =
        /// True, if the current platform allows the add-in to display a UI for selling or upgrading; otherwise returns False.
        abstract commerceAllowed: bool with get, set
        /// Gets the locale (language) specified by the user for editing the document or item.
        abstract contentLanguage: string with get, set
        /// Gets information about the environment in which the add-in is running.
        abstract diagnostics: ContextInformation with get, set
        /// Gets the locale (language) specified by the user for the UI of the Office host application.
        abstract displayLanguage: string with get, set
        /// Gets an object that represents the document the content or task pane add-in is interacting with.
        abstract document: Office.Document with get, set
        /// Contains the Office application host in which the add-in is running.
        /// 
        /// **Important**: In Outlook, this property is available from requirement set 1.5.
        /// For all Mailbox requirement sets, you can use the `Office.context.diagnostics` property to get the host.
        abstract host: HostType with get, set
        /// Gets the license information for the user's Office installation.
        abstract license: string with get, set
        /// Provides access to the Microsoft Outlook Add-in object model.
        abstract mailbox: Office.Mailbox with get, set
        /// Provides access to the properties for Office theme colors.
        abstract officeTheme: OfficeTheme with get, set
        /// Provides the platform on which the add-in is running.
        /// 
        /// **Important**: In Outlook, this property is available from requirement set 1.5.
        /// For all Mailbox requirement sets, you can use the `Office.context.diagnostics` property to get the platform.
        abstract platform: PlatformType with get, set
        /// Provides a method for determining what requirement sets are supported on the current host and platform.
        abstract requirements: RequirementSetSupport with get, set
        /// Gets an object that represents the custom settings or state of a mail add-in saved to a user's mailbox.
        /// 
        /// The `RoamingSettings` object lets you store and access data for a mail add-in that is stored in a user's mailbox, so it's available to 
        /// that add-in when it is running from any host client application used to access that mailbox.
        abstract roamingSettings: Office.RoamingSettings with get, set
        /// Specifies whether the platform and device allows touch interaction. 
        /// True if the add-in is running on a touch device, such as an iPad; false otherwise.
        abstract touchEnabled: bool with get, set
        /// Provides objects and methods that you can use to create and manipulate UI components, such as dialog boxes.
        abstract ui: UI with get, set

    /// Provides specific information about an error that occurred during an asynchronous data operation.
    type [<AllowNullLiteral>] Error =
        /// Gets the numeric code of the error. For a list of error codes, see {@link https://docs.microsoft.com/office/dev/add-ins/reference/javascript-api-for-office-error-codes | JavaScript API for Office error codes}.
        abstract code: float with get, set
        /// Gets the name of the error.
        abstract message: string with get, set
        /// Gets a detailed description of the error.
        abstract name: string with get, set

    module AddinCommands =

        /// The `Event` object is passed as a parameter to add-in functions invoked by UI-less command buttons. The object allows the add-in to identify 
        /// which button was clicked and to signal the host that it has completed its processing.
        type [<AllowNullLiteral>] Event =
            /// Information about the control that triggered calling this function.
            abstract source: Source with get, set
            /// <summary>Indicates that the add-in has completed processing and will automatically be closed.
            /// 
            /// This method must be called at the end of a function which was invoked by the following.
            /// 
            /// - A UI-less button (i.e., an add-in command defined with an `Action` element where the `xsi:type` attribute is set to `ExecuteFunction`)
            /// 
            /// - An {@link https://docs.microsoft.com/office/dev/add-ins/reference/manifest/event | event} defined in the
            /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/manifest/extensionpoint#events | Events extension point},
            /// e.g., an `ItemSend` event
            /// 
            /// [Api set: Mailbox 1.3]</summary>
            /// <param name="options">Optional. An object that specifies behavior options for when the event is completed.</param>
            abstract completed: ?options: EventCompletedOptions -> unit

        /// Specifies the behavior for when the event is completed.
        type [<AllowNullLiteral>] EventCompletedOptions =
            /// A boolean value. When the completed method is used to signal completion of an event handler, 
            /// this value indicates if the handled event should continue execution or be canceled. 
            /// For example, an add-in that handles the `ItemSend` event can set `allowEvent` to `false` to cancel sending of the message.
            abstract allowEvent: bool with get, set

        /// Encapsulates source data for add-in events.
        type [<AllowNullLiteral>] Source =
            /// The ID of the control that triggered calling this function. The ID comes from the manifest.
            abstract id: string with get, set

    /// Provides objects and methods that you can use to create and manipulate UI components, such as dialog boxes, in your Office Add-ins.
    /// 
    /// Visit "{@link https://docs.microsoft.com/office/dev/add-ins/develop/dialog-api-in-office-add-ins | Use the Dialog API in your Office Add-ins}" 
    /// for more information.
    type [<AllowNullLiteral>] UI =
        /// <summary>Adds an event handler to the object using the specified event type.</summary>
        /// <param name="eventType">Specifies the type of event to add. This must be `Office.EventType.DialogParentMessageReceived`.</param>
        /// <param name="handler">The event handler function to add, whose only parameter is of type {@link Office.DialogParentMessageReceivedEventArgs}.</param>
        /// <param name="options">Optional. Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the handler registration returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addHandlerAsync: eventType: Office.EventType * handler: (DialogParentMessageReceivedEventArgs -> unit) * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a dialog to show or collect information from the user or to facilitate Web navigation.</summary>
        /// <param name="startAddress">- Accepts the initial full HTTPS URL that opens in the dialog. Relative URLs must not be used.</param>
        /// <param name="options">- Optional. Accepts an {@link Office.DialogOptions} object to define dialog display.</param>
        /// <param name="callback">- Optional. Accepts a callback method to handle the dialog creation attempt. If successful, the AsyncResult.value is a Dialog object.</param>
        abstract displayDialogAsync: startAddress: string * ?options: DialogOptions * ?callback: (AsyncResult<Dialog> -> unit) -> unit
        /// <summary>Displays a dialog to show or collect information from the user or to facilitate Web navigation.</summary>
        /// <param name="startAddress">- Accepts the initial full HTTPS URL that opens in the dialog. Relative URLs must not be used.</param>
        /// <param name="callback">- Optional. Accepts a callback method to handle the dialog creation attempt. If successful, the AsyncResult.value is a Dialog object.</param>
        abstract displayDialogAsync: startAddress: string * ?callback: (AsyncResult<Dialog> -> unit) -> unit
        /// <summary>Delivers a message from the dialog box to its parent/opener page.</summary>
        /// <param name="message">Accepts a message from the dialog to deliver to the add-in. Anything that can serialized to a string including JSON and XML can be sent.</param>
        /// <param name="messageOptions">Optional. Provides options for how to send the message.</param>
        abstract messageParent: message: string * ?messageOptions: DialogMessageOptions -> unit
        /// Closes the UI container where the JavaScript is executing.
        abstract closeContainer: unit -> unit
        /// <summary>Opens a browser window and loads the specified URL.</summary>
        /// <param name="url">The full URL to be opened including protocol (e.g., https), and port number, if any.</param>
        abstract openBrowserWindow: url: string -> unit

    /// Provides information about which Requirement Sets are supported in the current environment.
    type [<AllowNullLiteral>] RequirementSetSupport =
        /// <summary>Check if the specified requirement set is supported by the host Office application.</summary>
        /// <param name="name">- The requirement set name (e.g., "ExcelApi").</param>
        /// <param name="minVersion">- The minimum required version (e.g., "1.4").</param>
        abstract isSetSupported: name: string * ?minVersion: string -> bool
        /// <summary>Check if the specified requirement set is supported by the host Office application.
        /// 
        /// **Warning**: This overload of `isSetSupported` (where `minVersionNumber` is a number) has been deprecated. Use the string overload of `isSetSupported` instead.</summary>
        /// <param name="name">- The requirement set name (e.g., "ExcelApi").</param>
        /// <param name="minVersionNumber">- The minimum required version (e.g., 1.4).</param>
        abstract isSetSupported: name: string * ?minVersionNumber: float -> bool

    /// Provides options for how a dialog is displayed.
    type [<AllowNullLiteral>] DialogOptions =
        /// Defines the height of the dialog as a percentage of the current display. Defaults to 80%. 250px minimum.
        abstract height: float option with get, set
        /// Defines the width of the dialog as a percentage of the current display. Defaults to 80%. 150px minimum.
        abstract width: float option with get, set
        /// Determines whether the dialog box should be displayed within an IFrame. This setting is only applicable in Office on the web, and is 
        /// ignored by other platforms. If false (default), the dialog will be displayed as a new browser window (pop-up). Recommended for 
        /// authentication pages that cannot be displayed in an IFrame. If true, the dialog will be displayed as a floating overlay with an IFrame. 
        /// This is best for user experience and performance.
        abstract displayInIframe: bool option with get, set
        /// Determines if the pop-up blocker dialog will be shown to the user. Defaults to true.
        /// 
        /// `true` - The framework displays a pop-up to trigger the navigation and avoid the browser's pop-up blocker.
        /// `false` - The dialog will not be shown and the developer must handle pop-ups (by providing a user interface artifact to trigger the navigation).
        abstract promptBeforeOpen: bool option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// The Office Auth namespace, `Office.context.auth`, provides a method that allows the Office client application to obtain an access token to the add-in's web application.
    /// Indirectly, this also enables the add-in to access the signed-in user's Microsoft Graph data without requiring the user to sign in a second time.
    type [<AllowNullLiteral>] Auth =
        /// <summary>Calls the Azure Active Directory V 2.0 endpoint to get an access token to your add-in's web application. Enables add-ins to identify users.
        /// Server-side code can use this token to access Microsoft Graph for the add-in's web application by using the
        /// {@link https://docs.microsoft.com/azure/active-directory/develop/active-directory-v2-protocols-oauth-on-behalf-of | "on behalf of" OAuth flow}.
        /// 
        /// **Important**: In Outlook, this API is not supported if the add-in is loaded in an Outlook.com or Gmail mailbox.
        /// 
        /// **Warning**: `getAccessTokenAsync` has been deprecated. Use `Office.auth.getAccessToken` instead.</summary>
        /// <param name="options">- Optional. Accepts an `AuthOptions` object to define sign-on behaviors.</param>
        /// <param name="callback">- Optional. Accepts a callback method that can parse the token for the user's ID or use the token in the "on behalf of" flow to get access to Microsoft Graph.
        ///   If `AsyncResult.status` is "succeeded", then `AsyncResult.value` is the raw AAD v. 2.0-formatted access token.</param>
        abstract getAccessTokenAsync: ?options: AuthOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Calls the Azure Active Directory V 2.0 endpoint to get an access token to your add-in's web application. Enables add-ins to identify users.
        /// Server-side code can use this token to access Microsoft Graph for the add-in's web application by using the
        /// {@link https://docs.microsoft.com/azure/active-directory/develop/active-directory-v2-protocols-oauth-on-behalf-of | "on behalf of" OAuth flow}.
        /// 
        /// **Important**: In Outlook, this API is not supported if the add-in is loaded in an Outlook.com or Gmail mailbox.
        /// 
        /// **Warning**: `getAccessTokenAsync` has been deprecated. Use `Office.auth.getAccessToken` instead.</summary>
        /// <param name="callback">- Optional. Accepts a callback method that can parse the token for the user's ID or use the token in the "on behalf of" flow to get access to Microsoft Graph.
        ///   If `AsyncResult.status` is "succeeded", then `AsyncResult.value` is the raw AAD v. 2.0-formatted access token.</param>
        abstract getAccessTokenAsync: ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Calls the Azure Active Directory V 2.0 endpoint to get an access token to your add-in's web application. Enables add-ins to identify users.
        /// Server-side code can use this token to access Microsoft Graph for the add-in's web application by using the
        /// {@link https://docs.microsoft.com/azure/active-directory/develop/active-directory-v2-protocols-oauth-on-behalf-of | "on behalf of" OAuth flow}. 
        /// This API requires a single sign-on configuration that bridges the add-in to an Azure application. Office users sign in with Organizational
        /// Accounts and Microsoft Accounts. Microsoft Azure returns tokens intended for both user account types to access resources in the Microsoft Graph.</summary>
        /// <param name="options">- Optional. Accepts an `AuthOptions` object to define sign-on behaviors.</param>
        abstract getAccessToken: ?options: AuthOptions -> Promise<string>

    /// Provides options for the user experience when Office obtains an access token to the add-in from AAD v. 2.0 with the `getAccessToken` method.
    type [<AllowNullLiteral>] AuthOptions =
        /// Allows Office to get an access token silently or through interactive consent, if one is required. Default value is `false`.
        /// If set to `false`, Office will silently try to get an access token. If it fails to do so, Office will return a descriptive error.
        /// If set to `true`, Office will show an interactive consent UI after it fails to silently get an access token.
        /// The prompt will only allow consent to the AAD profile scope, not to any Microsoft Graph scopes.
        abstract allowConsentPrompt: bool option with get, set
        /// Allows Office to get an access token silently provided consent is present or show interactive UI to sign in the user. Default value is `false`.
        /// If set to `false`, Office will silently try to get an access token. If it fails to do so, Office will return a descriptive error.
        /// If set to `true`, Office will show an interactive sign-in UI after it fails to silently get an access token.
        abstract allowSignInPrompt: bool option with get, set
        /// Prompts the user to add their Office account (or to switch to it, if it is already added). Default value is `false`.
        /// 
        /// **Warning**: `forceAddAccount` has been deprecated. Use `allowSignInPrompt` instead.
        abstract forceAddAccount: bool option with get, set
        /// Causes Office to display the add-in consent experience. Useful if the add-in's Azure permissions have changed or if the user's consent has
        /// been revoked. Default value is `false`.
        /// 
        /// **Warning**: `forceConsent` has been deprecated. Use `allowConsentPrompt` instead.
        abstract forceConsent: bool option with get, set
        /// Causes Office to prompt the user to provide the additional factor when the tenancy being targeted by Microsoft Graph requires multifactor
        /// authentication. The string value identifies the type of additional factor that is required. In most cases, you won't know at development
        /// time whether the user's tenant requires an additional factor or what the string should be. So this option would be used in a "second try"
        /// call of `getAccessToken` after Microsoft Graph has sent an error requesting the additional factor and containing the string that should
        /// be used with the `authChallenge` option.
        abstract authChallenge: string option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the `asyncContext` property of the `AsyncResult` object that is passed to a callback.
        abstract asyncContext: obj option with get, set
        /// Causes Office to return a descriptive error when the add-in wants to access Microsoft Graph and the user/admin has not granted consent to Graph scopes. Default value is `false`.
        /// Office only supports consent to Graph scopes when the add-in has been deployed by a tenant admin. This information will not be available during development.
        /// Setting this option to `true` will cause Office to inform your add-in beforehand (by returning a descriptive error) if Graph access will fail.
        abstract forMSGraphAccess: bool option with get, set

    /// Provides an option for preserving context data of any type, unchanged, for use in a callback.
    type [<AllowNullLiteral>] AsyncContextOptions =
        /// A user-defined item of any type that is returned, unchanged, in the `asyncContext` property of the `AsyncResult` object
        /// that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides information about the environment in which the add-in is running.
    type [<AllowNullLiteral>] ContextInformation =
        /// Gets the Office application host in which the add-in is running.
        abstract host: Office.HostType with get, set
        /// Gets the platform on which the add-in is running.
        abstract platform: Office.PlatformType with get, set
        /// Gets the version of Office on which the add-in is running.
        abstract version: string with get, set

    /// Provides options for how to get the data in a binding.
    type [<AllowNullLiteral>] GetBindingDataOptions =
        /// The expected shape of the selection. Use {@link Office.CoercionType} or text value. Default: The original, uncoerced type of the binding.
        abstract coercionType: U2<Office.CoercionType, string> option with get, set
        /// Specifies whether values, such as numbers and dates, are returned with their formatting applied. Use Office.ValueFormat or text value. 
        /// Default: Unformatted data.
        abstract valueFormat: U2<Office.ValueFormat, string> option with get, set
        /// For table or matrix bindings, specifies the zero-based starting row for a subset of the data in the binding. Default is first row.
        abstract startRow: float option with get, set
        /// For table or matrix bindings, specifies the zero-based starting column for a subset of the data in the binding. Default is first column.
        abstract startColumn: float option with get, set
        /// For table or matrix bindings, specifies the number of rows offset from the startRow. Default is all subsequent rows.
        abstract rowCount: float option with get, set
        /// For table or matrix bindings, specifies the number of columns offset from the startColumn. Default is all subsequent columns.
        abstract columnCount: float option with get, set
        /// Specify whether to get only the visible (filtered in) data or all the data (default is all). Useful when filtering data. 
        /// Use Office.FilterType or text value.
        abstract filterType: U2<Office.FilterType, string> option with get, set
        /// Only for table bindings in content add-ins for Access. Specifies the pre-defined string "thisRow" to get data in the currently selected row.
        /// 
        /// **Important**: We no longer recommend that you create and use Access web apps and databases in SharePoint.
        /// As an alternative, we recommend that you use {@link https://powerapps.microsoft.com/ | Microsoft PowerApps}
        /// to build no-code business solutions for web and mobile devices.
        abstract rows: string option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides options for how to set the data in a binding.
    type [<AllowNullLiteral>] SetBindingDataOptions =
        /// Use only with binding type table and when a TableData object is passed for the data parameter. An array of objects that specify a range of 
        /// columns, rows, or cells and specify, as key-value pairs, the cell formatting to apply to that range. 
        /// 
        /// Example: `[{cells: Office.Table.Data, format: {fontColor: "yellow"}}, {cells: {row: 3, column: 4}, format: {borderColor: "white", fontStyle: "bold"}}]`
        abstract cellFormat: ResizeArray<RangeFormatConfiguration> option with get, set
        /// Explicitly sets the shape of the data object. If not supplied is inferred from the data type.
        abstract coercionType: U2<Office.CoercionType, string> option with get, set
        /// Only for table bindings in content add-ins for Access. Array of strings. Specifies the column names.
        /// 
        /// **Important**: We no longer recommend that you create and use Access web apps and databases in SharePoint.
        /// As an alternative, we recommend that you use {@link https://powerapps.microsoft.com/ | Microsoft PowerApps}
        /// to build no-code business solutions for web and mobile devices.
        abstract columns: ResizeArray<string> option with get, set
        /// Only for table bindings in content add-ins for Access. Specifies the pre-defined string "thisRow" to get data in the currently selected row.
        /// 
        /// **Important**: We no longer recommend that you create and use Access web apps and databases in SharePoint.
        /// As an alternative, we recommend that you use {@link https://powerapps.microsoft.com/ | Microsoft PowerApps}
        /// to build no-code business solutions for web and mobile devices.
        abstract rows: string option with get, set
        /// Specifies the zero-based starting row for a subset of the data in the binding. Only for table or matrix bindings. If omitted, data is set 
        /// starting in the first row.
        abstract startRow: float option with get, set
        /// Specifies the zero-based starting column for a subset of the data. Only for table or matrix bindings. If omitted, data is set starting in 
        /// the first column.
        abstract startColumn: float option with get, set
        /// For an inserted table, a list of key-value pairs that specify table formatting options, such as header row, total row, and banded rows. 
        /// Example: `{bandedRows: true,  filterButton: false}`
        abstract tableOptions: obj option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Specifies a range and its formatting.
    type [<AllowNullLiteral>] RangeFormatConfiguration =
        /// Specifies the range. Example of using Office.Table enum: Office.Table.All. Example of using RangeCoordinates: `{row: 3, column: 4}` specifies 
        /// the cell in the 3rd (zero-based) row in the 4th (zero-based) column.
        abstract cells: U2<Office.Table, RangeCoordinates> with get, set
        /// Specifies the formatting as key-value pairs. Example: `{borderColor: "white", fontStyle: "bold"}`
        abstract format: obj with get, set

    /// Specifies a cell, or row, or column, by its zero-based row and/or column number. Example: `{row: 3, column: 4}` specifies the cell in the 3rd 
    /// (zero-based) row in the 4th (zero-based) column.
    type [<AllowNullLiteral>] RangeCoordinates =
        /// The zero-based row of the range. If not specified, all cells, in the column specified by `column` are included.
        abstract row: float option with get, set
        /// The zero-based column of the range. If not specified, all cells, in the row specified by `row` are included.
        abstract column: float option with get, set

    /// Provides options to determine which event handler or handlers are removed.
    type [<AllowNullLiteral>] RemoveHandlerOptions =
        /// The handler to be removed. If a particular handler is not specified, then all handlers for the specified event type are removed.
        abstract handler: (U2<Office.BindingDataChangedEventArgs, Office.BindingSelectionChangedEventArgs> -> obj option) option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides options for configuring the binding that is created.
    type [<AllowNullLiteral>] AddBindingFromNamedItemOptions =
        /// The unique ID of the binding. Autogenerated if not supplied.
        abstract id: string option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides options for configuring the prompt and identifying the binding that is created.
    type [<AllowNullLiteral>] AddBindingFromPromptOptions =
        /// The unique ID of the binding. Autogenerated if not supplied.
        abstract id: string option with get, set
        /// Specifies the string to display in the prompt UI that tells the user what to select. Limited to 200 characters. 
        /// If no promptText argument is passed, "Please make a selection" is displayed.
        abstract promptText: string option with get, set
        /// Specifies a table of sample data displayed in the prompt UI as an example of the kinds of fields (columns) that can be bound by your add-in. 
        /// The headers provided in the TableData object specify the labels used in the field selection UI.
        /// 
        /// **Note**: This parameter is used only in add-ins for Access. It is ignored if provided when calling the method in an add-in for Excel.
        /// 
        /// **Important**: We no longer recommend that you create and use Access web apps and databases in SharePoint.
        /// As an alternative, we recommend that you use {@link https://powerapps.microsoft.com/ | Microsoft PowerApps}
        /// to build no-code business solutions for web and mobile devices.
        abstract sampleData: Office.TableData option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides options for identifying the binding that is created.
    type [<AllowNullLiteral>] AddBindingFromSelectionOptions =
        /// The unique ID of the binding. Autogenerated if not supplied.
        abstract id: string option with get, set
        /// The names of the columns involved in the binding.
        abstract columns: ResizeArray<string> option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides options for setting the size of slices that the document will be divided into.
    type [<AllowNullLiteral>] GetFileOptions =
        /// The the size of the slices in bytes. The maximum (and the default) is 4194304 (4MB).
        abstract sliceSize: float option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides options for customizing what data is returned and how it is formatted.
    type [<AllowNullLiteral>] GetSelectedDataOptions =
        /// Specify whether the data is formatted. Use Office.ValueFormat or string equivalent.
        abstract valueFormat: U2<Office.ValueFormat, string> option with get, set
        /// Specify whether to get only the visible (that is, filtered-in) data or all the data. Useful when filtering data. 
        /// Use {@link Office.FilterType} or string equivalent. This parameter is ignored in Word documents.
        abstract filterType: U2<Office.FilterType, string> option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides options for whether to select the location that is navigated to.
    type [<AllowNullLiteral>] GoToByIdOptions =
        /// Specifies whether the location specified by the id parameter is selected (highlighted). 
        /// Use {@link Office.SelectionMode} or string equivalent. See the Remarks for more information.
        abstract selectionMode: U2<Office.SelectionMode, string> option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides options for how to insert data to the selection.
    type [<AllowNullLiteral>] SetSelectedDataOptions =
        /// Use only with binding type table and when a TableData object is passed for the data parameter. An array of objects that specify a range of 
        /// columns, rows, or cells and specify, as key-value pairs, the cell formatting to apply to that range. 
        /// 
        /// Example: `[{cells: Office.Table.Data, format: {fontColor: "yellow"}}, {cells: {row: 3, column: 4}, format: {borderColor: "white", fontStyle: "bold"}}]`
        abstract cellFormat: ResizeArray<RangeFormatConfiguration> option with get, set
        /// Explicitly sets the shape of the data object. If not supplied is inferred from the data type.
        abstract coercionType: U2<Office.CoercionType, string> option with get, set
        /// For an inserted table, a list of key-value pairs that specify table formatting options, such as header row, total row, and banded rows. 
        /// Example: `{bandedRows: true,  filterButton: false}`
        abstract tableOptions: obj option with get, set
        /// This option is applicable for inserting images. Indicates the insert location in relation to the top of the slide for PowerPoint, and its 
        /// relation to the currently selected cell in Excel. This value is ignored for Word. This value is in points.
        abstract imageTop: float option with get, set
        /// This option is applicable for inserting images. Indicates the image width. If this option is provided without the imageHeight, the image 
        /// will scale to match the value of the image width. If both image width and image height are provided, the image will be resized accordingly. 
        /// If neither the image height or width is provided, the default image size and aspect ratio will be used. This value is in points.
        abstract imageWidth: float option with get, set
        /// This option is applicable for inserting images. Indicates the insert location in relation to the left side of the slide for PowerPoint, and 
        /// its relation to the currently selected cell in Excel. This value is ignored for Word. This value is in points.
        abstract imageLeft: float option with get, set
        /// This option is applicable for inserting images. Indicates the image height. If this option is provided without the imageWidth, the image 
        /// will scale to match the value of the image height. If both image width and image height are provided, the image will be resized accordingly. 
        /// If neither the image height or width is provided, the default image size and aspect ratio will be used. This value is in points.
        abstract imageHeight: float option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides options for saving settings.
    type [<AllowNullLiteral>] SaveSettingsOptions =
        /// **Warning**: This setting has been deprecated and should not be used. It has no effect on most platforms and will cause errors if set to `false` in Excel on the web.
        abstract overwriteIfStale: bool option with get, set
        /// A user-defined item of any type that is returned, unchanged, in the asyncContext property of the AsyncResult object that is passed to a callback.
        abstract asyncContext: obj option with get, set

    /// Provides access to the properties for Office theme colors.
    /// 
    /// Using Office theme colors lets you coordinate the color scheme of your add-in with the current Office theme selected by the user with File \> 
    /// Office Account \> Office Theme UI, which is applied across all Office host applications. Using Office theme colors is appropriate for mail and 
    /// task pane add-ins.
    type [<AllowNullLiteral>] OfficeTheme =
        /// Gets the Office theme body background color as a hexadecimal color triplet (e.g., "FFA500").
        abstract bodyBackgroundColor: string with get, set
        /// Gets the Office theme body foreground color as a hexadecimal color triplet (e.g., "FFA500").
        abstract bodyForegroundColor: string with get, set
        /// Gets the Office theme control background color as a hexadecimal color triplet (e.g., "FFA500").
        abstract controlBackgroundColor: string with get, set
        /// Gets the Office theme control foreground color as a hexadecimal color triplet (e.g., "FFA500").
        abstract controlForegroundColor: string with get, set

    /// The object that is returned when `UI.displayDialogAsync` is called. It exposes methods for registering event handlers and closing the dialog.
    type [<AllowNullLiteral>] Dialog =
        /// Called from a parent page to close the corresponding dialog box. 
        /// 
        /// This method is asynchronous. It does not take a callback parameter and it does not return a Promise object, so it cannot be awaited with either the `await` keyword or the `then` function. See this best practice for more information: {@link https://docs.microsoft.com/office/dev/add-ins/develop/dialog-best-practices#opening-another-dialog-immediately-after-closing-one | Opening another dialog immediately after closing one}
        abstract close: unit -> unit
        /// <summary>Registers an event handler. The two supported events are:
        /// 
        /// - DialogMessageReceived. Triggered when the dialog box sends a message to its parent.
        /// 
        /// - DialogEventReceived. Triggered when the dialog box has been closed or otherwise unloaded.</summary>
        /// <param name="eventType">Must be either DialogMessageReceived or DialogEventReceived.</param>
        /// <param name="handler">A function which accepts either an object with a `message` and `origin` property, if `eventType` is `DialogMessageReceived`, or an object with an `error` property, if `eventType` is `DialogEventReceived`. Note that the `origin` property is `undefined` on clients that don’t support {@link https://docs.microsoft.com/office/dev/add-ins/reference/requirement-sets/dialog-origin-requirement-sets | DialogOrigin 1.1}.</param>
        abstract addEventHandler: eventType: Office.EventType * handler: (U2<DialogAddEventHandler, DialogAddEventHandler2> -> unit) -> unit
        /// <summary>Delivers a message from the host page, such as a task pane or a UI-less function file, to a dialog that was opened from the page.</summary>
        /// <param name="message">Accepts a message from the host page to deliver to the dialog. Anything that can be serialized to a string, including JSON and XML, can be sent.</param>
        /// <param name="messageOptions">Optional. Provides options for how to send the message.</param>
        abstract messageChild: message: string * ?messageOptions: DialogMessageOptions -> unit
        /// FOR INTERNAL USE ONLY. DO NOT CALL IN YOUR CODE.
        abstract sendMessage: name: string -> unit

    type ActiveView =
        obj

    type BindingType =
        obj

    type CoercionType =
        obj

    type DocumentMode =
        obj

    type CustomXMLNodeType =
        obj

    type EventType =
        obj

    type FileType =
        obj

    type FilterType =
        obj

    type GoToType =
        obj

    type Index =
        obj

    type SelectionMode =
        obj

    type ValueFormat =
        obj

    /// Represents a binding to a section of the document.
    /// 
    /// The Binding object exposes the functionality possessed by all bindings regardless of type.
    /// 
    /// The Binding object is never called directly. It is the abstract parent class of the objects that represent each type of binding: 
    /// {@link Office.MatrixBinding}, {@link Office.TableBinding}, or {@link Office.TextBinding}. All three of these objects inherit the getDataAsync 
    /// and setDataAsync methods from the Binding object that enable to you interact with the data in the binding. They also inherit the id and type 
    /// properties for querying those property values. Additionally, the MatrixBinding and TableBinding objects expose additional methods for matrix- 
    /// and table-specific features, such as counting the number of rows and columns.
    type [<AllowNullLiteral>] Binding =
        /// Get the Document object associated with the binding.
        abstract document: Office.Document with get, set
        /// A string that uniquely identifies this binding among the bindings in the same {@link Office.Document} object.
        abstract id: string with get, set
        /// Gets the type of the binding.
        abstract ``type``: Office.BindingType with get, set
        /// <summary>Adds an event handler to the object for the specified {@link Office.EventType}. Supported EventTypes are 
        /// `Office.EventType.BindingDataChanged` and `Office.EventType.BindingSelectionChanged`.</summary>
        /// <param name="eventType">The event type. For bindings, it can be `Office.EventType.BindingDataChanged` or `Office.EventType.BindingSelectionChanged`.</param>
        /// <param name="handler">The event handler function to add, whose only parameter is of type {@link Office.BindingDataChangedEventArgs} or {@link Office.BindingSelectionChangedEventArgs}.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addHandlerAsync: eventType: Office.EventType * handler: obj option * ?options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an event handler to the object for the specified {@link Office.EventType}. Supported EventTypes are 
        /// `Office.EventType.BindingDataChanged` and `Office.EventType.BindingSelectionChanged`.</summary>
        /// <param name="eventType">The event type. For bindings, it can be `Office.EventType.BindingDataChanged` or `Office.EventType.BindingSelectionChanged`.</param>
        /// <param name="handler">The event handler function to add, whose only parameter is of type {@link Office.BindingDataChangedEventArgs} or {@link Office.BindingSelectionChangedEventArgs}.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addHandlerAsync: eventType: Office.EventType * handler: obj option * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Returns the data contained within the binding.</summary>
        /// <param name="options">Provides options for how to get the data in a binding.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the values in the specified binding. 
        ///  If the `coercionType` parameter is specified (and the call is successful), the data is returned in the format described in the CoercionType enumeration topic.</param>
        abstract getDataAsync: ?options: GetBindingDataOptions * ?callback: (AsyncResult<'T> -> unit) -> unit
        /// <summary>Returns the data contained within the binding.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the values in the specified binding. 
        ///  If the `coercionType` parameter is specified (and the call is successful), the data is returned in the format described in the CoercionType enumeration topic.</param>
        abstract getDataAsync: ?callback: (AsyncResult<'T> -> unit) -> unit
        /// <summary>Removes the specified handler from the binding for the specified event type.</summary>
        /// <param name="eventType">The event type. For bindings, it can be `Office.EventType.BindingDataChanged` or `Office.EventType.BindingSelectionChanged`.</param>
        /// <param name="options">Provides options to determine which event handler or handlers are removed.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract removeHandlerAsync: eventType: Office.EventType * ?options: RemoveHandlerOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes the specified handler from the binding for the specified event type.</summary>
        /// <param name="eventType">The event type. For bindings, it can be `Office.EventType.BindingDataChanged` or `Office.EventType.BindingSelectionChanged`.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract removeHandlerAsync: eventType: Office.EventType * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Writes data to the bound section of the document represented by the specified binding object.</summary>
        /// <param name="data">The data to be set in the current selection. Possible data types by host:
        /// 
        /// string: Excel on the web and Windows, and Word on the web and Windows only
        /// 
        /// array of arrays: Excel and Word only
        /// 
        /// {@link Office.TableData}: Excel and Word only
        /// 
        /// HTML: Word on the web and Windows only
        /// 
        /// Office Open XML: Word only</param>
        /// <param name="options">Provides options for how to set the data in a binding.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setDataAsync: data: U2<TableData, obj option> * ?options: SetBindingDataOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Writes data to the bound section of the document represented by the specified binding object.</summary>
        /// <param name="data">The data to be set in the current selection. Possible data types by host:
        /// 
        /// string: Excel on the web and Windows, and Word on the web and Windows only
        /// 
        /// array of arrays: Excel and Word only
        /// 
        /// `TableData`: Excel and Word only
        /// 
        /// HTML: Word on the web and Windows only
        /// 
        /// Office Open XML: Word only</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setDataAsync: data: U2<TableData, obj option> * ?callback: (AsyncResult<unit> -> unit) -> unit

    /// Provides information about the binding that raised the DataChanged event.
    type [<AllowNullLiteral>] BindingDataChangedEventArgs =
        /// Gets an {@link Office.Binding} object that represents the binding that raised the DataChanged event.
        abstract binding: Binding with get, set
        /// Gets an {@link Office.EventType} enumeration value that identifies the kind of event that was raised.
        abstract ``type``: EventType with get, set

    /// Provides information about the binding that raised the SelectionChanged event.
    type [<AllowNullLiteral>] BindingSelectionChangedEventArgs =
        /// Gets an {@link Office.Binding} object that represents the binding that raised the SelectionChanged event.
        abstract binding: Binding with get, set
        /// Gets the number of columns selected. If a single cell is selected returns 1. 
        /// 
        /// If the user makes a non-contiguous selection, the count for the last contiguous selection within the binding is returned. 
        /// 
        /// For Word, this property will work only for bindings of {@link Office.BindingType} "table". If the binding is of type "matrix", null is 
        /// returned. Also, the call will fail if the table contains merged cells, because the structure of the table must be uniform for this property 
        /// to work correctly.
        abstract columnCount: float with get, set
        /// Gets the number of rows selected. If a single cell is selected returns 1. 
        /// 
        /// If the user makes a non-contiguous selection, the count for the last contiguous selection within the binding is returned. 
        /// 
        /// For Word, this property will work only for bindings of {@link Office.BindingType} "table". If the binding is of type "matrix", null is 
        /// returned. Also, the call will fail if the table contains merged cells, because the structure of the table must be uniform for this property 
        /// to work correctly.
        abstract rowCount: float with get, set
        /// The zero-based index of the first column of the selection counting from the leftmost column in the binding. 
        /// 
        /// If the user makes a non-contiguous selection, the coordinates for the last contiguous selection within the binding are returned. 
        /// 
        /// For Word, this property will work only for bindings of {@link Office.BindingType} "table". If the binding is of type "matrix", null is 
        /// returned. Also, the call will fail if the table contains merged cells, because the structure of the table must be uniform for this property 
        /// to work correctly.
        abstract startColumn: float with get, set
        /// The zero-based index of the first row of the selection counting from the first row in the binding. 
        /// 
        /// If the user makes a non-contiguous selection, the coordinates for the last contiguous selection within the binding are returned. 
        /// 
        /// For Word, this property will work only for bindings of {@link Office.BindingType} "table". If the binding is of type "matrix", null is 
        /// returned. Also, the call will fail if the table contains merged cells, because the structure of the table must be uniform for this property 
        /// to work correctly.
        abstract startRow: float with get, set
        /// Gets an {@link Office.EventType} enumeration value that identifies the kind of event that was raised.
        abstract ``type``: EventType with get, set

    /// Represents the bindings the add-in has within the document.
    type [<AllowNullLiteral>] Bindings =
        /// Gets an {@link Office.Document} object that represents the document associated with this set of bindings.
        abstract document: Document with get, set
        /// <summary>Creates a binding against a named object in the document.</summary>
        /// <param name="itemName">Name of the bindable object in the document. For Example 'MyExpenses' table in Excel."</param>
        /// <param name="bindingType">The {@link Office.BindingType} for the data. The method returns null if the selected object cannot be coerced into the specified type.</param>
        /// <param name="options">Provides options for configuring the binding that is created.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the Binding object that represents the specified named item.</param>
        abstract addFromNamedItemAsync: itemName: string * bindingType: BindingType * ?options: AddBindingFromNamedItemOptions * ?callback: (AsyncResult<Binding> -> unit) -> unit
        /// <summary>Creates a binding against a named object in the document.</summary>
        /// <param name="itemName">Name of the bindable object in the document. For Example 'MyExpenses' table in Excel."</param>
        /// <param name="bindingType">The {@link Office.BindingType} for the data. The method returns null if the selected object cannot be coerced into the specified type.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the Binding object that represents the specified named item.</param>
        abstract addFromNamedItemAsync: itemName: string * bindingType: BindingType * ?callback: (AsyncResult<Binding> -> unit) -> unit
        /// <summary>Create a binding by prompting the user to make a selection on the document.</summary>
        /// <param name="bindingType">Specifies the type of the binding object to create. Required. 
        /// Returns null if the selected object cannot be coerced into the specified type.</param>
        /// <param name="options">Provides options for configuring the prompt and identifying the binding that is created.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the Binding object that represents the selection specified by the user.</param>
        abstract addFromPromptAsync: bindingType: BindingType * ?options: AddBindingFromPromptOptions * ?callback: (AsyncResult<Binding> -> unit) -> unit
        /// <summary>Create a binding by prompting the user to make a selection on the document.</summary>
        /// <param name="bindingType">Specifies the type of the binding object to create. Required. 
        /// Returns null if the selected object cannot be coerced into the specified type.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the Binding object that represents the selection specified by the user.</param>
        abstract addFromPromptAsync: bindingType: BindingType * ?callback: (AsyncResult<Binding> -> unit) -> unit
        /// <summary>Create a binding based on the user's current selection.</summary>
        /// <param name="bindingType">Specifies the type of the binding object to create. Required. 
        /// Returns null if the selected object cannot be coerced into the specified type.</param>
        /// <param name="options">Provides options for identifying the binding that is created.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the Binding object that represents the selection specified by the user.</param>
        abstract addFromSelectionAsync: bindingType: BindingType * ?options: AddBindingFromSelectionOptions * ?callback: (AsyncResult<Binding> -> unit) -> unit
        /// <summary>Create a binding based on the user's current selection.</summary>
        /// <param name="bindingType">Specifies the type of the binding object to create. Required. 
        /// Returns null if the selected object cannot be coerced into the specified type.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the Binding object that represents the selection specified by the user.</param>
        abstract addFromSelectionAsync: bindingType: BindingType * ?callback: (AsyncResult<Binding> -> unit) -> unit
        /// <summary>Gets all bindings that were previously created.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an array that contains each binding created for the referenced Bindings object.</param>
        abstract getAllAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<ResizeArray<Binding>> -> unit) -> unit
        /// <summary>Gets all bindings that were previously created.</summary>
        /// <param name="callback">A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an array that contains each binding created for the referenced Bindings object.</param>
        abstract getAllAsync: ?callback: (AsyncResult<ResizeArray<Binding>> -> unit) -> unit
        /// <summary>Retrieves a binding based on its Name</summary>
        /// <param name="id">Specifies the unique name of the binding object. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the Binding object specified by the id in the call.</param>
        abstract getByIdAsync: id: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<Binding> -> unit) -> unit
        /// <summary>Retrieves a binding based on its Name</summary>
        /// <param name="id">Specifies the unique name of the binding object. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the Binding object specified by the id in the call.</param>
        abstract getByIdAsync: id: string * ?callback: (AsyncResult<Binding> -> unit) -> unit
        /// <summary>Removes the binding from the document</summary>
        /// <param name="id">Specifies the unique name to be used to identify the binding object. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract releaseByIdAsync: id: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes the binding from the document</summary>
        /// <param name="id">Specifies the unique name to be used to identify the binding object. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract releaseByIdAsync: id: string * ?callback: (AsyncResult<unit> -> unit) -> unit

    /// Represents an XML node in a tree in a document.
    type [<AllowNullLiteral>] CustomXmlNode =
        /// Gets the base name of the node without the namespace prefix, if one exists.
        abstract baseName: string with get, set
        /// Retrieves the string GUID of the CustomXMLPart.
        abstract namespaceUri: string with get, set
        /// Gets the type of the CustomXMLNode.
        abstract nodeType: string with get, set
        /// <summary>Gets the nodes associated with the XPath expression.</summary>
        /// <param name="xPath">The XPath expression that specifies the nodes to get. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an array of CustomXmlNode objects that represent the nodes specified by the XPath expression passed to the `xPath` parameter.</param>
        abstract getNodesAsync: xPath: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<ResizeArray<CustomXmlNode>> -> unit) -> unit
        /// <summary>Gets the nodes associated with the XPath expression.</summary>
        /// <param name="xPath">The XPath expression that specifies the nodes to get. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an array of CustomXmlNode objects that represent the nodes specified by the XPath expression passed to the `xPath` parameter.</param>
        abstract getNodesAsync: xPath: string * ?callback: (AsyncResult<ResizeArray<CustomXmlNode>> -> unit) -> unit
        /// <summary>Gets the node value.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the value of the referenced node.</param>
        abstract getNodeValueAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Gets the node value.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the value of the referenced node.</param>
        abstract getNodeValueAsync: ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Gets the text of an XML node in a custom XML part.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the inner text of the referenced nodes.</param>
        abstract getTextAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Gets the text of an XML node in a custom XML part.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the inner text of the referenced nodes.</param>
        abstract getTextAsync: ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Gets the node's XML.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the XML of the referenced node.</param>
        abstract getXmlAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Gets the node's XML.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the XML of the referenced node.</param>
        abstract getXmlAsync: ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Sets the node value.</summary>
        /// <param name="value">The value to be set on the node</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setNodeValueAsync: value: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets the node value.</summary>
        /// <param name="value">The value to be set on the node</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setNodeValueAsync: value: string * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Asynchronously sets the text of an XML node in a custom XML part.</summary>
        /// <param name="text">Required. The text value of the XML node.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setTextAsync: text: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Asynchronously sets the text of an XML node in a custom XML part.</summary>
        /// <param name="text">Required. The text value of the XML node.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setTextAsync: text: string * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets the node XML.</summary>
        /// <param name="xml">The XML to be set on the node</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setXmlAsync: xml: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets the node XML.</summary>
        /// <param name="xml">The XML to be set on the node</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setXmlAsync: xml: string * ?callback: (AsyncResult<unit> -> unit) -> unit

    /// Represents a single CustomXMLPart in an {@link Office.CustomXmlParts} collection.
    type [<AllowNullLiteral>] CustomXmlPart =
        /// True, if the custom XML part is built in; otherwise false.
        abstract builtIn: bool with get, set
        /// Gets the GUID of the CustomXMLPart.
        abstract id: string with get, set
        /// Gets the set of namespace prefix mappings ({@link Office.CustomXmlPrefixMappings}) used against the current CustomXmlPart.
        abstract namespaceManager: CustomXmlPrefixMappings with get, set
        /// <summary>Adds an event handler to the object using the specified event type.</summary>
        /// <param name="eventType">Specifies the type of event to add. For a CustomXmlPart object, the eventType parameter can be specified as 
        /// `Office.EventType.NodeDeleted`, `Office.EventType.NodeInserted`, and `Office.EventType.NodeReplaced`.</param>
        /// <param name="handler">The event handler function to add, whose only parameter is of type {@link Office.NodeDeletedEventArgs}, 
        /// {@link Office.NodeInsertedEventArgs}, or {@link Office.NodeReplacedEventArgs}</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addHandlerAsync: eventType: Office.EventType * handler: (obj option -> unit) * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an event handler to the object using the specified event type.</summary>
        /// <param name="eventType">Specifies the type of event to add. For a CustomXmlPart object, the eventType parameter can be specified as 
        /// `Office.EventType.NodeDeleted`, `Office.EventType.NodeInserted`, and `Office.EventType.NodeReplaced`.</param>
        /// <param name="handler">The event handler function to add, whose only parameter is of type {@link Office.NodeDeletedEventArgs}, 
        /// {@link Office.NodeInsertedEventArgs}, or {@link Office.NodeReplacedEventArgs}</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addHandlerAsync: eventType: Office.EventType * handler: (obj option -> unit) * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Deletes the Custom XML Part.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract deleteAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Deletes the Custom XML Part.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract deleteAsync: ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Asynchronously gets any CustomXmlNodes in this custom XML part which match the specified XPath.</summary>
        /// <param name="xPath">An XPath expression that specifies the nodes you want returned. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an array of CustomXmlNode objects that represent the nodes specified by the XPath expression passed to the xPath parameter.</param>
        abstract getNodesAsync: xPath: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<ResizeArray<CustomXmlNode>> -> unit) -> unit
        /// <summary>Asynchronously gets any CustomXmlNodes in this custom XML part which match the specified XPath.</summary>
        /// <param name="xPath">An XPath expression that specifies the nodes you want returned. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an array of CustomXmlNode objects that represent the nodes specified by the XPath expression passed to the xPath parameter.</param>
        abstract getNodesAsync: xPath: string * ?callback: (AsyncResult<ResizeArray<CustomXmlNode>> -> unit) -> unit
        /// <summary>Asynchronously gets the XML inside this custom XML part.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the XML of the referenced CustomXmlPart object.</param>
        abstract getXmlAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously gets the XML inside this custom XML part.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the XML of the referenced CustomXmlPart object.</param>
        abstract getXmlAsync: ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Removes an event handler for the specified event type.</summary>
        /// <param name="eventType">Specifies the type of event to remove. For a CustomXmlPart object, the eventType parameter can be specified as 
        /// `Office.EventType.NodeDeleted`, `Office.EventType.NodeInserted`, and `Office.EventType.NodeReplaced`.</param>
        /// <param name="handler">The name of the handler to remove.</param>
        /// <param name="options">Provides options to determine which event handler or handlers are removed.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract removeHandlerAsync: eventType: Office.EventType * ?handler: (obj option -> unit) * ?options: RemoveHandlerOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes an event handler for the specified event type.</summary>
        /// <param name="eventType">Specifies the type of event to remove. For a CustomXmlPart object, the eventType parameter can be specified as 
        /// `Office.EventType.NodeDeleted`, `Office.EventType.NodeInserted`, and `Office.EventType.NodeReplaced`.</param>
        /// <param name="handler">The name of the handler to remove.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract removeHandlerAsync: eventType: Office.EventType * ?handler: (obj option -> unit) * ?callback: (AsyncResult<unit> -> unit) -> unit

    /// Provides information about the deleted node that raised the nodeDeleted event.
    type [<AllowNullLiteral>] NodeDeletedEventArgs =
        /// Gets whether the node was deleted as part of an Undo/Redo action by the user.
        abstract isUndoRedo: bool with get, set
        /// Gets the former next sibling of the node that was just deleted from the {@link Office.CustomXmlPart} object.
        abstract oldNextSibling: CustomXmlNode with get, set
        /// Gets the node which was just deleted from the {@link Office.CustomXmlPart} object.
        /// 
        /// Note that this node may have children, if a subtree is being removed from the document. Also, this node will be a "disconnected" node in 
        /// that you can query down from the node, but you cannot query up the tree - the node appears to exist alone.
        abstract oldNode: CustomXmlNode with get, set

    /// Provides information about the inserted node that raised the nodeInserted event.
    type [<AllowNullLiteral>] NodeInsertedEventArgs =
        /// Gets whether the node was inserted as part of an Undo/Redo action by the user.
        abstract isUndoRedo: bool with get, set
        /// Gets the node that was just added to the CustomXMLPart object.
        /// 
        /// Note that this node may have children, if a subtree was just added to the document.
        abstract newNode: CustomXmlNode with get, set

    /// Provides information about the replaced node that raised the nodeReplaced event.
    type [<AllowNullLiteral>] NodeReplacedEventArgs =
        /// Gets whether the replaced node was inserted as part of an undo or redo operation by the user.
        abstract isUndoRedo: bool with get, set
        /// Gets the node that was just added to the CustomXMLPart object.
        /// 
        /// Note that this node may have children, if a subtree was just added to the document.
        abstract newNode: CustomXmlNode with get, set
        /// Gets the node which was just deleted (replaced) from the CustomXmlPart object.
        /// 
        /// Note that this node may have children, if a subtree is being removed from the document. Also, this node will be a "disconnected" node in 
        /// that you can query down from the node, but you cannot query up the tree - the node appears to exist alone.
        abstract oldNode: CustomXmlNode with get, set

    /// Represents a collection of CustomXmlPart objects.
    type [<AllowNullLiteral>] CustomXmlParts =
        /// <summary>Asynchronously adds a new custom XML part to a file.</summary>
        /// <param name="xml">The XML to add to the newly created custom XML part.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the newly created CustomXmlPart object.</param>
        abstract addAsync: xml: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<CustomXmlPart> -> unit) -> unit
        /// <summary>Asynchronously adds a new custom XML part to a file.</summary>
        /// <param name="xml">The XML to add to the newly created custom XML part.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the newly created CustomXmlPart object.</param>
        abstract addAsync: xml: string * ?callback: (AsyncResult<CustomXmlPart> -> unit) -> unit
        /// <summary>Asynchronously gets the specified custom XML part by its id.</summary>
        /// <param name="id">The GUID of the custom XML part, including opening and closing braces.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a CustomXmlPart object that represents the specified custom XML part.
        ///  If there is no custom XML part with the specified id, the method returns null.</param>
        abstract getByIdAsync: id: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<CustomXmlPart> -> unit) -> unit
        /// <summary>Asynchronously gets the specified custom XML part by its id.</summary>
        /// <param name="id">The GUID of the custom XML part, including opening and closing braces.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a CustomXmlPart object that represents the specified custom XML part.
        ///  If there is no custom XML part with the specified id, the method returns null.</param>
        abstract getByIdAsync: id: string * ?callback: (AsyncResult<CustomXmlPart> -> unit) -> unit
        /// <summary>Asynchronously gets the specified custom XML part(s) by its namespace.</summary>
        /// <param name="ns">The namespace URI.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an array of CustomXmlPart objects that match the specified namespace.</param>
        abstract getByNamespaceAsync: ns: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<ResizeArray<CustomXmlPart>> -> unit) -> unit
        /// <summary>Asynchronously gets the specified custom XML part(s) by its namespace.</summary>
        /// <param name="ns">The namespace URI.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an array of CustomXmlPart objects that match the specified namespace.</param>
        abstract getByNamespaceAsync: ns: string * ?callback: (AsyncResult<ResizeArray<CustomXmlPart>> -> unit) -> unit

    /// Represents a collection of CustomXmlPart objects.
    type [<AllowNullLiteral>] CustomXmlPrefixMappings =
        /// <summary>Asynchronously adds a prefix to namespace mapping to use when querying an item.</summary>
        /// <param name="prefix">Specifies the prefix to add to the prefix mapping list. Required.</param>
        /// <param name="ns">Specifies the namespace URI to assign to the newly added prefix. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addNamespaceAsync: prefix: string * ns: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Asynchronously adds a prefix to namespace mapping to use when querying an item.</summary>
        /// <param name="prefix">Specifies the prefix to add to the prefix mapping list. Required.</param>
        /// <param name="ns">Specifies the namespace URI to assign to the newly added prefix. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addNamespaceAsync: prefix: string * ns: string * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Asynchronously gets the namespace mapped to the specified prefix.</summary>
        /// <param name="prefix">TSpecifies the prefix to get the namespace for. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the namespace mapped to the specified prefix.</param>
        abstract getNamespaceAsync: prefix: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously gets the namespace mapped to the specified prefix.</summary>
        /// <param name="prefix">TSpecifies the prefix to get the namespace for. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the namespace mapped to the specified prefix.</param>
        abstract getNamespaceAsync: prefix: string * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously gets the prefix for the specified namespace.</summary>
        /// <param name="ns">Specifies the namespace to get the prefix for. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the prefix of the specified namespace.</param>
        abstract getPrefixAsync: ns: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously gets the prefix for the specified namespace.</summary>
        /// <param name="ns">Specifies the namespace to get the prefix for. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is a string that contains the prefix of the specified namespace.</param>
        abstract getPrefixAsync: ns: string * ?callback: (AsyncResult<string> -> unit) -> unit

    /// An abstract class that represents the document the add-in is interacting with.
    type [<AllowNullLiteral>] Document =
        /// Gets an object that provides access to the bindings defined in the document.
        abstract bindings: Bindings with get, set
        /// Gets an object that represents the custom XML parts in the document.
        abstract customXmlParts: CustomXmlParts with get, set
        /// Gets the mode the document is in.
        abstract mode: DocumentMode with get, set
        /// Gets an object that represents the saved custom settings of the content or task pane add-in for the current document.
        abstract settings: Settings with get, set
        /// Gets the URL of the document that the host application currently has open. Returns null if the URL is unavailable.
        abstract url: string with get, set
        /// <summary>Adds an event handler for a Document object event.</summary>
        /// <param name="eventType">For a Document object event, the eventType parameter can be specified as `Office.EventType.Document.SelectionChanged` or 
        /// `Office.EventType.Document.ActiveViewChanged`, or the corresponding text value of this enumeration.</param>
        /// <param name="handler">The event handler function to add, whose only parameter is of type {@link Office.DocumentSelectionChangedEventArgs}. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addHandlerAsync: eventType: Office.EventType * handler: obj option * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an event handler for a Document object event.</summary>
        /// <param name="eventType">For a Document object event, the eventType parameter can be specified as `Office.EventType.Document.SelectionChanged` or 
        /// `Office.EventType.Document.ActiveViewChanged`, or the corresponding text value of this enumeration.</param>
        /// <param name="handler">The event handler function to add, whose only parameter is of type {@link Office.DocumentSelectionChangedEventArgs}. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addHandlerAsync: eventType: Office.EventType * handler: obj option * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Returns the state of the current view of the presentation (edit or read).</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the state of the presentation's current view. 
        ///  The value returned can be either "edit" or "read". "edit" corresponds to any of the views in which you can edit slides, 
        ///  such as Normal or Outline View. "read" corresponds to either Slide Show or Reading View.</param>
        abstract getActiveViewAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<DocumentGetActiveViewAsyncAsyncResult> -> unit) -> unit
        /// <summary>Returns the state of the current view of the presentation (edit or read).</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the state of the presentation's current view. 
        ///  The value returned can be either "edit" or "read". "edit" corresponds to any of the views in which you can edit slides, 
        ///  such as Normal or Outline View. "read" corresponds to either Slide Show or Reading View.</param>
        abstract getActiveViewAsync: ?callback: (AsyncResult<DocumentGetActiveViewAsyncAsyncResult> -> unit) -> unit
        /// <summary>Returns the entire document file in slices of up to 4194304 bytes (4 MB). For add-ins on iPad, file slice is supported up to 65536 (64 KB). 
        /// Note that specifying file slice size of above permitted limit will result in an "Internal Error" failure.</summary>
        /// <param name="fileType">The format in which the file will be returned</param>
        /// <param name="options">Provides options for setting the size of slices that the document will be divided into.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the File object.</param>
        abstract getFileAsync: fileType: FileType * ?options: GetFileOptions * ?callback: (AsyncResult<Office.File> -> unit) -> unit
        /// <summary>Returns the entire document file in slices of up to 4194304 bytes (4 MB). For add-ins on iPad, file slice is supported up to 65536 (64 KB). 
        /// Note that specifying file slice size of above permitted limit will result in an "Internal Error" failure.</summary>
        /// <param name="fileType">The format in which the file will be returned</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the File object.</param>
        abstract getFileAsync: fileType: FileType * ?callback: (AsyncResult<Office.File> -> unit) -> unit
        /// <summary>Gets file properties of the current document.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the file's properties (with the URL found at `asyncResult.value.url`).</param>
        abstract getFilePropertiesAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<Office.FileProperties> -> unit) -> unit
        /// <summary>Gets file properties of the current document.</summary>
        /// <param name="callback">A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the file's properties (with the URL found at `asyncResult.value.url`).</param>
        abstract getFilePropertiesAsync: ?callback: (AsyncResult<Office.FileProperties> -> unit) -> unit
        /// <summary>Reads the data contained in the current selection in the document.</summary>
        /// <param name="coercionType">The type of data structure to return. See the remarks section for each host's supported coercion types.</param>
        /// <param name="options">Provides options for customizing what data is returned and how it is formatted.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the data in the current selection. 
        ///  This is returned in the data structure or format you specified with the coercionType parameter. 
        ///  (See Remarks for more information about data coercion.)</param>
        abstract getSelectedDataAsync: coercionType: Office.CoercionType * ?options: GetSelectedDataOptions * ?callback: (AsyncResult<'T> -> unit) -> unit
        /// <summary>Reads the data contained in the current selection in the document.</summary>
        /// <param name="coercionType">The type of data structure to return. See the remarks section for each host's supported coercion types.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the data in the current selection. 
        ///  This is returned in the data structure or format you specified with the coercionType parameter. 
        ///  (See Remarks for more information about data coercion.)</param>
        abstract getSelectedDataAsync: coercionType: Office.CoercionType * ?callback: (AsyncResult<'T> -> unit) -> unit
        /// <summary>Goes to the specified object or location in the document.</summary>
        /// <param name="id">The identifier of the object or location to go to.</param>
        /// <param name="goToType">The type of the location to go to.</param>
        /// <param name="options">Provides options for whether to select the location that is navigated to.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the current view.</param>
        abstract goToByIdAsync: id: U2<string, float> * goToType: GoToType * ?options: GoToByIdOptions * ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Goes to the specified object or location in the document.</summary>
        /// <param name="id">The identifier of the object or location to go to.</param>
        /// <param name="goToType">The type of the location to go to.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the current view.</param>
        abstract goToByIdAsync: id: U2<string, float> * goToType: GoToType * ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Removes an event handler for the specified event type.</summary>
        /// <param name="eventType">The event type. For document can be 'Document.SelectionChanged' or 'Document.ActiveViewChanged'.</param>
        /// <param name="options">Provides options to determine which event handler or handlers are removed.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract removeHandlerAsync: eventType: Office.EventType * ?options: RemoveHandlerOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes an event handler for the specified event type.</summary>
        /// <param name="eventType">The event type. For document can be 'Document.SelectionChanged' or 'Document.ActiveViewChanged'.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract removeHandlerAsync: eventType: Office.EventType * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Writes the specified data into the current selection.</summary>
        /// <param name="data">The data to be set. Either a string or  {@link Office.CoercionType} value, 2d array or TableData object.
        /// 
        /// If the value passed for `data` is:
        /// 
        /// - A string: Plain text or anything that can be coerced to a string will be inserted. 
        /// In Excel, you can also specify data as a valid formula to add that formula to the selected cell. For example, setting data to "=SUM(A1:A5)" 
        /// will total the values in the specified range. However, when you set a formula on the bound cell, after doing so, you can't read the added 
        /// formula (or any pre-existing formula) from the bound cell. If you call the Document.getSelectedDataAsync method on the selected cell to 
        /// read its data, the method can return only the data displayed in the cell (the formula's result).
        /// 
        /// - An array of arrays ("matrix"): Tabular data without headers will be inserted. For example, to write data to three rows in two columns, 
        /// you can pass an array like this: [["R1C1", "R1C2"], ["R2C1", "R2C2"], ["R3C1", "R3C2"]]. To write a single column of three rows, pass an 
        /// array like this: [["R1C1"], ["R2C1"], ["R3C1"]]
        /// 
        /// In Excel, you can also specify data as an array of arrays that contains valid formulas to add them to the selected cells. For example if no 
        /// other data will be overwritten, setting data to [["=SUM(A1:A5)","=AVERAGE(A1:A5)"]] will add those two formulas to the selection. Just as 
        /// when setting a formula on a single cell as "text", you can't read the added formulas (or any pre-existing formulas) after they have been 
        /// set - you can only read the formulas' results.
        /// 
        /// - A TableData object: A table with headers will be inserted.
        /// In Excel, if you specify formulas in the TableData object you pass for the data parameter, you might not get the results you expect due to 
        /// the "calculated columns" feature of Excel, which automatically duplicates formulas within a column. To work around this when you want to 
        /// write `data` that contains formulas to a selected table, try specifying the data as an array of arrays (instead of a TableData object), and 
        /// specify the coercionType as Microsoft.Office.Matrix or "matrix". However, this technique will block the "calculated columns" feature only 
        /// when one of the following conditions is met: (1) you are writing to all the cells of the column, or (2) there are already at least two 
        /// different formulas in the column.</param>
        /// <param name="options">Provides options for how to insert data to the selection.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The AsyncResult.value property always returns undefined because there is no object or data to retrieve.</param>
        abstract setSelectedDataAsync: data: U3<string, TableData, ResizeArray<ResizeArray<obj option>>> * ?options: SetSelectedDataOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Writes the specified data into the current selection.</summary>
        /// <param name="data">The data to be set. Either a string or  {@link Office.CoercionType} value, 2d array or TableData object.
        /// 
        /// If the value passed for `data` is:
        /// 
        /// - A string: Plain text or anything that can be coerced to a string will be inserted. 
        /// In Excel, you can also specify data as a valid formula to add that formula to the selected cell. For example, setting data to "=SUM(A1:A5)" 
        /// will total the values in the specified range. However, when you set a formula on the bound cell, after doing so, you can't read the added 
        /// formula (or any pre-existing formula) from the bound cell. If you call the Document.getSelectedDataAsync method on the selected cell to 
        /// read its data, the method can return only the data displayed in the cell (the formula's result).
        /// 
        /// - An array of arrays ("matrix"): Tabular data without headers will be inserted. For example, to write data to three rows in two columns, 
        /// you can pass an array like this: [["R1C1", "R1C2"], ["R2C1", "R2C2"], ["R3C1", "R3C2"]]. To write a single column of three rows, pass an 
        /// array like this: [["R1C1"], ["R2C1"], ["R3C1"]]
        /// 
        /// In Excel, you can also specify data as an array of arrays that contains valid formulas to add them to the selected cells. For example if no 
        /// other data will be overwritten, setting data to [["=SUM(A1:A5)","=AVERAGE(A1:A5)"]] will add those two formulas to the selection. Just as 
        /// when setting a formula on a single cell as "text", you can't read the added formulas (or any pre-existing formulas) after they have been 
        /// set - you can only read the formulas' results.
        /// 
        /// - A TableData object: A table with headers will be inserted.
        /// In Excel, if you specify formulas in the TableData object you pass for the data parameter, you might not get the results you expect due to 
        /// the "calculated columns" feature of Excel, which automatically duplicates formulas within a column. To work around this when you want to 
        /// write `data` that contains formulas to a selected table, try specifying the data as an array of arrays (instead of a TableData object), and 
        /// specify the coercionType as Microsoft.Office.Matrix or "matrix". However, this technique will block the "calculated columns" feature only 
        /// when one of the following conditions is met: (1) you are writing to all the cells of the column, or (2) there are already at least two 
        /// different formulas in the column.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The AsyncResult.value property always returns undefined because there is no object or data to retrieve.</param>
        abstract setSelectedDataAsync: data: U3<string, TableData, ResizeArray<ResizeArray<obj option>>> * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Project documents only. Get Project field (Ex. ProjectWebAccessURL).</summary>
        /// <param name="fieldId">Project level fields.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result contains the `fieldValue` property, which represents the value of the specified field.</param>
        abstract getProjectFieldAsync: fieldId: float * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Project documents only. Get Project field (Ex. ProjectWebAccessURL).</summary>
        /// <param name="fieldId">Project level fields.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result contains the `fieldValue` property, which represents the value of the specified field.</param>
        abstract getProjectFieldAsync: fieldId: float * ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Project documents only. Get resource field for provided resource Id. (Ex.ResourceName)</summary>
        /// <param name="resourceId">Either a string or value of the Resource Id.</param>
        /// <param name="fieldId">Resource Fields.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the GUID of the resource as a string.</param>
        abstract getResourceFieldAsync: resourceId: string * fieldId: float * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Project documents only. Get resource field for provided resource Id. (Ex.ResourceName)</summary>
        /// <param name="resourceId">Either a string or value of the Resource Id.</param>
        /// <param name="fieldId">Resource Fields.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the GUID of the resource as a string.</param>
        abstract getResourceFieldAsync: resourceId: string * fieldId: float * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Project documents only. Get the current selected Resource's Id.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the GUID of the resource as a string.</param>
        abstract getSelectedResourceAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Project documents only. Get the current selected Resource's Id.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the GUID of the resource as a string.</param>
        abstract getSelectedResourceAsync: ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Project documents only. Get the current selected Task's Id.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the GUID of the resource as a string.</param>
        abstract getSelectedTaskAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Project documents only. Get the current selected Task's Id.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the GUID of the resource as a string.</param>
        abstract getSelectedTaskAsync: ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Project documents only. Get the current selected View Type (Ex. Gantt) and View Name.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result contains the following properties:
        ///  `viewName` - The name of the view, as a ProjectViewTypes constant.
        ///  `viewType` - The type of view, as the integer value of a ProjectViewTypes constant.</param>
        abstract getSelectedViewAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Project documents only. Get the current selected View Type (Ex. Gantt) and View Name.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result contains the following properties:
        ///  `viewName` - The name of the view, as a ProjectViewTypes constant.
        ///  `viewType` - The type of view, as the integer value of a ProjectViewTypes constant.</param>
        abstract getSelectedViewAsync: ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Project documents only. Get the Task Name, WSS Task Id, and ResourceNames for given taskId.</summary>
        /// <param name="taskId">Either a string or value of the Task Id.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result contains the following properties:
        ///  `taskName` - The name of the task.
        ///  `wssTaskId` - The ID of the task in the synchronized SharePoint task list. If the project is not synchronized with a SharePoint task list, the value is 0.
        ///  `resourceNames` - The comma-separated list of the names of resources that are assigned to the task.</param>
        abstract getTaskAsync: taskId: string * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Project documents only. Get the Task Name, WSS Task Id, and ResourceNames for given taskId.</summary>
        /// <param name="taskId">Either a string or value of the Task Id.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result contains the following properties:
        ///  `taskName` - The name of the task.
        ///  `wssTaskId` - The ID of the task in the synchronized SharePoint task list. If the project is not synchronized with a SharePoint task list, the value is 0.
        ///  `resourceNames` - The comma-separated list of the names of resources that are assigned to the task.</param>
        abstract getTaskAsync: taskId: string * ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Project documents only. Get task field for provided task Id. (Ex. StartDate).</summary>
        /// <param name="taskId">Either a string or value of the Task Id.</param>
        /// <param name="fieldId">Task Fields.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result contains the `fieldValue` property, which represents the value of the specified field.</param>
        abstract getTaskFieldAsync: taskId: string * fieldId: float * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Project documents only. Get task field for provided task Id. (Ex. StartDate).</summary>
        /// <param name="taskId">Either a string or value of the Task Id.</param>
        /// <param name="fieldId">Task Fields.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result contains the `fieldValue` property, which represents the value of the specified field.</param>
        abstract getTaskFieldAsync: taskId: string * fieldId: float * ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Project documents only. Get the WSS Url and list name for the Tasks List, the MPP is synced too.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result contains the following properties:
        ///  `listName` - the name of the synchronized SharePoint task list.
        ///  `serverUrl` - the URL of the synchronized SharePoint task list.</param>
        abstract getWSSUrlAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Project documents only. Get the WSS Url and list name for the Tasks List, the MPP is synced too.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result contains the following properties:
        ///  `listName` - the name of the synchronized SharePoint task list.
        ///  `serverUrl` - the URL of the synchronized SharePoint task list.</param>
        abstract getWSSUrlAsync: ?callback: (AsyncResult<obj option> -> unit) -> unit
        /// <summary>Project documents only. Get the maximum index of the collection of resources in the current project.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the highest index number in the current project's resource collection.</param>
        abstract getMaxResourceIndexAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<float> -> unit) -> unit
        /// <summary>Project documents only. Get the maximum index of the collection of resources in the current project.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the highest index number in the current project's resource collection.</param>
        abstract getMaxResourceIndexAsync: ?callback: (AsyncResult<float> -> unit) -> unit
        /// <summary>Project documents only. Get the maximum index of the collection of tasks in the current project.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the highest index number in the current project's task collection.</param>
        abstract getMaxTaskIndexAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<float> -> unit) -> unit
        /// <summary>Project documents only. Get the maximum index of the collection of tasks in the current project.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the highest index number in the current project's task collection.</param>
        abstract getMaxTaskIndexAsync: ?callback: (AsyncResult<float> -> unit) -> unit
        /// <summary>Project documents only. Get the GUID of the resource that has the specified index in the resource collection.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="resourceIndex">The index of the resource in the collection of resources for the project.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the GUID of the resource as a string.</param>
        abstract getResourceByIndexAsync: resourceIndex: float * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Project documents only. Get the GUID of the resource that has the specified index in the resource collection.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="resourceIndex">The index of the resource in the collection of resources for the project.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the GUID of the resource as a string.</param>
        abstract getResourceByIndexAsync: resourceIndex: float * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Project documents only. Get the GUID of the task that has the specified index in the task collection.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="taskIndex">The index of the task in the collection of tasks for the project.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the GUID of the task as a string.</param>
        abstract getTaskByIndexAsync: taskIndex: float * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Project documents only. Get the GUID of the task that has the specified index in the task collection.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="taskIndex">The index of the task in the collection of tasks for the project.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the GUID of the task as a string.</param>
        abstract getTaskByIndexAsync: taskIndex: float * ?callback: (AsyncResult<string> -> unit) -> unit
        /// <summary>Project documents only. Set resource field for specified resource Id.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="resourceId">Either a string or value of the Resource Id.</param>
        /// <param name="fieldId">Resource Fields.</param>
        /// <param name="fieldValue">Value of the target field.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setResourceFieldAsync: resourceId: string * fieldId: float * fieldValue: U4<string, float, bool, obj> * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Project documents only. Set resource field for specified resource Id.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="resourceId">Either a string or value of the Resource Id.</param>
        /// <param name="fieldId">Resource Fields.</param>
        /// <param name="fieldValue">Value of the target field.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setResourceFieldAsync: resourceId: string * fieldId: float * fieldValue: U4<string, float, bool, obj> * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Project documents only. Set task field for specified task Id.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="taskId">Either a string or value of the Task Id.</param>
        /// <param name="fieldId">Task Fields.</param>
        /// <param name="fieldValue">Value of the target field.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setTaskFieldAsync: taskId: string * fieldId: float * fieldValue: U4<string, float, bool, obj> * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Project documents only. Set task field for specified task Id.
        /// 
        /// **Important**: This API works only in Project 2016 on Windows desktop.</summary>
        /// <param name="taskId">Either a string or value of the Task Id.</param>
        /// <param name="fieldId">Task Fields.</param>
        /// <param name="fieldValue">Value of the target field.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setTaskFieldAsync: taskId: string * fieldId: float * fieldValue: U4<string, float, bool, obj> * ?callback: (AsyncResult<unit> -> unit) -> unit

    /// Provides information about the document that raised the SelectionChanged event.
    type [<AllowNullLiteral>] DocumentSelectionChangedEventArgs =
        /// Gets an {@link Office.Document} object that represents the document that raised the SelectionChanged event.
        abstract document: Document with get, set
        /// Get an {@link Office.EventType} enumeration value that identifies the kind of event that was raised.
        abstract ``type``: EventType with get, set

    /// Represents the document file associated with an Office Add-in.
    type [<AllowNullLiteral>] File =
        /// Gets the document file size in bytes.
        abstract size: float with get, set
        /// Gets the number of slices into which the file is divided.
        abstract sliceCount: float with get, set
        /// <summary>Closes the document file.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract closeAsync: ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Returns the specified slice.</summary>
        /// <param name="sliceIndex">Specifies the zero-based index of the slice to be retrieved. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is the {@link Office.Slice} object.</param>
        abstract getSliceAsync: sliceIndex: float * ?callback: (AsyncResult<Office.Slice> -> unit) -> unit

    type [<AllowNullLiteral>] FileProperties =
        /// File's URL
        abstract url: string with get, set

    /// Represents a binding in two dimensions of rows and columns.
    type [<AllowNullLiteral>] MatrixBinding =
        inherit Binding
        /// Gets the number of columns in the matrix data structure, as an integer value.
        abstract columnCount: float with get, set
        /// Gets the number of rows in the matrix data structure, as an integer value.
        abstract rowCount: float with get, set

    /// Represents custom settings for a task pane or content add-in that are stored in the host document as name/value pairs.
    type [<AllowNullLiteral>] Settings =
        /// <summary>Adds an event handler for the settingsChanged event.
        /// 
        /// **Important**: Your add-in's code can register a handler for the settingsChanged event when the add-in is running with any Excel client, but 
        /// the event will fire only when the add-in is loaded with a spreadsheet that is opened in Excel on the web, and more than one user is editing the 
        /// spreadsheet (coauthoring). Therefore, effectively the settingsChanged event is supported only in Excel on the web in coauthoring scenarios.</summary>
        /// <param name="eventType">Specifies the type of event to add. Required.</param>
        /// <param name="handler">The event handler function to add, whose only parameter is of type {@link Office.SettingsChangedEventArgs}. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        /// 
        /// <table>
        /// <tr>
        /// <th>Property</th>
        /// <th>Use to...</th>
        /// </tr>
        /// <tr>
        /// <td>AsyncResult.value</td>
        /// <td>Always returns undefined because there is no data or object to retrieve when adding an event handler.</td>
        /// </tr>
        /// <tr>
        /// <td>AsyncResult.status</td>
        /// <td>Determine the success or failure of the operation.</td>
        /// </tr>
        /// <tr>
        /// <td>AsyncResult.error</td>
        /// <td>Access an Error object that provides error information if the operation failed.</td>
        /// </tr>
        /// <tr>
        /// <td>AsyncResult.asyncContext</td>
        /// <td>A user-defined item of any type that is returned in the AsyncResult object without being altered.</td>
        /// </tr>
        /// </table></param>
        abstract addHandlerAsync: eventType: Office.EventType * handler: obj option * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an event handler for the settingsChanged event.
        /// 
        /// **Important**: Your add-in's code can register a handler for the settingsChanged event when the add-in is running with any Excel client, but 
        /// the event will fire only when the add-in is loaded with a spreadsheet that is opened in Excel on the web, and more than one user is editing the 
        /// spreadsheet (coauthoring). Therefore, effectively the settingsChanged event is supported only in Excel on the web in coauthoring scenarios.</summary>
        /// <param name="eventType">Specifies the type of event to add. Required.</param>
        /// <param name="handler">The event handler function to add, whose only parameter is of type {@link Office.SettingsChangedEventArgs}. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        /// 
        /// <table>
        /// <tr>
        /// <th>Property</th>
        /// <th>Use to...</th>
        /// </tr>
        /// <tr>
        /// <td>AsyncResult.value</td>
        /// <td>Always returns undefined because there is no data or object to retrieve when adding an event handler.</td>
        /// </tr>
        /// <tr>
        /// <td>AsyncResult.status</td>
        /// <td>Determine the success or failure of the operation.</td>
        /// </tr>
        /// <tr>
        /// <td>AsyncResult.error</td>
        /// <td>Access an Error object that provides error information if the operation failed.</td>
        /// </tr>
        /// <tr>
        /// <td>AsyncResult.asyncContext</td>
        /// <td>A user-defined item of any type that is returned in the AsyncResult object without being altered.</td>
        /// </tr>
        /// </table></param>
        abstract addHandlerAsync: eventType: Office.EventType * handler: obj option * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// Retrieves the specified setting.
        abstract get: name: string -> obj option
        /// <summary>Reads all settings persisted in the document and refreshes the content or task pane add-in's copy of those settings held in memory.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an {@link Office.Settings} object with the refreshed values.</param>
        abstract refreshAsync: ?callback: (AsyncResult<Office.Settings> -> unit) -> unit
        /// Removes the specified setting.
        /// 
        /// **Important**: Be aware that the Settings.remove method affects only the in-memory copy of the settings property bag. To persist the removal of 
        /// the specified setting in the document, at some point after calling the Settings.remove method and before the add-in is closed, you must 
        /// call the Settings.saveAsync method.
        abstract remove: name: string -> unit
        /// <summary>Removes an event handler for the settingsChanged event.</summary>
        /// <param name="eventType">Specifies the type of event to remove. Required.</param>
        /// <param name="options">Provides options to determine which event handler or handlers are removed.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract removeHandlerAsync: eventType: Office.EventType * ?options: RemoveHandlerOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes an event handler for the settingsChanged event.</summary>
        /// <param name="eventType">Specifies the type of event to remove. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract removeHandlerAsync: eventType: Office.EventType * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Persists the in-memory copy of the settings property bag in the document.</summary>
        /// <param name="options">Provides options for saving settings.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract saveAsync: ?options: SaveSettingsOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Persists the in-memory copy of the settings property bag in the document.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract saveAsync: ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets or creates the specified setting.
        /// 
        /// **Important**: Be aware that the Settings.set method affects only the in-memory copy of the settings property bag. 
        /// To make sure that additions or changes to settings will be available to your add-in the next time the document is opened, at some point 
        /// after calling the Settings.set method and before the add-in is closed, you must call the Settings.saveAsync method to persist settings in 
        /// the document.</summary>
        /// <param name="value">Specifies the value to be stored.</param>
        abstract set: name: string * value: obj option -> unit

    /// Provides information about the settings that raised the settingsChanged event.
    /// 
    /// To add an event handler for the settingsChanged event, use the addHandlerAsync method of the 
    /// {@link Office.Settings} object.
    /// 
    /// The settingsChanged event fires only when your add-in's script calls the Settings.saveAsync method to persist 
    /// the in-memory copy of the settings into the document file. The settingsChanged event is not triggered when the 
    /// Settings.set or Settings.remove methods are called.
    /// 
    /// The settingsChanged event was designed to let you to handle potential conflicts when two or more users are 
    /// attempting to save settings at the same time when your add-in is used in a shared (coauthored) document.
    /// 
    /// **Important**: Your add-in's code can register a handler for the settingsChanged event when the add-in 
    /// is running with any Excel client, but the event will fire only when the add-in is loaded with a spreadsheet 
    /// that is opened in Excel on the web, and more than one user is editing the spreadsheet (coauthoring). 
    /// Therefore, effectively the settingsChanged event is supported only in Excel on the web in coauthoring scenarios.
    type [<AllowNullLiteral>] SettingsChangedEventArgs =
        /// Gets an {@link Office.Settings} object that represents the settings that raised the settingsChanged event.
        abstract settings: Settings with get, set
        /// Get an {@link Office.EventType} enumeration value that identifies the kind of event that was raised.
        abstract ``type``: EventType with get, set

    /// Provides options for how to send messages, in either direction, between a dialog and its parent.
    type [<AllowNullLiteral>] DialogMessageOptions =
        /// Specifies the intended recipient domain for a message sent, in either direction, between a dialog and its parent. For example, `https://resources.contoso.com`.
        abstract targetOrigin: string with get, set

    /// Provides information about the message from the parent page that raised the `DialogParentMessageReceived` event.
    /// 
    /// To add an event handler for the `DialogParentMessageReceived` event, use the `addHandlerAsync` method of the
    /// {@link Office.UI} object.
    type [<AllowNullLiteral>] DialogParentMessageReceivedEventArgs =
        /// Gets the content of the message sent from the parent page, which can be any string or stringified data.
        abstract message: string with get, set
        /// Gets the domain of the parent page that called `Dialog.messageChild`.
        abstract origin: string option with get, set
        /// Gets an {@link Office.EventType} enumeration value that identifies the kind of event that was raised.
        abstract ``type``: EventType with get, set

    /// Represents a slice of a document file. The Slice object is accessed with the `File.getSliceAsync` method.
    type [<AllowNullLiteral>] Slice =
        /// Gets the raw data of the file slice in `Office.FileType.Text` or `Office.FileType.Compressed` format as specified 
        /// by the `fileType` parameter of the call to the `Document.getFileAsync` method.
        abstract data: obj option with get, set
        /// Gets the zero-based index of the file slice.
        abstract index: float with get, set
        /// Gets the size of the slice in bytes.
        abstract size: float with get, set

    /// Represents a binding in two dimensions of rows and columns, optionally with headers.
    type [<AllowNullLiteral>] TableBinding =
        inherit Binding
        /// Gets the number of columns in the TableBinding, as an integer value.
        abstract columnCount: float with get, set
        /// True, if the table has headers; otherwise false.
        abstract hasHeaders: bool with get, set
        /// Gets the number of rows in the TableBinding, as an integer value.
        abstract rowCount: float with get, set
        /// <summary>Adds the specified data to the table as additional columns.</summary>
        /// <param name="tableData">An array of arrays ("matrix") or a TableData object that contains one or more columns of data to add to the table. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addColumnsAsync: tableData: U2<TableData, ResizeArray<ResizeArray<obj option>>> * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds the specified data to the table as additional columns.</summary>
        /// <param name="tableData">An array of arrays ("matrix") or a TableData object that contains one or more columns of data to add to the table. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addColumnsAsync: tableData: U2<TableData, ResizeArray<ResizeArray<obj option>>> * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds the specified data to the table as additional rows.</summary>
        /// <param name="rows">An array of arrays ("matrix") or a TableData object that contains one or more rows of data to add to the table. Required.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addRowsAsync: rows: U2<TableData, ResizeArray<ResizeArray<obj option>>> * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds the specified data to the table as additional rows.</summary>
        /// <param name="rows">An array of arrays ("matrix") or a TableData object that contains one or more rows of data to add to the table. Required.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract addRowsAsync: rows: U2<TableData, ResizeArray<ResizeArray<obj option>>> * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Deletes all non-header rows and their values in the table, shifting appropriately for the host application.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract deleteAllDataValuesAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Deletes all non-header rows and their values in the table, shifting appropriately for the host application.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract deleteAllDataValuesAsync: ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Clears formatting on the bound table.</summary>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract clearFormatsAsync: ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Clears formatting on the bound table.</summary>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract clearFormatsAsync: ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Gets the formatting on specified items in the table.</summary>
        /// <param name="cellReference">An object literal containing name-value pairs that specify the range of cells to get formatting from.</param>
        /// <param name="formats">An array specifying the format properties to get.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an array containing one or more JavaScript objects specifying the formatting of their corresponding cells.</param>
        abstract getFormatsAsync: ?cellReference: obj * ?formats: ResizeArray<obj option> * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<ResizeArray<TableBindingGetFormatsAsyncAsyncResult>> -> unit) -> unit
        /// <summary>Gets the formatting on specified items in the table.</summary>
        /// <param name="cellReference">An object literal containing name-value pairs that specify the range of cells to get formatting from.</param>
        /// <param name="formats">An array specifying the format properties to get.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.
        ///  The `value` property of the result is an array containing one or more JavaScript objects specifying the formatting of their corresponding cells.</param>
        abstract getFormatsAsync: ?cellReference: obj * ?formats: ResizeArray<obj option> * ?callback: (AsyncResult<ResizeArray<TableBindingGetFormatsAsyncAsyncResult>> -> unit) -> unit
        /// <summary>Sets formatting on specified items and data in the table.</summary>
        /// <param name="cellFormat">An array that contains one or more JavaScript objects that specify which cells to target and the formatting to apply to them.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setFormatsAsync: cellFormat: ResizeArray<obj option> * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets formatting on specified items and data in the table.</summary>
        /// <param name="cellFormat">An array that contains one or more JavaScript objects that specify which cells to target and the formatting to apply to them.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setFormatsAsync: cellFormat: ResizeArray<obj option> * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Updates table formatting options on the bound table.</summary>
        /// <param name="tableOptions">An object literal containing a list of property name-value pairs that define the table options to apply.</param>
        /// <param name="options">Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setTableOptionsAsync: tableOptions: obj option * ?options: Office.AsyncContextOptions * ?callback: (AsyncResult<unit> -> unit) -> unit
        /// <summary>Updates table formatting options on the bound table.</summary>
        /// <param name="tableOptions">An object literal containing a list of property name-value pairs that define the table options to apply.</param>
        /// <param name="callback">Optional. A function that is invoked when the callback returns, whose only parameter is of type {@link Office.AsyncResult}.</param>
        abstract setTableOptionsAsync: tableOptions: obj option * ?callback: (AsyncResult<unit> -> unit) -> unit

    /// Represents the data in a table or an {@link Office.TableBinding}.
    type [<AllowNullLiteral>] TableData =
        /// Gets or sets the headers of the table.
        abstract headers: ResizeArray<obj option> with get, set
        /// Gets or sets the rows in the table. Returns an array of arrays that contains the data in the table. 
        /// Returns an empty array if there are no rows.
        abstract rows: ResizeArray<ResizeArray<obj option>> with get, set

    /// Represents the data in a table or an {@link Office.TableBinding}.
    type [<AllowNullLiteral>] TableDataStatic =
        [<Emit "new $0($1...)">] abstract Create: rows: ResizeArray<ResizeArray<obj option>> * headers: ResizeArray<obj option> -> TableData
        [<Emit "new $0($1...)">] abstract Create: unit -> TableData

    type Table =
        obj

    /// Represents a bound text selection in the document.
    /// 
    /// The TextBinding object inherits the id property, type property, getDataAsync method, and setDataAsync method from the {@link Office.Binding} 
    /// object. It does not implement any additional properties or methods of its own.
    type [<AllowNullLiteral>] TextBinding =
        inherit Binding

    type ProjectProjectFields =
        obj

    type ProjectResourceFields =
        obj

    type ProjectTaskFields =
        obj

    type ProjectViewTypes =
        obj

    module MailboxEnums =

        type [<StringEnum>] [<RequireQualifiedAccess>] ActionType =
            | ShowTaskPane

        type [<StringEnum>] [<RequireQualifiedAccess>] AttachmentContentFormat =
            | Base64
            | Url
            | Eml
            | ICalendar

        type [<StringEnum>] [<RequireQualifiedAccess>] AttachmentStatus =
            | Added
            | Removed

        type [<StringEnum>] [<RequireQualifiedAccess>] AttachmentType =
            | File
            | Item
            | Cloud

        type CategoryColor =
            obj

        type [<StringEnum>] [<RequireQualifiedAccess>] ComposeType =
            | Reply
            | NewMail
            | Forward

        type [<StringEnum>] [<RequireQualifiedAccess>] Days =
            | Mon
            | Tue
            | Wed
            | Thu
            | Fri
            | Sat
            | Sun
            | Weekday
            | WeekendDay
            | Day

        type [<RequireQualifiedAccess>] DelegatePermissions =
            | Read = 1
            | Write = 2
            | DeleteOwn = 4
            | DeleteAll = 8
            | EditOwn = 16
            | EditAll = 32

        type [<StringEnum>] [<RequireQualifiedAccess>] EntityType =
            | MeetingSuggestion
            | TaskSuggestion
            | Address
            | EmailAddress
            | Url
            | PhoneNumber
            | Contact

        type [<StringEnum>] [<RequireQualifiedAccess>] ItemNotificationMessageType =
            | ProgressIndicator
            | InformationalMessage
            | ErrorMessage
            | InsightMessage

        type [<StringEnum>] [<RequireQualifiedAccess>] ItemType =
            | Message
            | Appointment

        type [<StringEnum>] [<RequireQualifiedAccess>] LocationType =
            | Custom
            | Room

        type [<StringEnum>] [<RequireQualifiedAccess>] Month =
            | Jan
            | Feb
            | Mar
            | Apr
            | May
            | Jun
            | Jul
            | Aug
            | Sep
            | Oct
            | Nov
            | Dec

        type [<StringEnum>] [<RequireQualifiedAccess>] OWAView =
            | [<CompiledName "OneColumn">] OneColumn
            | [<CompiledName "TwoColumns">] TwoColumns
            | [<CompiledName "ThreeColumns">] ThreeColumns

        type [<StringEnum>] [<RequireQualifiedAccess>] RecipientType =
            | DistributionList
            | User
            | ExternalUser
            | Other

        type [<StringEnum>] [<RequireQualifiedAccess>] RecurrenceTimeZone =
            | [<CompiledName "Afghanistan Standard Time">] AfghanistanStandardTime
            | [<CompiledName "Alaskan Standard Time">] AlaskanStandardTime
            | [<CompiledName "Aleutian Standard Time">] AleutianStandardTime
            | [<CompiledName "Altai Standard Time">] AltaiStandardTime
            | [<CompiledName "Arab Standard Time">] ArabStandardTime
            | [<CompiledName "Arabian Standard Time">] ArabianStandardTime
            | [<CompiledName "Arabic Standard Time">] ArabicStandardTime
            | [<CompiledName "Argentina Standard Time">] ArgentinaStandardTime
            | [<CompiledName "Astrakhan Standard Time">] AstrakhanStandardTime
            | [<CompiledName "Atlantic Standard Time">] AtlanticStandardTime
            | [<CompiledName "AUS Central Standard Time">] AUSCentralStandardTime
            | [<CompiledName "Aus Central W. Standard Time">] AusCentralW_StandardTime
            | [<CompiledName "AUS Eastern Standard Time">] AUSEasternStandardTime
            | [<CompiledName "Azerbaijan Standard Time">] AzerbaijanStandardTime
            | [<CompiledName "Azores Standard Time">] AzoresStandardTime
            | [<CompiledName "Bahia Standard Time">] BahiaStandardTime
            | [<CompiledName "Bangladesh Standard Time">] BangladeshStandardTime
            | [<CompiledName "Belarus Standard Time">] BelarusStandardTime
            | [<CompiledName "Bougainville Standard Time">] BougainvilleStandardTime
            | [<CompiledName "Canada Central Standard Time">] CanadaCentralStandardTime
            | [<CompiledName "Cape Verde Standard Time">] CapeVerdeStandardTime
            | [<CompiledName "Caucasus Standard Time">] CaucasusStandardTime
            | [<CompiledName "Cen. Australia Standard Time">] CenAustraliaStandardTime
            | [<CompiledName "Central America Standard Time">] CentralAmericaStandardTime
            | [<CompiledName "Central Asia Standard Time">] CentralAsiaStandardTime
            | [<CompiledName "Central Brazilian Standard Time">] CentralBrazilianStandardTime
            | [<CompiledName "Central Europe Standard Time">] CentralEuropeStandardTime
            | [<CompiledName "Central European Standard Time">] CentralEuropeanStandardTime
            | [<CompiledName "Central Pacific Standard Time">] CentralPacificStandardTime
            | [<CompiledName "Central Standard Time">] CentralStandardTime
            | [<CompiledName "Central Standard Time (Mexico)">] CentralStandardTime_Mexico
            | [<CompiledName "Chatham Islands Standard Time">] ChathamIslandsStandardTime
            | [<CompiledName "China Standard Time">] ChinaStandardTime
            | [<CompiledName "Cuba Standard Time">] CubaStandardTime
            | [<CompiledName "Dateline Standard Time">] DatelineStandardTime
            | [<CompiledName "E. Africa Standard Time">] E_AfricaStandardTime
            | [<CompiledName "E. Australia Standard Time">] E_AustraliaStandardTime
            | [<CompiledName "E. Europe Standard Time">] E_EuropeStandardTime
            | [<CompiledName "E. South America Standard Time">] E_SouthAmericaStandardTime
            | [<CompiledName "Easter Island Standard Time">] EasterIslandStandardTime
            | [<CompiledName "Eastern Standard Time">] EasternStandardTime
            | [<CompiledName "Eastern Standard Time (Mexico)">] EasternStandardTime_Mexico
            | [<CompiledName "Egypt Standard Time">] EgyptStandardTime
            | [<CompiledName "Ekaterinburg Standard Time">] EkaterinburgStandardTime
            | [<CompiledName "Fiji Standard Time">] FijiStandardTime
            | [<CompiledName "FLE Standard Time">] FLEStandardTime
            | [<CompiledName "Georgian Standard Time">] GeorgianStandardTime
            | [<CompiledName "GMT Standard Time">] GMTStandardTime
            | [<CompiledName "Greenland Standard Time">] GreenlandStandardTime
            | [<CompiledName "Greenwich Standard Time">] GreenwichStandardTime
            | [<CompiledName "GTB Standard Time">] GTBStandardTime
            | [<CompiledName "Haiti Standard Time">] HaitiStandardTime
            | [<CompiledName "Hawaiian Standard Time">] HawaiianStandardTime
            | [<CompiledName "India Standard Time">] IndiaStandardTime
            | [<CompiledName "Iran Standard Time">] IranStandardTime
            | [<CompiledName "Israel Standard Time">] IsraelStandardTime
            | [<CompiledName "Jordan Standard Time">] JordanStandardTime
            | [<CompiledName "Kaliningrad Standard Time">] KaliningradStandardTime
            | [<CompiledName "Kamchatka Standard Time">] KamchatkaStandardTime
            | [<CompiledName "Korea Standard Time">] KoreaStandardTime
            | [<CompiledName "Libya Standard Time">] LibyaStandardTime
            | [<CompiledName "Line Islands Standard Time">] LineIslandsStandardTime
            | [<CompiledName "Lord Howe Standard Time">] LordHoweStandardTime
            | [<CompiledName "Magadan Standard Time">] MagadanStandardTime
            | [<CompiledName "Magallanes Standard Time">] MagallanesStandardTime
            | [<CompiledName "Marquesas Standard Time">] MarquesasStandardTime
            | [<CompiledName "Mauritius Standard Time">] MauritiusStandardTime
            | [<CompiledName "Mid-Atlantic Standard Time">] MidAtlanticStandardTime
            | [<CompiledName "Middle East Standard Time">] MiddleEastStandardTime
            | [<CompiledName "Montevideo Standard Time">] MontevideoStandardTime
            | [<CompiledName "Morocco Standard Time">] MoroccoStandardTime
            | [<CompiledName "Mountain Standard Time">] MountainStandardTime
            | [<CompiledName "Mountain Standard Time (Mexico)">] MountainStandardTime_Mexico
            | [<CompiledName "Myanmar Standard Time">] MyanmarStandardTime
            | [<CompiledName "N. Central Asia Standard Time">] N_CentralAsiaStandardTime
            | [<CompiledName "Namibia Standard Time">] NamibiaStandardTime
            | [<CompiledName "Nepal Standard Time">] NepalStandardTime
            | [<CompiledName "New Zealand Standard Time">] NewZealandStandardTime
            | [<CompiledName "Newfoundland Standard Time">] NewfoundlandStandardTime
            | [<CompiledName "Norfolk Standard Time">] NorfolkStandardTime
            | [<CompiledName "North Asia East Standard Time">] NorthAsiaEastStandardTime
            | [<CompiledName "North Asia Standard Time">] NorthAsiaStandardTime
            | [<CompiledName "North Korea Standard Time">] NorthKoreaStandardTime
            | [<CompiledName "Omsk Standard Time">] OmskStandardTime
            | [<CompiledName "Pacific SA Standard Time">] PacificSAStandardTime
            | [<CompiledName "Pacific Standard Time">] PacificStandardTime
            | [<CompiledName "Pacific Standard Time (Mexico)">] PacificStandardTimeMexico
            | [<CompiledName "Pakistan Standard Time">] PakistanStandardTime
            | [<CompiledName "Paraguay Standard Time">] ParaguayStandardTime
            | [<CompiledName "Romance Standard Time">] RomanceStandardTime
            | [<CompiledName "Russia Time Zone 10">] RussiaTimeZone10
            | [<CompiledName "Russia Time Zone 11">] RussiaTimeZone11
            | [<CompiledName "Russia Time Zone 3">] RussiaTimeZone3
            | [<CompiledName "Russian Standard Time">] RussianStandardTime
            | [<CompiledName "SA Eastern Standard Time">] SAEasternStandardTime
            | [<CompiledName "SA Pacific Standard Time">] SAPacificStandardTime
            | [<CompiledName "SA Western Standard Time">] SAWesternStandardTime
            | [<CompiledName "Saint Pierre Standard Time">] SaintPierreStandardTime
            | [<CompiledName "Sakhalin Standard Time">] SakhalinStandardTime
            | [<CompiledName "Samoa Standard Time">] SamoaStandardTime
            | [<CompiledName "Saratov Standard Time">] SaratovStandardTime
            | [<CompiledName "SE Asia Standard Time">] SEAsiaStandardTime
            | [<CompiledName "Singapore Standard Time">] SingaporeStandardTime
            | [<CompiledName "South Africa Standard Time">] SouthAfricaStandardTime
            | [<CompiledName "Sri Lanka Standard Time">] SriLankaStandardTime
            | [<CompiledName "Sudan Standard Time">] SudanStandardTime
            | [<CompiledName "Syria Standard Time">] SyriaStandardTime
            | [<CompiledName "Taipei Standard Time">] TaipeiStandardTime
            | [<CompiledName "Tasmania Standard Time">] TasmaniaStandardTime
            | [<CompiledName "Tocantins Standard Time">] TocantinsStandardTime
            | [<CompiledName "Tokyo Standard Time">] TokyoStandardTime
            | [<CompiledName "Tomsk Standard Time">] TomskStandardTime
            | [<CompiledName "Tonga Standard Time">] TongaStandardTime
            | [<CompiledName "Transbaikal Standard Time">] TransbaikalStandardTime
            | [<CompiledName "Turkey Standard Time">] TurkeyStandardTime
            | [<CompiledName "Turks And Caicos Standard Time">] TurksAndCaicosStandardTime
            | [<CompiledName "Ulaanbaatar Standard Time">] UlaanbaatarStandardTime
            | [<CompiledName "US Eastern Standard Time">] USEasternStandardTime
            | [<CompiledName "US Mountain Standard Time">] USMountainStandardTime
            | [<CompiledName "UTC">] UTC
            | [<CompiledName "UTC+12">] UTCPLUS12
            | [<CompiledName "UTC+13">] UTCPLUS13
            | [<CompiledName "UTC-02">] UTCMINUS02
            | [<CompiledName "UTC-08">] UTCMINUS08
            | [<CompiledName "UTC-09">] UTCMINUS09
            | [<CompiledName "UTC-11">] UTCMINUS11
            | [<CompiledName "Venezuela Standard Time">] VenezuelaStandardTime
            | [<CompiledName "Vladivostok Standard Time">] VladivostokStandardTime
            | [<CompiledName "W. Australia Standard Time">] W_AustraliaStandardTime
            | [<CompiledName "W. Central Africa Standard Time">] W_CentralAfricaStandardTime
            | [<CompiledName "W. Europe Standard Time">] W_EuropeStandardTime
            | [<CompiledName "W. Mongolia Standard Time">] W_MongoliaStandardTime
            | [<CompiledName "West Asia Standard Time">] WestAsiaStandardTime
            | [<CompiledName "West Bank Standard Time">] WestBankStandardTime
            | [<CompiledName "West Pacific Standard Time">] WestPacificStandardTime
            | [<CompiledName "Yakutsk Standard Time">] YakutskStandardTime

        type [<StringEnum>] [<RequireQualifiedAccess>] RecurrenceType =
            | Daily
            | Weekday
            | Weekly
            | Monthly
            | Yearly

        type [<StringEnum>] [<RequireQualifiedAccess>] ResponseType =
            | None
            | Organizer
            | Tentative
            | Accepted
            | Declined

        type [<StringEnum>] [<RequireQualifiedAccess>] RestVersion =
            | [<CompiledName "v1.0">] V1_0
            | [<CompiledName "v2.0">] V2_0
            | Beta

        type [<StringEnum>] [<RequireQualifiedAccess>] SourceProperty =
            | Body
            | Subject

        type [<StringEnum>] [<RequireQualifiedAccess>] WeekNumber =
            | First
            | Second
            | Third
            | Fourth
            | Last

    /// Provides an option for the data format.
    type [<AllowNullLiteral>] CoercionTypeOptions =
        /// The desired data format.
        abstract coercionType: U2<Office.CoercionType, string> option with get, set

    /// The subclass of {@link Office.Item | Item} dealing with appointments.
    /// 
    /// **Important**: This is an internal Outlook object, not directly exposed through existing interfaces. 
    /// You should treat this as a mode of `Office.context.mailbox.item`. For more information, refer to the
    /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item | Object Model} page.
    /// 
    /// Child interfaces:
    /// 
    /// - {@link Office.AppointmentCompose | AppointmentCompose}
    /// 
    /// - {@link Office.AppointmentRead | AppointmentRead}
    type [<AllowNullLiteral>] Appointment =
        inherit Item

    /// The appointment organizer mode of {@link Office.Item | Office.context.mailbox.item}.
    /// 
    /// **Important**: This is an internal Outlook object, not directly exposed through existing interfaces. 
    /// You should treat this as a mode of `Office.context.mailbox.item`. For more information, refer to the
    /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item | Object Model} page.
    /// 
    /// Parent interfaces:
    /// 
    /// - {@link Office.ItemCompose | ItemCompose}
    /// 
    /// - {@link Office.Appointment | Appointment}
    type [<AllowNullLiteral>] AppointmentCompose =
        inherit Appointment
        inherit ItemCompose
        /// Gets an object that provides methods for manipulating the body of an item.
        /// 
        /// [Api set: Mailbox 1.1]
        abstract body: Body with get, set
        /// Gets an object that provides methods for managing the item's categories.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract categories: Categories with get, set
        /// Gets or sets the date and time that the appointment is to end.
        /// 
        /// The `end` property is a {@link Office.Time | Time} object expressed as a Coordinated Universal Time (UTC) date and time value. 
        /// You can use the `convertToLocalClientTime` method to convert the `end` property value to the client's local date and time.
        /// 
        /// When you use the `Time.setAsync` method to set the end time, you should use the `convertToUtcClientTime` method to convert the local time on 
        /// the client to UTC for the server.
        /// 
        /// **Important**: In the Windows client, you can't use this property to update the end of a recurrence.
        abstract ``end``: Time with get, set
        /// Gets or sets the locations of the appointment. The `enhancedLocation` property returns an {@link Office.EnhancedLocation | EnhancedLocation}
        /// object that provides methods to get, remove, or add locations on an item.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract enhancedLocation: EnhancedLocation with get, set
        /// Gets the type of item that an instance represents.
        /// 
        /// The `itemType` property returns one of the `ItemType` enumeration values, indicating whether the `item` object instance is a message or an appointment.
        abstract itemType: U2<MailboxEnums.ItemType, string> with get, set
        /// Gets or sets the location of an appointment. The `location` property returns a {@link Office.Location | Location} object that provides methods that are 
        /// used to get and set the location of the appointment.
        abstract location: Location with get, set
        /// Gets the notification messages for an item.
        /// 
        /// [Api set: Mailbox 1.3]
        abstract notificationMessages: NotificationMessages with get, set
        /// Provides access to the optional attendees of an event. The type of object and level of access depend on the mode of the current item.
        /// 
        /// The `optionalAttendees` property returns a `Recipients` object that provides methods to get or update the
        /// optional attendees for a meeting. However, depending on the client/platform (i.e., Windows, Mac, etc.), limits may apply on how many
        /// recipients you can get or update. See the {@link Office.Recipients | Recipients} object for more details.
        abstract optionalAttendees: Recipients with get, set
        /// Gets the organizer for the specified meeting. 
        /// 
        /// The `organizer` property returns an {@link Office.Organizer | Organizer} object that provides a method to get the organizer value.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract organizer: Organizer with get, set
        /// Gets or sets the recurrence pattern of an appointment.
        /// 
        /// The `recurrence` property returns a recurrence object for recurring appointments or meetings requests if an item is a series or an instance 
        /// in a series. `null` is returned for single appointments and meeting requests of single appointments.
        /// 
        /// **Note**: Meeting requests have an `itemClass` value of `IPM.Schedule.Meeting.Request`.
        /// 
        /// **Note**: If the recurrence object is null, this indicates that the object is a single appointment or a meeting request of a single 
        /// appointment and NOT a part of a series.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract recurrence: Recurrence with get, set
        /// Provides access to the required attendees of an event. The type of object and level of access depend on the mode of the current item.
        /// 
        /// The `requiredAttendees` property returns a `Recipients` object that provides methods to get or update the
        /// required attendees for a meeting. However, depending on the client/platform (i.e., Windows, Mac, etc.), limits may apply on how many
        /// recipients you can get or update. See the {@link Office.Recipients | Recipients} object for more details.
        abstract requiredAttendees: Recipients with get, set
        /// Gets the id of the series that an instance belongs to.
        /// 
        /// In Outlook on the web and desktop clients, the `seriesId` property returns the Exchange Web Services (EWS) ID of the parent (series) item
        /// that this item belongs to. However, on iOS and Android, the seriesId returns the REST ID of the parent item.
        /// 
        /// **Note**: The identifier returned by the `seriesId` property is the same as the Exchange Web Services item identifier. 
        /// The `seriesId` property is not identical to the Outlook IDs used by the Outlook REST API. 
        /// Before making REST API calls using this value, it should be converted using `Office.context.mailbox.convertToRestId`. 
        /// For more details, see {@link https://docs.microsoft.com/office/dev/add-ins/outlook/use-rest-api | Use the Outlook REST APIs from an Outlook add-in}.
        /// 
        /// The `seriesId` property returns `null` for items that do not have parent items such as single appointments, series items, or meeting requests 
        /// and returns `undefined` for any other items that are not meeting requests.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract seriesId: string with get, set
        /// Gets or sets the date and time that the appointment is to begin.
        /// 
        /// The `start` property is a {@link Office.Time | Time} object expressed as a Coordinated Universal Time (UTC) date and time value. 
        /// You can use the `convertToLocalClientTime` method to convert the value to the client's local date and time.
        /// 
        /// When you use the `Time.setAsync` method to set the start time, you should use the `convertToUtcClientTime` method to convert the local time on 
        /// the client to UTC for the server.
        /// 
        /// **Important**: In the Windows client, you can't use this property to update the start of a recurrence.
        abstract start: Time with get, set
        /// Gets or sets the description that appears in the subject field of an item.
        /// 
        /// The `subject` property gets or sets the entire subject of the item, as sent by the email server.
        /// 
        /// The `subject` property returns a `Subject` object that provides methods to get and set the subject.
        abstract subject: Subject with get, set
        /// <summary>Adds a file to a message or appointment as an attachment.
        /// 
        /// The `addFileAttachmentAsync` method uploads the file at the specified URI and attaches it to the item in the compose form.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// **Important**: In recent builds of Outlook on Windows, a bug was introduced that incorrectly appends an `Authorization: Bearer` header to
        /// this action (whether using this API or the Outlook UI). To work around this issue, you can try using the `addFileAttachmentFromBase64` API
        /// introduced with requirement set 1.8.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="uri">- The URI that provides the location of the file to attach to the message or appointment. The maximum length is 2048 characters.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `isInline`: If true, indicates that the attachment will be shown inline in the message body,
        ///     and should not be displayed in the attachment list.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`.
        /// On success, the attachment identifier will be provided in the `asyncResult.value` property.
        /// If uploading the attachment fails, the `asyncResult` object will contain
        /// an `Error` object that provides a description of the error.</param>
        abstract addFileAttachmentAsync: uri: string * attachmentName: string * options: obj * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Adds a file to a message or appointment as an attachment.
        /// 
        /// The `addFileAttachmentAsync` method uploads the file at the specified URI and attaches it to the item in the compose form.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// **Important**: In recent builds of Outlook on Windows, a bug was introduced that incorrectly appends an `Authorization: Bearer` header to
        /// this action (whether using this API or the Outlook UI). To work around this issue, you can try using the `addFileAttachmentFromBase64` API
        /// introduced with requirement set 1.8.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="uri">- The URI that provides the location of the file to attach to the message or appointment. The maximum length is 2048 characters.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`.
        /// On success, the attachment identifier will be provided in the `asyncResult.value` property.
        /// If uploading the attachment fails, the `asyncResult` object will contain
        /// an `Error` object that provides a description of the error.</param>
        abstract addFileAttachmentAsync: uri: string * attachmentName: string * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Adds a file to a message or appointment as an attachment.
        /// 
        /// The `addFileAttachmentFromBase64Async` method uploads the file from the base64 encoding and attaches it to the item in the compose form.
        /// This method returns the attachment identifier in the `asyncResult.value` object.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// **Note**: If you're using a data URL API (e.g., `readAsDataURL`), you need to strip out the data URL prefix then send the rest of the string to this API.
        /// For example, if the full string is represented by `data:image/svg+xml;base64,<rest of base64 string>`, remove `data:image/svg+xml;base64,`.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="base64File">- The base64 encoded content of an image or file to be added to an email or event.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `isInline`: If true, indicates that the attachment will be shown inline in the message body
        ///     and should not be displayed in the attachment list.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`.
        ///  On success, the attachment identifier will be provided in the `asyncResult.value` property. 
        ///  If uploading the attachment fails, the `asyncResult` object will contain
        ///  an `Error` object that provides a description of the error.</param>
        abstract addFileAttachmentFromBase64Async: base64File: string * attachmentName: string * options: obj * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Adds a file to a message or appointment as an attachment.
        /// 
        /// The `addFileAttachmentFromBase64Async` method uploads the file from the base64 encoding and attaches it to the item in the compose form.
        /// This method returns the attachment identifier in the `asyncResult.value` object.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// **Note**: If you're using a data URL API (e.g., `readAsDataURL`), you need to strip out the data URL prefix then send the rest of the string to this API.
        /// For example, if the full string is represented by `data:image/svg+xml;base64,<rest of base64 string>`, remove `data:image/svg+xml;base64,`.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="base64File">- The base64 encoded content of an image or file to be added to an email or event.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`.
        ///  On success, the attachment identifier will be provided in the `asyncResult.value` property. 
        ///  If uploading the attachment fails, the `asyncResult` object will contain
        ///  an `Error` object that provides a description of the error.</param>
        abstract addFileAttachmentFromBase64Async: base64File: string * attachmentName: string * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Adds an event handler for a supported event. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should invoke the handler.</param>
        /// <param name="handler">- The function to handle the event. The function must accept a single parameter, which is an object literal. 
        /// The `type` property on the parameter will match the `eventType` parameter passed to `addHandlerAsync`.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract addHandlerAsync: eventType: U2<Office.EventType, string> * handler: obj option * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an event handler for a supported event. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should invoke the handler.</param>
        /// <param name="handler">- The function to handle the event. The function must accept a single parameter, which is an object literal. 
        /// The `type` property on the parameter will match the `eventType` parameter passed to `addHandlerAsync`.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract addHandlerAsync: eventType: U2<Office.EventType, string> * handler: obj option * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an Exchange item, such as a message, as an attachment to the message or appointment.
        /// 
        /// The `addItemAttachmentAsync` method attaches the item with the specified Exchange identifier to the item in the compose form. 
        /// If you specify a callback method, the method is called with one parameter, `asyncResult`, which contains either the attachment identifier or 
        /// a code that indicates any error that occurred while attaching the item. 
        /// You can use the `options` parameter to pass state information to the callback method, if needed.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// If your Office Add-in is running in Outlook on the web, the `addItemAttachmentAsync` method can attach items to items other than the item that 
        /// you are editing; however, this is not supported and is not recommended.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="itemId">- The Exchange identifier of the item to attach. The maximum length is 100 characters.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the callback parameter is called with a single parameter of
        ///             type `Office.AsyncResult`.
        /// On success, the attachment identifier will be provided in the `asyncResult.value` property. 
        /// If adding the attachment fails, the `asyncResult` object will contain
        /// an `Error` object that provides a description of the error.</param>
        abstract addItemAttachmentAsync: itemId: obj option * attachmentName: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Adds an Exchange item, such as a message, as an attachment to the message or appointment.
        /// 
        /// The `addItemAttachmentAsync` method attaches the item with the specified Exchange identifier to the item in the compose form. 
        /// If you specify a callback method, the method is called with one parameter, `asyncResult`, which contains either the attachment identifier or 
        /// a code that indicates any error that occurred while attaching the item. 
        /// You can use the `options` parameter to pass state information to the callback method, if needed.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// If your Office Add-in is running in Outlook on the web, the `addItemAttachmentAsync` method can attach items to items other than the item that 
        /// you are editing; however, this is not supported and is not recommended.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="itemId">- The Exchange identifier of the item to attach. The maximum length is 100 characters.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the callback parameter is called with a single parameter of
        ///             type `Office.AsyncResult`.
        /// On success, the attachment identifier will be provided in the `asyncResult.value` property. 
        /// If adding the attachment fails, the `asyncResult` object will contain
        /// an `Error` object that provides a description of the error.</param>
        abstract addItemAttachmentAsync: itemId: obj option * attachmentName: string * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// Closes the current item that is being composed
        /// 
        /// The behaviors of the `close` method depends on the current state of the item being composed. 
        /// If the item has unsaved changes, the client prompts the user to save, discard, or close the action.
        /// 
        /// In the Outlook desktop client, if the message is an inline reply, the `close` method has no effect.
        /// 
        /// **Note**: In Outlook on the web, if the item is an appointment and it has previously been saved using `saveAsync`, the user is prompted to save, 
        /// discard, or cancel even if no changes have occurred since the item was last saved.
        /// 
        /// [Api set: Mailbox 1.3]
        abstract close: unit -> unit
        /// <summary>Disables the Outlook client signature.
        /// 
        /// For Windows and Mac rich clients, this API sets the signature under the "New Message" and "Replies/Forwards" sections
        /// for the sending account to "(none)", effectively disabling the signature.
        /// For Outlook on the web, the API should disable the signature option for new mails, replies, and forwards.
        /// If the signature is selected, this API call should disable it.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the callback parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract disableClientSignatureAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Disables the Outlook client signature.
        /// 
        /// For Windows and Mac rich clients, this API sets the signature under the "New Message" and "Replies/Forwards" sections
        /// for the sending account to "(none)", effectively disabling the signature.
        /// For Outlook on the web, the API should disable the signature option for new mails, replies, and forwards.
        /// If the signature is selected, this API call should disable it.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the callback parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract disableClientSignatureAsync: ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Gets an attachment from a message or appointment and returns it as an `AttachmentContent` object.
        /// 
        /// The `getAttachmentContentAsync` method gets the attachment with the specified identifier from the item. As a best practice, you should use 
        /// the identifier to retrieve an attachment in the same session that the attachmentIds were retrieved with the `getAttachmentsAsync` or 
        /// `item.attachments` call. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session. 
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to 
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="attachmentId">- The identifier of the attachment you want to get.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object. If the call fails, the `asyncResult.error` property will contain
        /// an error code with the reason for the failure.</param>
        abstract getAttachmentContentAsync: attachmentId: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<AttachmentContent> -> unit) -> unit
        /// <summary>Gets an attachment from a message or appointment and returns it as an `AttachmentContent` object.
        /// 
        /// The `getAttachmentContentAsync` method gets the attachment with the specified identifier from the item. As a best practice, you should use 
        /// the identifier to retrieve an attachment in the same session that the attachmentIds were retrieved with the `getAttachmentsAsync` or 
        /// `item.attachments` call. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session. 
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to 
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="attachmentId">- The identifier of the attachment you want to get.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object. If the call fails, the `asyncResult.error` property will contain
        /// an error code with the reason for the failure.</param>
        abstract getAttachmentContentAsync: attachmentId: string * ?callback: (Office.AsyncResult<AttachmentContent> -> unit) -> unit
        /// <summary>Gets the item's attachments as an array.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If the call fails, the `asyncResult.error` property will contain an error code with the reason for
        /// the failure.</param>
        abstract getAttachmentsAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<ResizeArray<AttachmentDetailsCompose>> -> unit) -> unit
        /// <summary>Gets the item's attachments as an array.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If the call fails, the `asyncResult.error` property will contain an error code with the reason for
        /// the failure.</param>
        abstract getAttachmentsAsync: ?callback: (Office.AsyncResult<ResizeArray<AttachmentDetailsCompose>> -> unit) -> unit
        /// <summary>Asynchronously gets the ID of a saved item.
        /// 
        /// When invoked, this method returns the item ID via the callback method.
        /// 
        /// **Note**: If your add-in calls `getItemIdAsync` on an item in compose mode (e.g., to get an `itemId` to use with EWS or the REST API),
        /// be aware that when Outlook is in cached mode, it may take some time before the item is synced to the server.
        /// Until the item is synced, the `itemId` is not recognized and using it returns an error.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///   of type `Office.AsyncResult`.</param>
        abstract getItemIdAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously gets the ID of a saved item.
        /// 
        /// When invoked, this method returns the item ID via the callback method.
        /// 
        /// **Note**: If your add-in calls `getItemIdAsync` on an item in compose mode (e.g., to get an `itemId` to use with EWS or the REST API),
        /// be aware that when Outlook is in cached mode, it may take some time before the item is synced to the server.
        /// Until the item is synced, the `itemId` is not recognized and using it returns an error.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///   of type `Office.AsyncResult`.</param>
        abstract getItemIdAsync: callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously returns selected data from the subject or body of a message.
        /// 
        /// If there is no selection but the cursor is in the body or subject, the method returns an empty string for the selected data. 
        /// If a field other than the body or subject is selected, the method returns the `InvalidSelection` error.
        /// 
        /// To access the selected data from the callback method, call `asyncResult.value.data`. 
        /// To access the `source` property that the selection comes from, call `asyncResult.value.sourceProperty`, which will be either `body` or `subject`.
        /// 
        /// [Api set: Mailbox 1.2]</summary>
        /// <param name="coercionType">- Requests a format for the data. If `Text`, the method returns the plain text as a string, removing any HTML tags present. 
        /// If `HTML`, the method returns the selected text, whether it is plaintext or HTML.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///   of type `Office.AsyncResult`.</param>
        abstract getSelectedDataAsync: coercionType: U2<Office.CoercionType, string> * options: Office.AsyncContextOptions * callback: (Office.AsyncResult<obj option> -> unit) -> unit
        /// <summary>Asynchronously returns selected data from the subject or body of a message.
        /// 
        /// If there is no selection but the cursor is in the body or subject, the method returns an empty string for the selected data. 
        /// If a field other than the body or subject is selected, the method returns the `InvalidSelection` error.
        /// 
        /// To access the selected data from the callback method, call `asyncResult.value.data`. 
        /// To access the `source` property that the selection comes from, call `asyncResult.value.sourceProperty`, which will be either `body` or `subject`.
        /// 
        /// [Api set: Mailbox 1.2]</summary>
        /// <param name="coercionType">- Requests a format for the data. If `Text`, the method returns the plain text as a string, removing any HTML tags present. 
        /// If `HTML`, the method returns the selected text, whether it is plaintext or HTML.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.</param>
        abstract getSelectedDataAsync: coercionType: U2<Office.CoercionType, string> * callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Gets the properties of an appointment or message in a shared folder.
        /// 
        /// For more information around using this API, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.
        /// The `value` property of the result is the properties of the shared item.</param>
        abstract getSharedPropertiesAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<SharedProperties> -> unit) -> unit
        /// <summary>Gets the properties of an appointment or message in a shared folder.
        /// 
        /// For more information around using this API, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- When the method completes, the function passed in the callback parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.
        /// The `value` property of the result is the properties of the shared item.</param>
        abstract getSharedPropertiesAsync: callback: (Office.AsyncResult<SharedProperties> -> unit) -> unit
        /// <summary>Gets if the client signature is enabled.
        /// 
        /// For Windows and Mac rich clients, the API call should return `true` if the default signature for new messages, replies, or forwards is set
        /// to a template for the sending Outlook account.
        /// For Outlook on the web, the API call should return `true` if the signature is enabled for compose types `newMail`, `reply`, or `forward`.
        /// If the settings are set to "(none)" in Mac or Windows rich clients or disabled in Outlook on the Web, the API call should return `false`.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        ///   type `Office.AsyncResult`.</param>
        abstract isClientSignatureEnabledAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<bool> -> unit) -> unit
        /// <summary>Gets if the client signature is enabled.
        /// 
        /// For Windows and Mac rich clients, the API call should return `true` if the default signature for new messages, replies, or forwards is set
        /// to a template for the sending Outlook account.
        /// For Outlook on the web, the API call should return `true` if the signature is enabled for compose types `newMail`, `reply`, or `forward`.
        /// If the settings are set to "(none)" in Mac or Windows rich clients or disabled in Outlook on the Web, the API call should return `false`.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        ///   type `Office.AsyncResult`.</param>
        abstract isClientSignatureEnabledAsync: callback: (Office.AsyncResult<bool> -> unit) -> unit
        /// <summary>Asynchronously loads custom properties for this add-in on the selected item.
        /// 
        /// Custom properties are stored as key/value pairs on a per-app, per-item basis. 
        /// This method returns a `CustomProperties` object in the callback, which provides methods to access the custom properties specific to the 
        /// current item and the current add-in. Custom properties are not encrypted on the item, so this should not be used as secure storage.
        /// 
        /// The custom properties are provided as a `CustomProperties` object in the `asyncResult.value` property. 
        /// This object can be used to get, set, and remove custom properties from the item and save changes to the custom property set back to 
        /// the server.</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.</param>
        /// <param name="userContext">- Optional. Developers can provide any object they wish to access in the callback function. 
        /// This object can be accessed by the `asyncResult.asyncContext` property in the callback function.</param>
        abstract loadCustomPropertiesAsync: callback: (Office.AsyncResult<CustomProperties> -> unit) * ?userContext: obj -> unit
        /// <summary>Removes an attachment from a message or appointment.
        /// 
        /// The `removeAttachmentAsync` method removes the attachment with the specified identifier from the item. 
        /// As a best practice, you should use the attachment identifier to remove an attachment only if the same mail app has added that attachment 
        /// in the same session. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session. 
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to 
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="attachmentId">- The identifier of the attachment to remove. The maximum string length of the `attachmentId`
        ///   is 200 characters in Outlook on the web and on Windows.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        ///             type `Office.AsyncResult`. 
        /// If removing the attachment fails, the `asyncResult.error` property will contain an error code with the reason for the failure.</param>
        abstract removeAttachmentAsync: attachmentId: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes an attachment from a message or appointment.
        /// 
        /// The `removeAttachmentAsync` method removes the attachment with the specified identifier from the item. 
        /// As a best practice, you should use the attachment identifier to remove an attachment only if the same mail app has added that attachment 
        /// in the same session. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session. 
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to 
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="attachmentId">- The identifier of the attachment to remove. The maximum string length of the `attachmentId`
        ///   is 200 characters in Outlook on the web and on Windows.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        ///             type `Office.AsyncResult`. 
        /// If removing the attachment fails, the `asyncResult.error` property will contain an error code with the reason for the failure.</param>
        abstract removeAttachmentAsync: attachmentId: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes the event handlers for a supported event type. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should revoke the handler.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract removeHandlerAsync: eventType: U2<Office.EventType, string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes the event handlers for a supported event type. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should revoke the handler.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract removeHandlerAsync: eventType: U2<Office.EventType, string> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Asynchronously saves an item.
        /// 
        /// When invoked, this method saves the current message as a draft and returns the item ID via the callback method. 
        /// In Outlook on the web or Outlook in online mode, the item is saved to the server. 
        /// In Outlook in cached mode, the item is saved to the local cache.
        /// 
        /// Since appointments have no draft state, if `saveAsync` is called on an appointment in compose mode, the item will be saved as a normal 
        /// appointment on the user's calendar. For new appointments that have not been saved before, no invitation will be sent. 
        /// Saving an existing appointment will send an update to added or removed attendees.
        /// 
        /// **Note**: If your add-in calls `saveAsync` on an item in compose mode in order to get an item ID to use with EWS or the REST API, be aware
        /// that when Outlook is in cached mode, it may take some time before the item is actually synced to the server. 
        /// Until the item is synced, using the item ID will return an error.
        /// 
        /// **Note**: In Outlook on Mac, only build 16.35.308 or later supports saving a meeting.
        /// Otherwise, the `saveAsync` method fails when called from a meeting in compose mode.
        /// For a workaround, see {@link https://support.microsoft.com/help/4505745 | Cannot save a meeting as a draft in Outlook for Mac by using Office JS API}.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        ///   type `Office.AsyncResult`.</param>
        abstract saveAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously saves an item.
        /// 
        /// When invoked, this method saves the current message as a draft and returns the item ID via the callback method. 
        /// In Outlook on the web or Outlook in online mode, the item is saved to the server. In Outlook in cached mode, the item is saved to the local cache.
        /// 
        /// Since appointments have no draft state, if `saveAsync` is called on an appointment in compose mode, the item will be saved as a normal 
        /// appointment on the user's calendar. For new appointments that have not been saved before, no invitation will be sent. 
        /// Saving an existing appointment will send an update to added or removed attendees.
        /// 
        /// **Note**: If your add-in calls `saveAsync` on an item in compose mode in order to get an item ID to use with EWS or the REST API, be aware that 
        /// when Outlook is in cached mode, it may take some time before the item is actually synced to the server. 
        /// Until the item is synced, using the item ID will return an error.
        /// 
        /// **Note**: In Outlook on Mac, only build 16.35.308 or later supports saving a meeting.
        /// Otherwise, the `saveAsync` method fails when called from a meeting in compose mode.
        /// For a workaround, see {@link https://support.microsoft.com/help/4505745 | Cannot save a meeting as a draft in Outlook for Mac by using Office JS API}.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="callback">- When the method completes, the function passed in the callback parameter is called with a single parameter of
        ///   type `Office.AsyncResult`.</param>
        abstract saveAsync: callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously inserts data into the body or subject of a message.
        /// 
        /// The `setSelectedDataAsync` method inserts the specified string at the cursor location in the subject or body of the item, or, if text is 
        /// selected in the editor, it replaces the selected text. If the cursor is not in the body or subject field, an error is returned. 
        /// After insertion, the cursor is placed at the end of the inserted content.
        /// 
        /// [Api set: Mailbox 1.2]</summary>
        /// <param name="data">- The data to be inserted. Data is not to exceed 1,000,000 characters. 
        /// If more than 1,000,000 characters are passed in, an `ArgumentOutOfRange` exception is thrown.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `coercionType`: If text, the current style is applied in Outlook on the web and Windows. 
        ///       If the field is an HTML editor, only the text data is inserted, even if the data is HTML. 
        ///       If html and the field supports HTML (the subject doesn't), the current style is applied in Outlook on the web and the 
        ///       default style is applied in Outlook on desktop clients. 
        ///       If the field is a text field, an `InvalidDataFormat` error is returned. 
        ///       If `coercionType` is not set, the result depends on the field: if the field is HTML then HTML is used; 
        ///       if the field is text, then plain text is used.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.</param>
        abstract setSelectedDataAsync: data: string * options: obj * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Asynchronously inserts data into the body or subject of a message.
        /// 
        /// The `setSelectedDataAsync` method inserts the specified string at the cursor location in the subject or body of the item, or, if text is 
        /// selected in the editor, it replaces the selected text. If the cursor is not in the body or subject field, an error is returned. 
        /// After insertion, the cursor is placed at the end of the inserted content.
        /// 
        /// [Api set: Mailbox 1.2]</summary>
        /// <param name="data">- The data to be inserted. Data is not to exceed 1,000,000 characters. 
        /// If more than 1,000,000 characters are passed in, an `ArgumentOutOfRange` exception is thrown.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.</param>
        abstract setSelectedDataAsync: data: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// The `AppointmentForm` object is used to access the currently selected appointment.
    type [<AllowNullLiteral>] AppointmentForm =
        /// Gets an object that provides methods for manipulating the body of an item.
        /// 
        /// [Api set: Mailbox 1.1]
        abstract body: U2<Body, string> with get, set
        /// Gets or sets the date and time that the appointment is to end.
        /// 
        /// The `end` property is expressed as a Coordinated Universal Time (UTC) date and time value. You can use the `convertToLocalClientTime` method to 
        /// convert the `end` property value to the client's local date and time.
        /// 
        /// *Read mode*
        /// 
        /// The `end` property returns a `Date` object.
        /// 
        /// *Compose mode*
        /// 
        /// The `end` property returns a `Time` object.
        /// 
        /// When you use the `Time.setAsync` method to set the end time, you should use the `convertToUtcClientTime` method to convert the local time on 
        /// the client to UTC for the server.
        abstract ``end``: U2<Time, DateTime> with get, set
        /// Gets or sets the location of an appointment.
        /// 
        /// *Read mode*
        /// 
        /// The `location` property returns a string that contains the location of the appointment.
        /// 
        /// *Compose mode*
        /// 
        /// The `location` property returns a `Location` object that provides methods that are used to get and set the location of the appointment.
        abstract location: U2<Location, string> with get, set
        /// Provides access to the optional attendees of an event. The type of object and level of access depend on the mode of the current item.
        /// 
        /// *Read mode*
        /// 
        /// The `optionalAttendees` property returns an array that contains an {@link Office.EmailAddressDetails | EmailAddressDetails} object for
        /// each optional attendee to the meeting. Collection size limits:
        /// 
        /// - Windows: 500 members
        /// 
        /// - Mac: 100 members
        /// 
        /// - Other: No limit
        /// 
        /// *Compose mode*
        /// 
        /// The `optionalAttendees` property returns a `Recipients` object that provides methods to get or update the
        /// optional attendees for a meeting. However, depending on the client/platform (i.e., Windows, Mac, etc.), limits may apply on how many
        /// recipients you can get or update. See the {@link Office.Recipients | Recipients} object for more details.
        abstract optionalAttendees: U2<ResizeArray<Recipients>, ResizeArray<EmailAddressDetails>> with get, set
        /// Provides access to the resources of an event. Returns an array of strings containing the resources required for the appointment.
        abstract resources: ResizeArray<string> with get, set
        /// Provides access to the required attendees of an event. The type of object and level of access depend on the mode of the current item.
        /// 
        /// *Read mode*
        /// 
        /// The `requiredAttendees` property returns an array that contains an {@link Office.EmailAddressDetails | EmailAddressDetails} object for
        /// each required attendee to the meeting. Collection size limits:
        /// 
        /// - Windows: 500 members
        /// 
        /// - Mac: 100 members
        /// 
        /// - Other: No limit
        /// 
        /// *Compose mode*
        /// 
        /// The `requiredAttendees` property returns a `Recipients` object that provides methods to get or update the
        /// required attendees for a meeting. However, depending on the client/platform (i.e., Windows, Mac, etc.), limits may apply on how many
        /// recipients you can get or update. See the {@link Office.Recipients | Recipients} object for more details.
        abstract requiredAttendees: U2<ResizeArray<Recipients>, ResizeArray<EmailAddressDetails>> with get, set
        /// Gets or sets the date and time that the appointment is to begin.
        /// 
        /// The `start` property is expressed as a Coordinated Universal Time (UTC) date and time value. You can use the `convertToLocalClientTime` method
        /// to convert the value to the client's local date and time.
        /// 
        /// *Read mode*
        /// 
        /// The `start` property returns a `Date` object.
        /// 
        /// *Compose mode*
        /// 
        /// The `start` property returns a `Time` object.
        /// 
        /// When you use the `Time.setAsync` method to set the start time, you should use the `convertToUtcClientTime` method to convert the local time on
        /// the client to UTC for the server.
        abstract start: U2<Time, DateTime> with get, set
        /// Gets or sets the description that appears in the subject field of an item.
        /// 
        /// The `subject` property gets or sets the entire subject of the item, as sent by the email server.
        /// 
        /// *Read mode*
        /// 
        /// The `subject` property returns a string. Use the `normalizedSubject` property to get the subject minus any leading prefixes such as RE: and FW:.
        /// 
        /// *Compose mode*
        /// 
        /// The `subject` property returns a `Subject` object that provides methods to get and set the subject.
        abstract subject: U2<Subject, string> with get, set

    /// The appointment attendee mode of {@link Office.Item | Office.context.mailbox.item}.
    /// 
    /// **Important**: This is an internal Outlook object, not directly exposed through existing interfaces. 
    /// You should treat this as a mode of `Office.context.mailbox.item`. For more information, refer to the
    /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item | Object Model} page.
    /// 
    /// Parent interfaces:
    /// 
    /// - {@link Office.ItemRead | ItemRead}
    /// 
    /// - {@link Office.Appointment | Appointment}
    type [<AllowNullLiteral>] AppointmentRead =
        inherit Appointment
        inherit ItemRead
        /// Gets the item's attachments as an array.
        abstract attachments: ResizeArray<AttachmentDetails> with get, set
        /// Gets an object that provides methods for manipulating the body of an item.
        /// 
        /// [Api set: Mailbox 1.1]
        abstract body: Body with get, set
        /// Gets an object that provides methods for managing the item's categories.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract categories: Categories with get, set
        /// Gets the date and time that an item was created.
        abstract dateTimeCreated: DateTime with get, set
        /// Gets the date and time that an item was last modified.
        /// 
        /// **Note**: This member is not supported in Outlook on iOS or Android.
        abstract dateTimeModified: DateTime with get, set
        /// Gets the date and time that the appointment is to end.
        /// 
        /// The `end` property is a `Date` object expressed as a Coordinated Universal Time (UTC) date and time value. 
        /// You can use the `convertToLocalClientTime` method to convert the `end` property value to the client's local date and time.
        /// 
        /// When you use the `Time.setAsync` method to set the end time, you should use the `convertToUtcClientTime` method to convert the local time on 
        /// the client to UTC for the server.
        abstract ``end``: DateTime with get, set
        /// Gets the locations of an appointment.
        /// 
        /// The `enhancedLocation` property returns an {@link Office.EnhancedLocation | EnhancedLocation} object that allows you to get the set of locations
        /// (each represented by a {@link Office.LocationDetails | LocationDetails} object) associated with the appointment.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract enhancedLocation: EnhancedLocation with get, set
        /// Gets the Exchange Web Services item class of the selected item.
        /// 
        /// You can create custom message classes that extends a default message class, for example, a custom appointment message class `IPM.Appointment.Contoso`.
        abstract itemClass: string with get, set
        /// Gets the {@link https://docs.microsoft.com/exchange/client-developer/exchange-web-services/ews-identifiers-in-exchange | Exchange Web Services item identifier}
        /// for the current item.
        /// 
        /// The `itemId` property is not available in compose mode. 
        /// If an item identifier is required, the `saveAsync` method can be used to save the item to the store, which will return the item identifier 
        /// in the `asyncResult.value` parameter in the callback function.
        /// 
        /// **Note**: The identifier returned by the `itemId` property is the same as the
        /// {@link https://docs.microsoft.com/exchange/client-developer/exchange-web-services/ews-identifiers-in-exchange | Exchange Web Services item identifier}. 
        /// The `itemId` property is not identical to the Outlook Entry ID or the ID used by the Outlook REST API. 
        /// Before making REST API calls using this value, it should be converted using `Office.context.mailbox.convertToRestId`. 
        /// For more details, see {@link https://docs.microsoft.com/office/dev/add-ins/outlook/use-rest-api#get-the-item-id | Use the Outlook REST APIs from an Outlook add-in}.
        abstract itemId: string with get, set
        /// Gets the type of item that an instance represents.
        /// 
        /// The `itemType` property returns one of the `ItemType` enumeration values, indicating whether the item object instance is a message or an appointment.
        abstract itemType: U2<MailboxEnums.ItemType, string> with get, set
        /// Gets the location of an appointment.
        /// 
        /// The `location` property returns a string that contains the location of the appointment.
        abstract location: string with get, set
        /// Gets the subject of an item, with all prefixes removed (including RE: and FWD:).
        /// 
        /// The `normalizedSubject` property gets the subject of the item, with any standard prefixes (such as RE: and FW:) that are added by email programs. 
        /// To get the subject of the item with the prefixes intact, use the `subject` property.
        abstract normalizedSubject: string with get, set
        /// Gets the notification messages for an item.
        /// 
        /// [Api set: Mailbox 1.3]
        abstract notificationMessages: NotificationMessages with get, set
        /// Provides access to the optional attendees of an event. The type of object and level of access depend on the mode of the current item.
        /// 
        /// The `optionalAttendees` property returns an array that contains an {@link Office.EmailAddressDetails | EmailAddressDetails} object for
        /// each optional attendee to the meeting. Collection size limits:
        /// 
        /// - Windows: 500 members
        /// 
        /// - Mac: 100 members
        /// 
        /// - Other: No limit
        abstract optionalAttendees: ResizeArray<EmailAddressDetails> with get, set
        /// Gets the email address of the meeting organizer for a specified meeting.
        abstract organizer: EmailAddressDetails with get, set
        /// Gets the recurrence pattern of an appointment. Gets the recurrence pattern of a meeting request.
        /// 
        /// The `recurrence` property returns a {@link Office.Recurrence | Recurrence} object for recurring appointments or meetings requests
        /// if an item is a series or an instance in a series. `null` is returned for single appointments and meeting requests of single appointments.
        /// 
        /// **Note**: Meeting requests have an `itemClass` value of `IPM.Schedule.Meeting.Request`.
        /// 
        /// **Note**: If the recurrence object is null, this indicates that the object is a single appointment or a meeting request of a single 
        /// appointment and NOT a part of a series.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract recurrence: Recurrence with get, set
        /// Provides access to the required attendees of an event. The type of object and level of access depend on the mode of the current item.
        /// 
        /// The `requiredAttendees` property returns an array that contains an {@link Office.EmailAddressDetails | EmailAddressDetails} object for
        /// each required attendee to the meeting. Collection size limits:
        /// 
        /// - Windows: 500 members
        /// 
        /// - Mac: 100 members
        /// 
        /// - Other: No limit
        abstract requiredAttendees: ResizeArray<EmailAddressDetails> with get, set
        /// Gets the date and time that the appointment is to begin.
        /// 
        /// The `start` property is a `Date` object expressed as a Coordinated Universal Time (UTC) date and time value. 
        /// You can use the `convertToLocalClientTime` method to convert the value to the client's local date and time.
        abstract start: DateTime with get, set
        /// Gets the ID of the series that an instance belongs to.
        /// 
        /// In Outlook on the web and desktop clients, the `seriesId` returns the Exchange Web Services (EWS) ID of the parent (series) item
        /// that this item belongs to. However, on iOS and Android, the seriesId returns the REST ID of the parent item.
        /// 
        /// **Note**: The identifier returned by the `seriesId` property is the same as the Exchange Web Services item identifier. 
        /// The `seriesId` property is not identical to the Outlook IDs used by the Outlook REST API. Before making REST API calls using this value, it 
        /// should be converted using `Office.context.mailbox.convertToRestId`. 
        /// For more details, see {@link https://docs.microsoft.com/office/dev/add-ins/outlook/use-rest-api | Use the Outlook REST APIs from an Outlook add-in}.
        /// 
        /// The `seriesId` property returns `null` for items that do not have parent items such as single appointments, series items, or meeting requests 
        /// and returns `undefined` for any other items that are not meeting requests.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract seriesId: string with get, set
        /// Gets the description that appears in the subject field of an item.
        /// 
        /// The `subject` property gets or sets the entire subject of the item, as sent by the email server.
        /// 
        /// The `subject` property returns a string. Use the `normalizedSubject` property to get the subject minus any leading prefixes such as RE: and FW:.
        abstract subject: string with get, set
        /// <summary>Adds an event handler for a supported event. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should invoke the handler.</param>
        /// <param name="handler">- The function to handle the event. The function must accept a single parameter, which is an object literal. 
        /// The `type` property on the parameter will match the `eventType` parameter passed to `addHandlerAsync`.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract addHandlerAsync: eventType: U2<Office.EventType, string> * handler: obj option * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an event handler for a supported event. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should invoke the handler.</param>
        /// <param name="handler">- The function to handle the event. The function must accept a single parameter, which is an object literal. 
        /// The `type` property on the parameter will match the `eventType` parameter passed to `addHandlerAsync`.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract addHandlerAsync: eventType: U2<Office.EventType, string> * handler: obj option * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a reply form that includes either the sender and all recipients of the selected message or the organizer and all attendees of the
        /// selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyAllForm` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        abstract displayReplyAllForm: formData: U2<string, ReplyFormData> -> unit
        /// <summary>Displays a reply form that includes either the sender and all recipients of the selected message or the organizer and all attendees of the
        /// selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyAllFormAsync` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayReplyAllFormAsync: formData: U2<string, ReplyFormData> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a reply form that includes either the sender and all recipients of the selected message or the organizer and all attendees of the
        /// selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyAllFormAsync` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayReplyAllFormAsync: formData: U2<string, ReplyFormData> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a reply form that includes only the sender of the selected message or the organizer of the selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyForm` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        abstract displayReplyForm: formData: U2<string, ReplyFormData> -> unit
        /// <summary>Displays a reply form that includes only the sender of the selected message or the organizer of the selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyFormAsync` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayReplyFormAsync: formData: U2<string, ReplyFormData> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a reply form that includes only the sender of the selected message or the organizer of the selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyFormAsync` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayReplyFormAsync: formData: U2<string, ReplyFormData> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Gets an attachment from a message or appointment and returns it as an `AttachmentContent` object.
        /// 
        /// The `getAttachmentContentAsync` method gets the attachment with the specified identifier from the item. As a best practice, you should use 
        /// the identifier to retrieve an attachment in the same session that the attachmentIds were retrieved with the `getAttachmentsAsync` or 
        /// `item.attachments` call. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session. 
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to 
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="attachmentId">- The identifier of the attachment you want to get.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object. If the call fails, the `asyncResult.error` property will contain
        /// an error code with the reason for the failure.</param>
        abstract getAttachmentContentAsync: attachmentId: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<AttachmentContent> -> unit) -> unit
        /// <summary>Gets an attachment from a message or appointment and returns it as an `AttachmentContent` object.
        /// 
        /// The `getAttachmentContentAsync` method gets the attachment with the specified identifier from the item. As a best practice, you should use 
        /// the identifier to retrieve an attachment in the same session that the attachmentIds were retrieved with the `getAttachmentsAsync` or 
        /// `item.attachments` call. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session. 
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to 
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="attachmentId">- The identifier of the attachment you want to get.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object. If the call fails, the `asyncResult.error` property will contain
        /// an error code with the reason for the failure.</param>
        abstract getAttachmentContentAsync: attachmentId: string * ?callback: (Office.AsyncResult<AttachmentContent> -> unit) -> unit
        /// Gets the entities found in the selected item's body.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        abstract getEntities: unit -> Entities
        /// <summary>Gets an array of all the entities of the specified entity type found in the selected item's body.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="entityType">- One of the `EntityType` enumeration values.
        /// 
        /// While the minimum permission level to use this method is `Restricted`, some entity types require `ReadItem` to access, as specified in the following table.
        /// 
        /// <table>
        /// <tr>
        /// <th>Value of entityType</th>
        /// <th>Type of objects in returned array</th>
        /// <th>Required Permission Level</th>
        /// </tr>
        /// <tr>
        /// <td>Address</td>
        /// <td>String</td>
        /// <td>Restricted</td>
        /// </tr>
        /// <tr>
        /// <td>Contact</td>
        /// <td>Contact</td>
        /// <td>ReadItem</td>
        /// </tr>
        /// <tr>
        /// <td>EmailAddress</td>
        /// <td>String</td>
        /// <td>ReadItem</td>
        /// </tr>
        /// <tr>
        /// <td>MeetingSuggestion</td>
        /// <td>MeetingSuggestion</td>
        /// <td>ReadItem</td>
        /// </tr>
        /// <tr>
        /// <td>PhoneNumber</td>
        /// <td>PhoneNumber</td>
        /// <td>Restricted</td>
        /// </tr>
        /// <tr>
        /// <td>TaskSuggestion</td>
        /// <td>TaskSuggestion</td>
        /// <td>ReadItem</td>
        /// </tr>
        /// <tr>
        /// <td>URL</td>
        /// <td>String</td>
        /// <td>Restricted</td>
        /// </tr>
        /// </table></param>
        abstract getEntitiesByType: entityType: U2<MailboxEnums.EntityType, string> -> ResizeArray<U5<string, Contact, MeetingSuggestion, PhoneNumber, TaskSuggestion>>
        /// <summary>Returns well-known entities in the selected item that pass the named filter defined in the manifest XML file.
        /// 
        /// The `getFilteredEntitiesByName` method returns the entities that match the regular expression defined in the `ItemHasKnownEntity` rule element 
        /// in the manifest XML file with the specified `FilterName` element value.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="name">- The name of the `ItemHasKnownEntity` rule element that defines the filter to match.</param>
        abstract getFilteredEntitiesByName: name: string -> ResizeArray<U5<string, Contact, MeetingSuggestion, PhoneNumber, TaskSuggestion>>
        /// Returns string values in the selected item that match the regular expressions defined in the manifest XML file.
        /// 
        /// The `getRegExMatches` method returns the strings that match the regular expression defined in each `ItemHasRegularExpressionMatch` or
        /// `ItemHasKnownEntity` rule element in the manifest XML file. 
        /// For an `ItemHasRegularExpressionMatch` rule, a matching string has to occur in the property of the item that is specified by that rule.
        /// The `PropertyName` simple type defines the supported properties.
        /// 
        /// If you specify an `ItemHasRegularExpressionMatch` rule on the body property of an item, the regular expression should further filter the body
        /// and should not attempt to return the entire body of the item. 
        /// Using a regular expression such as .* to obtain the entire body of an item does not always return the expected results. 
        /// Instead, use the `Body.getAsync` method to retrieve the entire body.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        abstract getRegExMatches: unit -> obj option
        /// <summary>Returns string values in the selected item that match the named regular expression defined in the manifest XML file.
        /// 
        /// The `getRegExMatchesByName` method returns the strings that match the regular expression defined in the `ItemHasRegularExpressionMatch` rule
        /// element in the manifest XML file with the specified `RegExName` element value.
        /// 
        /// If you specify an `ItemHasRegularExpressionMatch` rule on the body property of an item, the regular expression should further filter the body
        /// and should not attempt to return the entire body of the item. 
        /// Using a regular expression such as .* to obtain the entire body of an item does not always return the expected results.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="name">- The name of the `ItemHasRegularExpressionMatch` rule element that defines the filter to match.</param>
        abstract getRegExMatchesByName: name: string -> ResizeArray<string>
        /// Gets the entities found in a highlighted match a user has selected. Highlighted matches apply to contextual add-ins.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.6]
        abstract getSelectedEntities: unit -> Entities
        /// Returns string values in a highlighted match that match the regular expressions defined in the manifest XML file. 
        /// Highlighted matches apply to contextual add-ins.
        /// 
        /// The `getSelectedRegExMatches` method returns the strings that match the regular expression defined in each `ItemHasRegularExpressionMatch` or
        /// `ItemHasKnownEntity` rule element in the manifest XML file.
        /// For an `ItemHasRegularExpressionMatch` rule, a matching string has to occur in the property of the item that is specified by that rule.
        /// The `PropertyName` simple type defines the supported properties.
        /// 
        /// If you specify an `ItemHasRegularExpressionMatch` rule on the body property of an item, the regular expression should further filter the body
        /// and should not attempt to return the entire body of the item.
        /// Using a regular expression such as .* to obtain the entire body of an item does not always return the expected results.
        /// Instead, use the `Body.getAsync` method to retrieve the entire body.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.6]
        abstract getSelectedRegExMatches: unit -> obj option
        /// <summary>Gets the properties of an appointment or message in a shared folder.
        /// 
        /// For more information around using this API, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.
        /// The `value` property of the result is the properties of the shared item.</param>
        abstract getSharedPropertiesAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<SharedProperties> -> unit) -> unit
        /// <summary>Gets the properties of an appointment or message in a shared folder.
        /// 
        /// For more information around using this API, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- When the method completes, the function passed in the callback parameter is called with a single parameter of
        /// type `Office.AsyncResult`.
        /// The `value` property of the result is the properties of the shared item.</param>
        abstract getSharedPropertiesAsync: callback: (Office.AsyncResult<SharedProperties> -> unit) -> unit
        /// <summary>Asynchronously loads custom properties for this add-in on the selected item.
        /// 
        /// Custom properties are stored as key/value pairs on a per-app, per-item basis.
        /// This method returns a `CustomProperties` object in the callback, which provides methods to access the custom properties specific to the
        /// current item and the current add-in. Custom properties are not encrypted on the item, so this should not be used as secure storage.
        /// 
        /// The custom properties are provided as a `CustomProperties` object in the asyncResult.value property.
        /// This object can be used to get, set, and remove custom properties from the item and save changes to the custom property set back to
        /// the server.</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        /// <param name="userContext">- Optional. Developers can provide any object they wish to access in the callback function.
        /// This object can be accessed by the `asyncResult.asyncContext` property in the callback function.</param>
        abstract loadCustomPropertiesAsync: callback: (Office.AsyncResult<CustomProperties> -> unit) * ?userContext: obj -> unit
        /// <summary>Removes the event handlers for a supported event type. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should revoke the handler.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract removeHandlerAsync: eventType: U2<Office.EventType, string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes the event handlers for a supported event type. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should revoke the handler.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract removeHandlerAsync: eventType: U2<Office.EventType, string> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Provides the current dates and times of the appointment that raised the `Office.EventType.AppointmentTimeChanged` event. 
    /// 
    /// [Api set: Mailbox 1.7]
    type [<AllowNullLiteral>] AppointmentTimeChangedEventArgs =
        /// Gets the appointment end date and time. 
        /// 
        /// [Api set: Mailbox 1.7]
        abstract ``end``: DateTime with get, set
        /// Gets the appointment start date and time. 
        /// 
        /// [Api set: Mailbox 1.7]
        abstract start: DateTime with get, set
        /// Gets the type of the event. For details, refer to {@link https://docs.microsoft.com/javascript/api/office/office.eventtype | Office.EventType}.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract ``type``: string with get, set

    /// Represents the content of an attachment on a message or appointment item.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] AttachmentContent =
        /// The content of an attachment as a string.
        abstract content: string with get, set
        /// The string format to use for an attachment's content.
        /// 
        /// For file attachments, the formatting is a base64-encoded string.
        /// 
        /// For item attachments that represent messages and were attached by drag-and-drop or "Attach Item",
        /// the formatting is a string representing an .eml formatted file.
        /// **Important**: If a message item was attached by drag-and-drop in Outlook on the web, then `getAttachmentContentAsync` throws an error.
        /// 
        /// For item attachments that represent calendar items and were attached by drag-and-drop or "Attach Item",
        /// the formatting is a string representing an .icalendar file.
        /// **Important**: If a calendar item was attached by drag-and-drop in Outlook on the web, then `getAttachmentContentAsync` throws an error.
        /// 
        /// For cloud attachments, the formatting is a URL string.
        abstract format: U2<MailboxEnums.AttachmentContentFormat, string> with get, set

    /// Represents an attachment on an item. Compose mode only.
    /// 
    /// An array of `AttachmentDetailsCompose` objects is returned as the attachments property of an appointment or message item.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] AttachmentDetailsCompose =
        /// Gets a value that indicates the type of an attachment.
        abstract attachmentType: U2<MailboxEnums.AttachmentType, string> with get, set
        /// Gets the index of the attachment.
        abstract id: string with get, set
        /// Gets a value that indicates whether the attachment should be displayed in the body of the item.
        abstract isInline: bool with get, set
        /// Gets the name of the attachment.
        /// 
        /// **Important**: For message or appointment items that were attached by drag-and-drop or "Attach Item",
        /// `name` includes a file extension in Outlook on Mac, but excludes the extension on the web or Windows.
        abstract name: string with get, set
        /// Gets the size of the attachment in bytes.
        abstract size: float with get, set
        /// Gets the url of the attachment if its type is `MailboxEnums.AttachmentType.Cloud`.
        abstract url: string option with get, set

    /// Represents an attachment on an item from the server. Read mode only.
    /// 
    /// An array of `AttachmentDetails` objects is returned as the attachments property of an appointment or message item.
    /// 
    /// [Api set: Mailbox 1.1]
    type [<AllowNullLiteral>] AttachmentDetails =
        /// Gets a value that indicates the type of an attachment.
        abstract attachmentType: U2<MailboxEnums.AttachmentType, string> with get, set
        /// Gets the MIME content type of the attachment.
        /// 
        /// **Important**: While the `contentType` value is a direct lookup of the attachment's extension, the internal mapping isn't actively maintained.
        /// If you require specific types, grab the attachment's extension and process accordingly.
        abstract contentType: string with get, set
        /// Gets the Exchange attachment ID of the attachment.
        /// However, if the attachment type is `MailboxEnums.AttachmentType.Cloud`, then a URL for the file is returned.
        abstract id: string with get, set
        /// Gets a value that indicates whether the attachment should be displayed in the body of the item.
        abstract isInline: bool with get, set
        /// Gets the name of the attachment.
        /// 
        /// **Important**: For message or appointment items that were attached by drag-and-drop or "Attach Item",
        /// `name` includes a file extension in Outlook on Mac, but excludes the extension on the web or Windows.
        abstract name: string with get, set
        /// Gets the size of the attachment in bytes.
        abstract size: float with get, set

    /// Provides information about the attachments that raised the `Office.EventType.AttachmentsChanged` event.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] AttachmentsChangedEventArgs =
        /// Represents the set of attachments that were added or removed. 
        /// For each such attachment, gets `id`, `name`, `size`, and `attachmentType` properties.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract attachmentDetails: ResizeArray<obj> with get, set
        /// Gets whether the attachments were added or removed. For details, refer to {@link Office.MailboxEnums.AttachmentStatus | MailboxEnums.AttachmentStatus}.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract attachmentStatus: U2<MailboxEnums.AttachmentStatus, string> with get, set
        /// Gets the type of the event. For details, refer to {@link https://docs.microsoft.com/javascript/api/office/office.eventtype | Office.EventType}.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract ``type``: string with get, set

    /// The body object provides methods for adding and updating the content of the message or appointment. 
    /// It is returned in the body property of the selected item.
    /// 
    /// [Api set: Mailbox 1.1]
    type [<AllowNullLiteral>] Body =
        /// <summary>Appends on send the specified content to the end of the item body, after any signature.
        /// 
        /// If the user is running add-ins that implement the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/outlook-on-send-addins?tabs=windows | on-send feature using `ItemSend` in the manifest},
        /// append-on-send runs before on-send functionality.
        /// 
        /// **Important**: If your add-in implements the on-send feature and calls `appendOnSendAsync` in the `ItemSend` handler,
        /// the `appendOnSendAsync` call returns an error as this scenario is not supported.
        /// 
        /// **Important**: To use `appendOnSendAsync`, the `ExtendedPermissions` manifest node must include the `AppendOnSend` extended permission.
        /// 
        /// **Note**: To clear data from a previous `appendOnSendAsync` call, you can call it again with the `data` parameter set to `null`.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="data">- The string to be added to the end of the body. The string is limited to 5,000 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `coercionType`: The desired format for the data to be appended. The string in the `data` parameter will be converted to this format.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`. Any errors encountered will be provided in the `asyncResult.error` property.</param>
        abstract appendOnSendAsync: data: string * options: obj * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Appends on send the specified content to the end of the item body, after any signature.
        /// 
        /// If the user is running add-ins that implement the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/outlook-on-send-addins?tabs=windows | on-send feature using `ItemSend` in the manifest},
        /// append-on-send runs before on-send functionality.
        /// 
        /// **Important**: If your add-in implements the on-send feature and calls `appendOnSendAsync` in the `ItemSend` handler,
        /// the `appendOnSendAsync` call returns an error as this scenario is not supported.
        /// 
        /// **Important**: To use `appendOnSendAsync`, the `ExtendedPermissions` manifest node must include the `AppendOnSend` extended permission.
        /// 
        /// **Note**: To clear data from a previous `appendOnSendAsync` call, you can call it again with the `data` parameter set to `null`.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="data">- The string to be added to the end of the body. The string is limited to 5,000 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`. Any errors encountered will be provided in the `asyncResult.error` property.</param>
        abstract appendOnSendAsync: data: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Returns the current body in a specified format.
        /// 
        /// This method returns the entire current body in the format specified by `coercionType`.
        /// 
        /// When working with HTML-formatted bodies, it is important to note that the `Body.getAsync` and `Body.setAsync` methods are not idempotent.
        /// The value returned from the `getAsync` method will not necessarily be exactly the same as the value that was passed in the `setAsync` method previously. 
        /// The client may modify the value passed to `setAsync` in order to make it render efficiently with its rendering engine.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="coercionType">- The format for the returned body.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties:
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type Office.AsyncResult. The body is provided in the requested format in the `asyncResult.value` property.</param>
        abstract getAsync: coercionType: U2<Office.CoercionType, string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Returns the current body in a specified format.
        /// 
        /// This method returns the entire current body in the format specified by `coercionType`.
        /// 
        /// When working with HTML-formatted bodies, it is important to note that the `Body.getAsync` and `Body.setAsync` methods are not idempotent.
        /// The value returned from the `getAsync` method will not necessarily be exactly the same as the value that was passed in the `setAsync` method previously. 
        /// The client may modify the value passed to `setAsync` in order to make it render efficiently with its rendering engine.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="coercionType">- The format for the returned body.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type Office.AsyncResult. The body is provided in the requested format in the `asyncResult.value` property.</param>
        abstract getAsync: coercionType: U2<Office.CoercionType, string> * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Gets a value that indicates whether the content is in HTML or text format.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`.
        ///  The content type is returned as one of the `CoercionType` values in the `asyncResult.value` property.</param>
        abstract getTypeAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<Office.CoercionType> -> unit) -> unit
        /// <summary>Gets a value that indicates whether the content is in HTML or text format.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`.
        ///  The content type is returned as one of the `CoercionType` values in the `asyncResult.value` property.</param>
        abstract getTypeAsync: ?callback: (Office.AsyncResult<Office.CoercionType> -> unit) -> unit
        /// <summary>Adds the specified content to the beginning of the item body.
        /// 
        /// The `prependAsync` method inserts the specified string at the beginning of the item body.
        /// After insertion, the cursor is returned to its original place, relative to the inserted content.
        /// 
        /// When working with HTML-formatted bodies, it's important to note that the client may modify the value passed to `prependAsync` in order to
        /// make it render efficiently with its rendering engine. This means that the value returned from a subsequent call to `Body.getAsync` method
        /// will not necessarily exactly contain the value that was passed in the `prependAsync` method previously.
        /// 
        /// When including links in HTML markup, you can disable online link preview by setting the `id` attribute on the anchor (\<a\>) to "LPNoLP"
        /// (see the **Examples** section for a sample).
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="data">- The string to be inserted at the beginning of the body. The string is limited to 1,000,000 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `coercionType`: The desired format for the body. The string in the `data` parameter will be converted to this format.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`. Any errors encountered will be provided in the `asyncResult.error` property.</param>
        abstract prependAsync: data: string * options: obj * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds the specified content to the beginning of the item body.
        /// 
        /// The `prependAsync` method inserts the specified string at the beginning of the item body.
        /// After insertion, the cursor is returned to its original place, relative to the inserted content.
        /// 
        /// When working with HTML-formatted bodies, it's important to note that the client may modify the value passed to `prependAsync` in order to
        /// make it render efficiently with its rendering engine. This means that the value returned from a subsequent call to `Body.getAsync` method
        /// will not necessarily exactly contain the value that was passed in the `prependAsync` method previously.
        /// 
        /// When including links in HTML markup, you can disable online link preview by setting the `id` attribute on the anchor (\<a\>) to "LPNoLP"
        /// (see the **Examples** section for a sample).
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="data">- The string to be inserted at the beginning of the body. The string is limited to 1,000,000 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`. Any errors encountered will be provided in the `asyncResult.error` property.</param>
        abstract prependAsync: data: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Replaces the entire body with the specified text.
        /// 
        /// When working with HTML-formatted bodies, it is important to note that the `Body.getAsync` and `Body.setAsync` methods are not idempotent.
        /// The value returned from the `getAsync` method will not necessarily be exactly the same as the value that was passed in the `setAsync` method
        /// previously. The client may modify the value passed to `setAsync` in order to make it render efficiently with its rendering engine.
        /// 
        /// When including links in HTML markup, you can disable online link preview by setting the `id` attribute on the anchor (\<a\>) to "LPNoLP" 
        /// (see the **Examples** section for a sample).
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="data">- The string that will replace the existing body. The string is limited to 1,000,000 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `coercionType`: The desired format for the body. The string in the `data` parameter will be converted to this format.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type Office.AsyncResult. Any errors encountered will be provided in the `asyncResult.error` property.</param>
        abstract setAsync: data: string * options: obj * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Replaces the entire body with the specified text.
        /// 
        /// When working with HTML-formatted bodies, it is important to note that the `Body.getAsync` and `Body.setAsync` methods are not idempotent.
        /// The value returned from the `getAsync` method will not necessarily be exactly the same as the value that was passed in the `setAsync` method
        /// previously. The client may modify the value passed to `setAsync` in order to make it render efficiently with its rendering engine.
        /// 
        /// When including links in HTML markup, you can disable online link preview by setting the `id` attribute on the anchor (\<a\>) to "LPNoLP" 
        /// (see the **Examples** section for a sample).
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="data">- The string that will replace the existing body. The string is limited to 1,000,000 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type Office.AsyncResult. Any errors encountered will be provided in the `asyncResult.error` property.</param>
        abstract setAsync: data: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Replaces the selection in the body with the specified text.
        /// 
        /// The `setSelectedDataAsync` method inserts the specified string at the cursor location in the body of the item, or, if text is selected in
        /// the editor, it replaces the selected text. If the cursor was never in the body of the item, or if the body of the item lost focus in the
        /// UI, the string will be inserted at the top of the body content. After insertion, the cursor is placed at the end of the inserted content.
        /// 
        /// When including links in HTML markup, you can disable online link preview by setting the id attribute on the anchor (\<a\>) to "LPNoLP"
        /// (see the **Examples** section for a sample).
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="data">- The string that will replace the existing body. The string is limited to 1,000,000 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `coercionType`: The desired format for the body. The string in the `data` parameter will be converted to this format.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`. Any errors encountered will be provided in the `asyncResult.error` property.</param>
        abstract setSelectedDataAsync: data: string * options: obj * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Replaces the selection in the body with the specified text.
        /// 
        /// The `setSelectedDataAsync` method inserts the specified string at the cursor location in the body of the item, or, if text is selected in
        /// the editor, it replaces the selected text. If the cursor was never in the body of the item, or if the body of the item lost focus in the
        /// UI, the string will be inserted at the top of the body content. After insertion, the cursor is placed at the end of the inserted content.
        /// 
        /// When including links in HTML markup, you can disable online link preview by setting the id attribute on the anchor (\<a\>) to "LPNoLP"
        /// (see the **Examples** section for a sample).
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="data">- The string that will replace the existing body. The string is limited to 1,000,000 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`. Any errors encountered will be provided in the `asyncResult.error` property.</param>
        abstract setSelectedDataAsync: data: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds or replaces the signature of the item body.
        /// 
        /// **Important**: In Outlook on the web, `setSignatureAsync` only works on messages.
        /// 
        /// **Important**: If your add-in implements the 
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/autolaunch | event-based activation feature using `LaunchEvent` in the manifest},
        /// and calls `setSignatureAsync` in the event handler, the following behavior applies.
        /// 
        /// - When the user composes a new item (including reply or forward), the signature is set but doesn't modify the form. This means
        /// if the user closes the form without making other edits, they won't be prompted to save changes.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="data">- The string that represents the signature to be set in the body of the mail. This string is limited to 30,000 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `coercionType`: The format the signature should be set to. If Text, the method sets the signature to plain text,
        ///         removing any HTML tags present. If Html, the method sets the signature to HTML.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`.</param>
        abstract setSignatureAsync: data: string * options: obj * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds or replaces the signature of the item body.
        /// 
        /// **Important**: In Outlook on the web, `setSignatureAsync` only works on messages.
        /// 
        /// **Important**: If your add-in implements the 
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/autolaunch | event-based activation feature using `LaunchEvent` in the manifest},
        /// and calls `setSignatureAsync` in the event handler, the following behavior applies.
        /// 
        /// - When the user composes a new item (including reply or forward), the signature is set but doesn't modify the form. This means
        /// if the user closes the form without making other edits, they won't be prompted to save changes.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="data">- The string that represents the signature to be set in the body of the mail. This string is limited to 30,000 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type `Office.AsyncResult`.</param>
        abstract setSignatureAsync: data: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Represents the categories on an item.
    /// 
    /// In Outlook, a user can tag messages and appointments by using a category to color-code them.
    /// The user defines {@link Office.MasterCategories | categories in a master list} on their mailbox.
    /// They can then apply one or more categories to an item.
    /// 
    /// **Important**: In Outlook on the web, you can't use the API to manage categories applied to a message in Compose mode.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] Categories =
        /// <summary>Adds categories to an item. Each category must be in the categories master list on that mailbox and so must have a unique name
        /// but multiple categories can use the same color.
        /// 
        /// **Important**: In Outlook on the web, you can't use the API to manage categories applied to a message or appointment item in Compose mode.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="categories">- The categories to be added to the item.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract addAsync: categories: ResizeArray<string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds categories to an item. Each category must be in the categories master list on that mailbox and so must have a unique name
        /// but multiple categories can use the same color.
        /// 
        /// **Important**: In Outlook on the web, you can't use the API to manage categories applied to a message or appointment item in Compose mode.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="categories">- The categories to be added to the item.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract addAsync: categories: ResizeArray<string> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Gets an item's categories.
        /// 
        /// **Important**:
        /// 
        /// - If there are no categories on the item, `null` or an empty array will be returned depending on the Outlook version
        /// so make sure to handle both cases.
        /// 
        /// - In Outlook on the web, you can't use the API to manage categories applied to a message in Compose mode.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If getting categories fails, the `asyncResult.error` property will contain an error code.</param>
        abstract getAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<ResizeArray<CategoryDetails>> -> unit) -> unit
        /// <summary>Gets an item's categories.
        /// 
        /// **Important**:
        /// 
        /// - If there are no categories on the item, `null` or an empty array will be returned depending on the Outlook version
        /// so make sure to handle both cases.
        /// 
        /// - In Outlook on the web, you can't use the API to manage categories applied to a message in Compose mode.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If getting categories fails, the `asyncResult.error` property will contain an error code.</param>
        abstract getAsync: callback: (Office.AsyncResult<ResizeArray<CategoryDetails>> -> unit) -> unit
        /// <summary>Removes categories from an item.
        /// 
        /// **Important**: In Outlook on the web, you can't use the API to manage categories applied to a message in Compose mode.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="categories">- The categories to be removed from the item.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If removing categories fails, the `asyncResult.error` property will contain an error code.</param>
        abstract removeAsync: categories: ResizeArray<string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes categories from an item.
        /// 
        /// **Important**: In Outlook on the web, you can't use the API to manage categories applied to a message in Compose mode.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="categories">- The categories to be removed from the item.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If removing categories fails, the `asyncResult.error` property will contain an error code.</param>
        abstract removeAsync: categories: ResizeArray<string> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Represents a category's details like name and associated color.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] CategoryDetails =
        /// The name of the category. Maximum length is 255 characters.
        abstract displayName: string with get, set
        /// The color of the category.
        abstract color: U2<MailboxEnums.CategoryColor, string> with get, set

    /// Represents the details about a contact (similar to what's on a physical contact or business card) extracted from the item's body. Read mode only.
    /// 
    /// The list of contacts extracted from the body of an email message or appointment is returned in the `contacts` property of the
    /// {@link Office.Entities | Entities} object returned by the `getEntities` or `getEntitiesByType` method of the current item.
    type [<AllowNullLiteral>] Contact =
        /// An array of strings containing the mailing and street addresses associated with the contact. Nullable.
        abstract addresses: ResizeArray<string> with get, set
        /// A string containing the name of the business associated with the contact. Nullable.
        abstract businessName: string with get, set
        /// An array of strings containing the SMTP email addresses associated with the contact. Nullable.
        abstract emailAddresses: ResizeArray<string> with get, set
        /// A string containing the name of the person associated with the contact. Nullable.
        abstract personName: string with get, set
        /// An array containing a `PhoneNumber` object for each phone number associated with the contact. Nullable.
        abstract phoneNumbers: ResizeArray<PhoneNumber> with get, set
        /// An array of strings containing the Internet URLs associated with the contact. Nullable.
        abstract urls: ResizeArray<string> with get, set

    /// The `CustomProperties` object represents custom properties that are specific to a particular item and specific to a mail add-in for Outlook.
    /// For example, there might be a need for a mail add-in to save some data that is specific to the current email message that activated the add-in. 
    /// If the user revisits the same message in the future and activates the mail add-in again, the add-in will be able to retrieve the data that had 
    /// been saved as custom properties. **Important**: The maximum length of a `CustomProperties` JSON object is 2500 characters.
    /// 
    /// Because Outlook on Mac doesn't cache custom properties, if the user's network goes down, mail add-ins cannot access their custom properties.
    type [<AllowNullLiteral>] CustomProperties =
        /// <summary>Returns the value of the specified custom property.</summary>
        /// <param name="name">- The name of the custom property to be returned.</param>
        abstract get: name: string -> obj option
        /// Returns an object with all custom properties in a collection of name/value pairs. The following are equivalent.
        /// 
        /// `customProps.get("name")`
        /// 
        /// `var dictionary = customProps.getAll(); dictionary["name"]`
        /// 
        /// You can iterate through the dictionary object to discover all `names` and `values`.
        /// 
        /// [Api set: Mailbox 1.9]
        abstract getAll: unit -> obj option
        /// <summary>Removes the specified property from the custom property collection.
        /// 
        /// To make the removal of the property permanent, you must call the `saveAsync` method of the `CustomProperties` object.</summary>
        /// <param name="name">- The `name` of the property to be removed.</param>
        abstract remove: name: string -> unit
        /// <summary>Saves item-specific custom properties to the server.
        /// 
        /// You must call the `saveAsync` method to persist any changes made with the `set` method or the `remove` method of the `CustomProperties` object.
        /// The saving action is asynchronous.
        /// 
        /// It's a good practice to have your callback function check for and handle errors from `saveAsync`.
        /// In particular, a read add-in can be activated while the user is in a connected state in a read form, and subsequently the user becomes
        /// disconnected. 
        /// If the add-in calls `saveAsync` while in the disconnected state, `saveAsync` would return an error.
        /// Your callback method should handle this error accordingly.</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        /// <param name="asyncContext">- Optional. Any state data that is passed to the callback method.</param>
        abstract saveAsync: callback: (Office.AsyncResult<unit> -> unit) * ?asyncContext: obj -> unit
        /// <summary>Saves item-specific custom properties to the server.
        /// 
        /// You must call the `saveAsync` method to persist any changes made with the `set` method or the `remove` method of the `CustomProperties` object.
        /// The saving action is asynchronous.
        /// 
        /// It's a good practice to have your callback function check for and handle errors from `saveAsync`.
        /// In particular, a read add-in can be activated while the user is in a connected state in a read form, and subsequently the user becomes
        /// disconnected. 
        /// If the add-in calls `saveAsync` while in the disconnected state, `saveAsync` would return an error.
        /// Your callback method should handle this error accordingly.</summary>
        /// <param name="asyncContext">- Optional. Any state data that is passed to the callback method.</param>
        abstract saveAsync: ?asyncContext: obj -> unit
        /// <summary>Sets the specified property to the specified value.
        /// 
        /// The `set` method sets the specified property to the specified value. You must use the `saveAsync` method to save the property to the server.
        /// 
        /// The `set` method creates a new property if the specified property does not already exist; 
        /// otherwise, the existing value is replaced with the new value. 
        /// The `value` parameter can be of any type; however, it is always passed to the server as a string.</summary>
        /// <param name="name">- The name of the property to be set.</param>
        /// <param name="value">- The value of the property to be set.</param>
        abstract set: name: string * value: string -> unit

    /// Provides diagnostic information to an Outlook add-in.
    type [<AllowNullLiteral>] Diagnostics =
        /// Gets a string that represents the name of the host application.
        /// 
        /// A string that can be one of the following values: `Outlook`, `OutlookWebApp`, `OutlookIOS`, or `OutlookAndroid`.
        /// 
        /// **Note**: The `Outlook` value is returned for Outlook on desktop clients (i.e., Windows and Mac).
        abstract hostName: string with get, set
        /// Gets a string that represents the version of either the host application or the Exchange Server (e.g., "15.0.468.0").
        /// 
        /// If the mail add-in is running in Outlook on a desktop or mobile client, the `hostVersion` property returns the version of the host 
        /// application, Outlook. In Outlook on the web, the property returns the version of the Exchange Server.
        abstract hostVersion: string with get, set
        /// Gets a string that represents the current view of Outlook on the web.
        /// 
        /// The returned string can be one of the following values: `OneColumn`, `TwoColumns`, or `ThreeColumns`.
        /// 
        /// If the host application is not Outlook on the web, then accessing this property results in undefined.
        /// 
        /// Outlook on the web has three views that correspond to the width of the screen and the window, and the number of columns that can be displayed:
        /// 
        /// - `OneColumn`, which is displayed when the screen is narrow. Outlook on the web uses this single-column layout on the entire screen of a 
        /// smartphone.
        /// 
        /// - `TwoColumns`, which is displayed when the screen is wider. Outlook on the web uses this view on most tablets.
        /// 
        /// - `ThreeColumns`, which is displayed when the screen is wide. For example, Outlook on the web uses this view in a full screen window on a 
        /// desktop computer.
        abstract OWAView: U2<MailboxEnums.OWAView, string> with get, set

    /// Provides the email properties of the sender or specified recipients of an email message or appointment.
    type [<AllowNullLiteral>] EmailAddressDetails =
        /// Gets the SMTP email address.
        abstract emailAddress: string with get, set
        /// Gets the display name associated with an email address.
        abstract displayName: string with get, set
        /// Gets the response that an attendee returned for an appointment. 
        /// This property applies to only an attendee of an appointment, as represented by the `optionalAttendees` or `requiredAttendees` property. 
        /// This property returns undefined in other scenarios.
        abstract appointmentResponse: U2<MailboxEnums.ResponseType, string> with get, set
        /// Gets the email address type of a recipient.
        abstract recipientType: U2<MailboxEnums.RecipientType, string> with get, set

    /// Represents an email account on an Exchange Server.
    type [<AllowNullLiteral>] EmailUser =
        /// Gets the display name associated with an email address.
        abstract displayName: string with get, set
        /// Gets the SMTP email address.
        abstract emailAddress: string with get, set

    /// Represents the set of locations on an appointment.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] EnhancedLocation =
        /// <summary>Adds to the set of locations associated with the appointment.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="locationIdentifiers">The locations to be added to the current list of locations.</param>
        /// <param name="options">An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object. Check the `status` property of `asyncResult` to determine if the call succeeded.</param>
        abstract addAsync: locationIdentifiers: ResizeArray<LocationIdentifier> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds to the set of locations associated with the appointment.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="locationIdentifiers">The locations to be added to the current list of locations.</param>
        /// <param name="callback">Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object. Check the `status` property of `asyncResult` to determine if the call succeeded.</param>
        abstract addAsync: locationIdentifiers: ResizeArray<LocationIdentifier> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Gets the set of locations associated with the appointment.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract getAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<ResizeArray<LocationDetails>> -> unit) -> unit
        /// <summary>Gets the set of locations associated with the appointment.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract getAsync: ?callback: (Office.AsyncResult<ResizeArray<LocationDetails>> -> unit) -> unit
        /// <summary>Removes the set of locations associated with the appointment.
        /// 
        /// If there are multiple locations with the same name, all matching locations will be removed even if only one was specified in `locationIdentifiers`.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="locationIdentifiers">The locations to be removed from the current list of locations.</param>
        /// <param name="options">An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object. Check the `status` property of `asyncResult` to determine if the call succeeded.</param>
        abstract removeAsync: locationIdentifiers: ResizeArray<LocationIdentifier> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes the set of locations associated with the appointment.
        /// 
        /// If there are multiple locations with the same name, all matching locations will be removed even if only one was specified in `locationIdentifiers`.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="locationIdentifiers">The locations to be removed from the current list of locations.</param>
        /// <param name="callback">Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object. Check the `status` property of `asyncResult` to determine if the call succeeded.</param>
        abstract removeAsync: locationIdentifiers: ResizeArray<LocationIdentifier> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Provides the current enhanced locations when the `Office.EventType.EnhancedLocationsChanged` event is raised.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] EnhancedLocationsChangedEventArgs =
        /// Gets the set of enhanced locations.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract enhancedLocations: ResizeArray<LocationDetails> with get, set
        /// Gets the type of the event. For details, refer to {@link https://docs.microsoft.com/javascript/api/office/office.eventtype | Office.EventType}.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract ``type``: string with get, set

    /// Represents a collection of entities found in an email message or appointment. Read mode only.
    /// 
    /// The `Entities` object is a container for the entity arrays returned by the `getEntities` and `getEntitiesByType` methods when the item 
    /// (either an email message or an appointment) contains one or more entities that have been found by the server. 
    /// You can use these entities in your code to provide additional context information to the viewer, such as a map to an address found in the item, 
    /// or to open a dialer for a phone number found in the item.
    /// 
    /// If no entities of the type specified in the property are present in the item, the property associated with that entity is null. 
    /// For example, if a message contains a street address and a phone number, the addresses property and phoneNumbers property would contain 
    /// information, and the other properties would be null.
    /// 
    /// To be recognized as an address, the string must contain a United States postal address that has at least a subset of the elements of a street 
    /// number, street name, city, state, and zip code.
    /// 
    /// To be recognized as a phone number, the string must contain a North American phone number format.
    /// 
    /// Entity recognition relies on natural language recognition that is based on machine learning of large amounts of data. 
    /// The recognition of an entity is non-deterministic and success sometimes relies on the particular context in the item.
    /// 
    /// When the property arrays are returned by the `getEntitiesByType` method, only the property for the specified entity contains data; 
    /// all other properties are null.
    type [<AllowNullLiteral>] Entities =
        /// Gets the physical addresses (street or mailing addresses) found in an email message or appointment.
        abstract addresses: ResizeArray<string> with get, set
        /// Gets the contacts found in an email address or appointment.
        abstract contacts: ResizeArray<Contact> with get, set
        /// Gets the email addresses found in an email message or appointment.
        abstract emailAddresses: ResizeArray<string> with get, set
        /// Gets the meeting suggestions found in an email message.
        abstract meetingSuggestions: ResizeArray<MeetingSuggestion> with get, set
        /// Gets the phone numbers found in an email message or appointment.
        abstract phoneNumbers: ResizeArray<PhoneNumber> with get, set
        /// Gets the task suggestions found in an email message or appointment.
        abstract taskSuggestions: ResizeArray<string> with get, set
        /// Gets the Internet URLs present in an email message or appointment.
        abstract urls: ResizeArray<string> with get, set

    /// Provides a method to get the from value of a message in an Outlook add-in.
    /// 
    /// [Api set: Mailbox 1.7]
    type [<AllowNullLiteral>] From =
        /// <summary>Gets the from value of a message.
        /// 
        /// The `getAsync` method starts an asynchronous call to the Exchange server to get the from value of a message.
        /// 
        /// The from value of the item is provided as an {@link Office.EmailAddressDetails | EmailAddressDetails} in the `asyncResult.value` property.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        ///             `asyncResult`, which is an `Office.AsyncResult` object.
        ///  The `value` property of the result is the item's from value, as an `EmailAddressDetails` object.</param>
        abstract getAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<EmailAddressDetails> -> unit) -> unit
        /// <summary>Gets the from value of a message.
        /// 
        /// The `getAsync` method starts an asynchronous call to the Exchange server to get the from value of a message.
        /// 
        /// The from value of the item is provided as an {@link Office.EmailAddressDetails | EmailAddressDetails} in the `asyncResult.value` property.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        ///             `asyncResult`, which is an `Office.AsyncResult` object.
        ///  The `value` property of the result is the item's from value, as an `EmailAddressDetails` object.</param>
        abstract getAsync: ?callback: (Office.AsyncResult<EmailAddressDetails> -> unit) -> unit

    /// The `InternetHeaders` object represents custom internet headers that are preserved after the message item leaves Exchange
    /// and is converted to a MIME message. These headers are stored as x-headers in the MIME message.
    /// 
    /// Internet headers are stored as key/value pairs on a per-item basis.
    /// 
    /// **Note**: This object is intended for you to set and get your custom headers on a message item. To learn more, see
    /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/internet-headers | Get and set internet headers on a message in an Outlook add-in}.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] InternetHeaders =
        /// <summary>Given an array of internet header names, this method returns a dictionary containing those internet headers and their values. 
        /// If the add-in requests an x-header that is not available, that x-header will not be returned in the results.
        /// 
        /// **Note**: This method is intended to return the values of the custom headers you set using the `setAsync` method.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="names">- The names of the internet headers to be returned.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties:
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract getAsync: names: ResizeArray<string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<InternetHeaders> -> unit) -> unit
        /// <summary>Given an array of internet header names, this method returns a dictionary containing those internet headers and their values. 
        /// If the add-in requests an x-header that is not available, that x-header will not be returned in the results.
        /// 
        /// **Note**: This method is intended to return the values of the custom headers you set using the `setAsync` method.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="names">- The names of the internet headers to be returned.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract getAsync: names: ResizeArray<string> * ?callback: (Office.AsyncResult<InternetHeaders> -> unit) -> unit
        /// <summary>Given an array of internet header names, this method removes the specified headers from the internet header collection.
        /// 
        /// **Note**: This method is intended to remove the custom headers you set using the `setAsync` method.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="names">- The names of the internet headers to be removed.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties:
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract removeAsync: names: ResizeArray<string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<InternetHeaders> -> unit) -> unit
        /// <summary>Given an array of internet header names, this method removes the specified headers from the internet header collection.
        /// 
        /// **Note**: This method is intended to remove the custom headers you set using the `setAsync` method.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="names">- The names of the internet headers to be removed.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract removeAsync: names: ResizeArray<string> * ?callback: (Office.AsyncResult<InternetHeaders> -> unit) -> unit
        /// <summary>Sets the specified internet headers to the specified values.
        /// 
        /// The `setAsync` method creates a new header if the specified header doesn't already exist; otherwise, the existing value is replaced with 
        /// the new value.
        /// 
        /// **Note**: This method is intended to set the values of your custom headers.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="headers">- The names and corresponding values of the headers to be set. Should be a dictionary object with keys being the names of the 
        /// internet headers and values being the values of the internet headers.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type Office.AsyncResult. Any errors encountered will be provided in the `asyncResult.error` property.</param>
        abstract setAsync: headers: Object * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets the specified internet headers to the specified values.
        /// 
        /// The `setAsync` method creates a new header if the specified header doesn't already exist; otherwise, the existing value is replaced with 
        /// the new value.
        /// 
        /// **Note**: This method is intended to set the values of your custom headers.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="headers">- The names and corresponding values of the headers to be set. Should be a dictionary object with keys being the names of the 
        /// internet headers and values being the values of the internet headers.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///             of type Office.AsyncResult. Any errors encountered will be provided in the `asyncResult.error` property.</param>
        abstract setAsync: headers: Object * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// The item namespace is used to access the currently selected message, meeting request, or appointment. 
    /// You can determine the type of the item by using the `itemType` property.
    /// 
    /// To see the full member list, refer to the
    /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item | Object Model} page.
    /// 
    /// If you want to see IntelliSense for only a specific type or mode, cast this item to one of the following:
    /// 
    /// - {@link Office.AppointmentCompose | AppointmentCompose}
    /// 
    /// - {@link Office.AppointmentRead | AppointmentRead}
    /// 
    /// - {@link Office.MessageCompose | MessageCompose}
    /// 
    /// - {@link Office.MessageRead | MessageRead}
    type [<AllowNullLiteral>] Item =
        interface end

    /// The compose mode of {@link Office.Item | Office.context.mailbox.item}.
    /// 
    /// **Important**: This is an internal Outlook object, not directly exposed through existing interfaces.
    /// You should treat this as a mode of `Office.context.mailbox.item`. For more information, refer to the
    /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item | Object Model} page.
    /// 
    /// Child interfaces:
    /// 
    /// - {@link Office.AppointmentCompose | AppointmentCompose}
    /// 
    /// - {@link Office.MessageCompose | MessageCompose}
    type [<AllowNullLiteral>] ItemCompose =
        inherit Item

    /// The read mode of {@link Office.Item | Office.context.mailbox.item}.
    /// 
    /// **Important**: This is an internal Outlook object, not directly exposed through existing interfaces.
    /// You should treat this as a mode of `Office.context.mailbox.item`. For more information, refer to the
    /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item | Object Model} page.
    /// 
    /// Child interfaces:
    /// 
    /// - {@link Office.AppointmentRead | AppointmentRead}
    /// 
    /// - {@link Office.MessageRead | MessageRead}
    type [<AllowNullLiteral>] ItemRead =
        inherit Item

    /// Represents a date and time in the local client's time zone. Read mode only.
    type [<AllowNullLiteral>] LocalClientTime =
        /// Integer value representing the month, beginning with 0 for January to 11 for December.
        abstract month: float with get, set
        /// Integer value representing the day of the month.
        abstract date: float with get, set
        /// Integer value representing the year.
        abstract year: float with get, set
        /// Integer value representing the hour on a 24-hour clock.
        abstract hours: float with get, set
        /// Integer value representing the minutes.
        abstract minutes: float with get, set
        /// Integer value representing the seconds.
        abstract seconds: float with get, set
        /// Integer value representing the milliseconds.
        abstract milliseconds: float with get, set
        /// Integer value representing the number of minutes difference between the local time zone and UTC.
        abstract timezoneOffset: float with get, set

    /// Provides methods to get and set the location of a meeting in an Outlook add-in.
    /// 
    /// [Api set: Mailbox 1.1]
    type [<AllowNullLiteral>] Location =
        /// <summary>Gets the location of an appointment.
        /// 
        /// The `getAsync` method starts an asynchronous call to the Exchange server to get the location of an appointment.
        /// The location of the appointment is provided as a string in the `asyncResult.value` property.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract getAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Gets the location of an appointment.
        /// 
        /// The `getAsync` method starts an asynchronous call to the Exchange server to get the location of an appointment.
        /// The location of the appointment is provided as a string in the `asyncResult.value` property.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract getAsync: callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Sets the location of an appointment.
        /// 
        /// The `setAsync` method starts an asynchronous call to the Exchange server to set the location of an appointment. 
        /// Setting the location of an appointment overwrites the current location.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="location">- The location of the appointment. The string is limited to 255 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. If setting the location fails, the `asyncResult.error` property will contain an error code.</param>
        abstract setAsync: location: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets the location of an appointment.
        /// 
        /// The `setAsync` method starts an asynchronous call to the Exchange server to set the location of an appointment. 
        /// Setting the location of an appointment overwrites the current location.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="location">- The location of the appointment. The string is limited to 255 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. If setting the location fails, the `asyncResult.error` property will contain an error code.</param>
        abstract setAsync: location: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Represents a location. Read only.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] LocationDetails =
        /// The `LocationIdentifier` of the location.
        abstract locationIdentifier: LocationIdentifier with get, set
        /// The location's display name.
        abstract displayName: string with get, set
        /// The email address associated with the location.
        abstract emailAddress: string with get, set

    /// Represents the ID of a location.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] LocationIdentifier =
        /// The location's unique ID.
        /// 
        /// For `Room` type, it's the room's email address.
        /// 
        /// For `Custom` type, it's the `displayName`.
        abstract id: string with get, set
        /// The location's type.
        abstract ``type``: U2<MailboxEnums.LocationType, string> with get, set

    /// Provides access to the Microsoft Outlook add-in object model.
    /// 
    /// Key properties:
    /// 
    /// - `diagnostics`: Provides diagnostic information to an Outlook add-in.
    /// 
    /// - `item`: Provides methods and properties for accessing a message or appointment in an Outlook add-in.
    /// 
    /// - `userProfile`: Provides information about the user in an Outlook add-in.
    type [<AllowNullLiteral>] Mailbox =
        /// Provides diagnostic information to an Outlook add-in.
        /// 
        /// Contains the following members:
        /// 
        ///   - `hostName` (string): A string that represents the name of the host application. 
        /// It should be one of the following values: `Outlook`, `OutlookWebApp`, `OutlookIOS`, or `OutlookAndroid`.
        /// **Note**: The "Outlook" value is returned for Outlook on desktop clients (i.e., Windows and Mac).
        /// 
        ///   - `hostVersion` (string): A string that represents the version of either the host application or the Exchange Server (e.g., "15.0.468.0"). 
        /// If the mail add-in is running in Outlook on desktop or mobile clients, the `hostVersion` property returns the version of the 
        /// host application, Outlook. In Outlook on the web, the property returns the version of the Exchange Server.
        /// 
        ///   - `OWAView` (`MailboxEnums.OWAView` or string): An enum (or string literal) that represents the current view of Outlook on the web. 
        /// If the host application is not Outlook on the web, then accessing this property results in undefined. 
        /// Outlook on the web has three views (`OneColumn` - displayed when the screen is narrow, `TwoColumns` - displayed when the screen is wider, 
        /// and `ThreeColumns` - displayed when the screen is wide) that correspond to the width of the screen and the window, and the number of columns 
        /// that can be displayed.
        /// 
        ///   More information is under {@link Office.Diagnostics}.
        abstract diagnostics: Diagnostics with get, set
        /// Gets the URL of the Exchange Web Services (EWS) endpoint for this email account. Read mode only.
        /// 
        /// Your app must have the `ReadItem` permission specified in its manifest to call the `ewsUrl` member in read mode.
        /// 
        /// In compose mode you must call the `saveAsync` method before you can use the `ewsUrl` member. 
        /// Your app must have `ReadWriteItem` permissions to call the `saveAsync` method.
        /// 
        /// **Note**: This member is not supported in Outlook on iOS or Android.
        abstract ewsUrl: string with get, set
        /// The mailbox item. Depending on the context in which the add-in opened, the item type may vary.
        /// If you want to see IntelliSense for only a specific type or mode, cast this item to one of the following:
        /// 
        /// {@link Office.MessageCompose | MessageCompose}, {@link Office.MessageRead | MessageRead},
        /// {@link Office.AppointmentCompose | AppointmentCompose}, {@link Office.AppointmentRead | AppointmentRead}
        /// 
        /// **Important**: `item` can be null if your add-in supports pinning the task pane. For details on how to handle, see
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/pinnable-taskpane#implement-the-event-handler | Implement a pinnable task pane in Outlook}.
        abstract item: obj option with get, set
        /// Gets an object that provides methods to manage the categories master list associated with a mailbox.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract masterCategories: MasterCategories with get, set
        /// Gets the URL of the REST endpoint for this email account.
        /// 
        /// Your app must have the `ReadItem` permission specified in its manifest to call the `restUrl` member in read mode.
        /// 
        /// In compose mode you must call the `saveAsync` method before you can use the `restUrl` member. 
        /// Your app must have `ReadWriteItem` permissions to call the `saveAsync` method.
        /// 
        /// However, in delegate or shared scenarios, you should instead use the `targetRestUrl` property of the
        /// {@link https://docs.microsoft.com/javascript/api/outlook/office.sharedproperties?view=outlook-js-1.8 | SharedProperties} object
        /// (introduced in requirement set 1.8). For more information, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// [Api set: Mailbox 1.5]
        abstract restUrl: string with get, set
        /// Information about the user associated with the mailbox. This includes their account type, display name, email address, and time zone.
        /// 
        /// More information is under {@link Office.UserProfile}
        abstract userProfile: UserProfile with get, set
        /// <summary>Adds an event handler for a supported event. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Mailbox object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox#events | events section}.
        /// 
        /// [Api set: Mailbox 1.5]</summary>
        /// <param name="eventType">- The event that should invoke the handler.</param>
        /// <param name="handler">- The function to handle the event. The function must accept a single parameter, which is an object literal.
        /// The `type` property on the parameter will match the `eventType` parameter passed to `addHandlerAsync`.</param>
        /// <param name="options">- Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract addHandlerAsync: eventType: U2<Office.EventType, string> * handler: obj option * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an event handler for a supported event. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Mailbox object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox#events | events section}.
        /// 
        /// [Api set: Mailbox 1.5]</summary>
        /// <param name="eventType">- The event that should invoke the handler.</param>
        /// <param name="handler">- The function to handle the event. The function must accept a single parameter, which is an object literal.
        /// The `type` property on the parameter will match the `eventType` parameter passed to `addHandlerAsync`.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract addHandlerAsync: eventType: U2<Office.EventType, string> * handler: obj option * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Converts an item ID formatted for REST into EWS format.
        /// 
        /// Item IDs retrieved via a REST API (such as the Outlook Mail API or the Microsoft Graph) use a different format than the format used by
        /// Exchange Web Services (EWS). The `convertToEwsId` method converts a REST-formatted ID into the proper format for EWS.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="itemId">- An item ID formatted for the Outlook REST APIs.</param>
        /// <param name="restVersion">- A value indicating the version of the Outlook REST API used to retrieve the item ID.</param>
        abstract convertToEwsId: itemId: string * restVersion: U2<MailboxEnums.RestVersion, string> -> string
        /// <summary>Gets a dictionary containing time information in local client time.
        /// 
        /// The dates and times used by a mail app for Outlook on the web or desktop clients can use different time zones. 
        /// Outlook uses the client computer time zone; Outlook on the web uses the time zone set on the Exchange Admin Center (EAC). 
        /// You should handle date and time values so that the values you display on the user interface are always consistent with the time zone that 
        /// the user expects.
        /// 
        /// If the mail app is running in Outlook on desktop clients, the `convertToLocalClientTime` method will return a dictionary object
        /// with the values set to the client computer time zone. 
        /// If the mail app is running in Outlook on the web, the `convertToLocalClientTime` method will return a dictionary object
        /// with the values set to the time zone specified in the EAC.</summary>
        /// <param name="timeValue">- A `Date` object.</param>
        abstract convertToLocalClientTime: timeValue: DateTime -> LocalClientTime
        /// <summary>Converts an item ID formatted for EWS into REST format.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="itemId">- An item ID formatted for Exchange Web Services (EWS)</param>
        /// <param name="restVersion">- A value indicating the version of the Outlook REST API that the converted ID will be used with.</param>
        abstract convertToRestId: itemId: string * restVersion: U2<MailboxEnums.RestVersion, string> -> string
        /// <summary>Gets a `Date` object from a dictionary containing time information.
        /// 
        /// The `convertToUtcClientTime` method converts a dictionary containing a local date and time to a `Date` object with the correct values for the 
        /// local date and time.</summary>
        /// <param name="input">- The local time value to convert.</param>
        abstract convertToUtcClientTime: input: LocalClientTime -> DateTime
        /// <summary>Displays an existing calendar appointment.
        /// 
        /// The `displayAppointmentForm` method opens an existing calendar appointment in a new window on the desktop or in a dialog box on 
        /// mobile devices.
        /// 
        /// In Outlook on Mac, you can use this method to display a single appointment that is not part of a recurring series, or the master appointment
        /// of a recurring series. However, you can't display an instance of the series because you can't access the properties
        /// (including the item ID) of instances of a recurring series.
        /// 
        /// In Outlook on the web, this method opens the specified form only if the body of the form is less than or equal to 32K characters.
        /// 
        /// If the specified item identifier does not identify an existing appointment, a blank pane opens on the client computer or device, and 
        /// no error message is returned.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="itemId">- The Exchange Web Services (EWS) identifier for an existing calendar appointment.</param>
        abstract displayAppointmentForm: itemId: string -> unit
        /// <summary>Displays an existing calendar appointment.
        /// 
        /// The `displayAppointmentFormAsync` method opens an existing calendar appointment in a new window on the desktop or in a dialog box on
        /// mobile devices.
        /// 
        /// In Outlook on Mac, you can use this method to display a single appointment that is not part of a recurring series, or the master appointment
        /// of a recurring series. However, you can't display an instance of the series because you can't access the properties
        /// (including the item ID) of instances of a recurring series.
        /// 
        /// In Outlook on the web, this method opens the specified form only if the body of the form is less than or equal to 32K characters.
        /// 
        /// If the specified item identifier does not identify an existing appointment, a blank pane opens on the client computer or device, and
        /// no error message is returned.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="itemId">- The Exchange Web Services (EWS) identifier for an existing calendar appointment.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayAppointmentFormAsync: itemId: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays an existing calendar appointment.
        /// 
        /// The `displayAppointmentFormAsync` method opens an existing calendar appointment in a new window on the desktop or in a dialog box on
        /// mobile devices.
        /// 
        /// In Outlook on Mac, you can use this method to display a single appointment that is not part of a recurring series, or the master appointment
        /// of a recurring series. However, you can't display an instance of the series because you can't access the properties
        /// (including the item ID) of instances of a recurring series.
        /// 
        /// In Outlook on the web, this method opens the specified form only if the body of the form is less than or equal to 32K characters.
        /// 
        /// If the specified item identifier does not identify an existing appointment, a blank pane opens on the client computer or device, and
        /// no error message is returned.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="itemId">- The Exchange Web Services (EWS) identifier for an existing calendar appointment.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayAppointmentFormAsync: itemId: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays an existing message.
        /// 
        /// The `displayMessageForm` method opens an existing message in a new window on the desktop or in a dialog box on mobile devices.
        /// 
        /// In Outlook on the web, this method opens the specified form only if the body of the form is less than or equal to 32K characters.
        /// 
        /// If the specified item identifier does not identify an existing message, no message will be displayed on the client computer, and
        /// no error message is returned.
        /// 
        /// Do not use the `displayMessageForm` with an itemId that represents an appointment. Use the `displayAppointmentForm` method to display
        /// an existing appointment, and `displayNewAppointmentForm` to display a form to create a new appointment.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="itemId">- The Exchange Web Services (EWS) identifier for an existing message.</param>
        abstract displayMessageForm: itemId: string -> unit
        /// <summary>Displays an existing message.
        /// 
        /// The `displayMessageFormAsync` method opens an existing message in a new window on the desktop or in a dialog box on mobile devices.
        /// 
        /// In Outlook on the web, this method opens the specified form only if the body of the form is less than or equal to 32K characters.
        /// 
        /// If the specified item identifier does not identify an existing message, no message will be displayed on the client computer, and
        /// no error message is returned.
        /// 
        /// Do not use the `displayMessageForm` or `displayMessageFormAsync` method with an itemId that represents an appointment.
        /// Use the `displayAppointmentForm` or `displayAppointmentFormAsync` method to display an existing appointment,
        /// and `displayNewAppointmentForm` or `displayNewAppointmentFormAsync` to display a form to create a new appointment.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="itemId">- The Exchange Web Services (EWS) identifier for an existing message.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayMessageFormAsync: itemId: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays an existing message.
        /// 
        /// The `displayMessageFormAsync` method opens an existing message in a new window on the desktop or in a dialog box on mobile devices.
        /// 
        /// In Outlook on the web, this method opens the specified form only if the body of the form is less than or equal to 32K characters.
        /// 
        /// If the specified item identifier does not identify an existing message, no message will be displayed on the client computer, and
        /// no error message is returned.
        /// 
        /// Do not use the `displayMessageForm` or `displayMessageFormAsync` method with an itemId that represents an appointment.
        /// Use the `displayAppointmentForm` or `displayAppointmentFormAsync` method to display an existing appointment,
        /// and `displayNewAppointmentForm` or `displayNewAppointmentFormAsync` to display a form to create a new appointment.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="itemId">- The Exchange Web Services (EWS) identifier for an existing message.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayMessageFormAsync: itemId: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a form for creating a new calendar appointment.
        /// 
        /// The `displayNewAppointmentForm` method opens a form that enables the user to create a new appointment or meeting.
        /// If parameters are specified, the appointment form fields are automatically populated with the contents of the parameters.
        /// 
        /// In Outlook on the web, this method always displays a form with an attendees field.
        /// If you do not specify any attendees as input arguments, the method displays a form with a **Save** button.
        /// If you have specified attendees, the form would include the attendees and a **Send** button.
        /// 
        /// In the Outlook rich client and Outlook RT, if you specify any attendees or resources in the `requiredAttendees`, `optionalAttendees`, or
        /// `resources` parameter, this method displays a meeting form with a **Send** button.
        /// If you don't specify any recipients, this method displays an appointment form with a **Save & Close** button.
        /// 
        /// If any of the parameters exceed the specified size limits, or if an unknown parameter name is specified, an exception is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="parameters">- An `AppointmentForm` describing the new appointment. All properties are optional.</param>
        abstract displayNewAppointmentForm: parameters: AppointmentForm -> unit
        /// <summary>Displays a form for creating a new calendar appointment.
        /// 
        /// The `displayNewAppointmentFormAsync` method opens a form that enables the user to create a new appointment or meeting.
        /// If parameters are specified, the appointment form fields are automatically populated with the contents of the parameters.
        /// 
        /// In Outlook on the web, this method always displays a form with an attendees field.
        /// If you do not specify any attendees as input arguments, the method displays a form with a **Save** button.
        /// If you have specified attendees, the form would include the attendees and a **Send** button.
        /// 
        /// In the Outlook rich client and Outlook RT, if you specify any attendees or resources in the `requiredAttendees`, `optionalAttendees`, or
        /// `resources` parameter, this method displays a meeting form with a **Send** button.
        /// If you don't specify any recipients, this method displays an appointment form with a **Save & Close** button.
        /// 
        /// If any of the parameters exceed the specified size limits, or if an unknown parameter name is specified, an exception is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="parameters">- An `AppointmentForm` describing the new appointment. All properties are optional.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayNewAppointmentFormAsync: parameters: AppointmentForm * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a form for creating a new calendar appointment.
        /// 
        /// The `displayNewAppointmentFormAsync` method opens a form that enables the user to create a new appointment or meeting.
        /// If parameters are specified, the appointment form fields are automatically populated with the contents of the parameters.
        /// 
        /// In Outlook on the web, this method always displays a form with an attendees field.
        /// If you do not specify any attendees as input arguments, the method displays a form with a **Save** button.
        /// If you have specified attendees, the form would include the attendees and a **Send** button.
        /// 
        /// In the Outlook rich client and Outlook RT, if you specify any attendees or resources in the `requiredAttendees`, `optionalAttendees`, or
        /// `resources` parameter, this method displays a meeting form with a **Send** button.
        /// If you don't specify any recipients, this method displays an appointment form with a **Save & Close** button.
        /// 
        /// If any of the parameters exceed the specified size limits, or if an unknown parameter name is specified, an exception is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="parameters">- An `AppointmentForm` describing the new appointment. All properties are optional.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayNewAppointmentFormAsync: parameters: AppointmentForm * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a form for creating a new message.
        /// 
        /// The `displayNewMessageForm` method opens a form that enables the user to create a new message. If parameters are specified, the message form 
        /// fields are automatically populated with the contents of the parameters.
        /// 
        /// If any of the parameters exceed the specified size limits, or if an unknown parameter name is specified, an exception is thrown.
        /// 
        /// [Api set: Mailbox 1.6]</summary>
        /// <param name="parameters">- A dictionary containing all values to be filled in for the user in the new form. All parameters are optional.
        /// 
        /// `toRecipients`: An array of strings containing the email addresses or an array containing an {@link Office.EmailAddressDetails | EmailAddressDetails} object 
        /// for each of the recipients on the **To** line. The array is limited to a maximum of 100 entries.
        /// 
        /// `ccRecipients`: An array of strings containing the email addresses or an array containing an {@link Office.EmailAddressDetails | EmailAddressDetails} object 
        /// for each of the recipients on the **Cc** line. The array is limited to a maximum of 100 entries.
        /// 
        /// `bccRecipients`: An array of strings containing the email addresses or an array containing an {@link Office.EmailAddressDetails | EmailAddressDetails} object 
        /// for each of the recipients on the **Bcc** line. The array is limited to a maximum of 100 entries.
        /// 
        /// `subject`: A string containing the subject of the message. The string is limited to a maximum of 255 characters.
        /// 
        /// `htmlBody`: The HTML body of the message. The body content is limited to a maximum size of 32 KB.
        /// 
        /// `attachments`: An array of JSON objects that are either file or item attachments.
        /// 
        /// `attachments.type`: Indicates the type of attachment. Must be file for a file attachment or item for an item attachment.
        /// 
        /// `attachments.name`: A string that contains the name of the attachment, up to 255 characters in length.
        /// 
        /// `attachments.url`: Only used if type is set to file. The URI of the location for the file.
        /// 
        /// `attachments.isInline`: Only used if type is set to file. If true, indicates that the attachment will be shown inline in the 
        /// message body, and should not be displayed in the attachment list.
        /// 
        /// `attachments.itemId`: Only used if type is set to item. The EWS item id of the existing e-mail you want to attach to the new message. 
        /// This is a string up to 100 characters.</param>
        abstract displayNewMessageForm: parameters: obj option -> unit
        /// <summary>Displays a form for creating a new message.
        /// 
        /// The `displayNewMessageFormAsync` method opens a form that enables the user to create a new message.
        /// If parameters are specified, the message form fields are automatically populated with the contents of the parameters.
        /// 
        /// If any of the parameters exceed the specified size limits, or if an unknown parameter name is specified, an exception is thrown.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="parameters">- A dictionary containing all values to be filled in for the user in the new form. All parameters are optional.
        /// 
        /// `toRecipients`: An array of strings containing the email addresses or an array containing an {@link Office.EmailAddressDetails | EmailAddressDetails} object
        /// for each of the recipients on the **To** line. The array is limited to a maximum of 100 entries.
        /// 
        /// `ccRecipients`: An array of strings containing the email addresses or an array containing an {@link Office.EmailAddressDetails | EmailAddressDetails} object
        /// for each of the recipients on the **Cc** line. The array is limited to a maximum of 100 entries.
        /// 
        /// `bccRecipients`: An array of strings containing the email addresses or an array containing an {@link Office.EmailAddressDetails | EmailAddressDetails} object
        /// for each of the recipients on the **Bcc** line. The array is limited to a maximum of 100 entries.
        /// 
        /// `subject`: A string containing the subject of the message. The string is limited to a maximum of 255 characters.
        /// 
        /// `htmlBody`: The HTML body of the message. The body content is limited to a maximum size of 32 KB.
        /// 
        /// `attachments`: An array of JSON objects that are either file or item attachments.
        /// 
        /// `attachments.type`: Indicates the type of attachment. Must be file for a file attachment or item for an item attachment.
        /// 
        /// `attachments.name`: A string that contains the name of the attachment, up to 255 characters in length.
        /// 
        /// `attachments.url`: Only used if type is set to file. The URI of the location for the file.
        /// 
        /// `attachments.isInline`: Only used if type is set to file. If true, indicates that the attachment will be shown inline in the
        /// message body, and should not be displayed in the attachment list.
        /// 
        /// `attachments.itemId`: Only used if type is set to item. The EWS item id of the existing e-mail you want to attach to the new message.
        /// This is a string up to 100 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayNewMessageFormAsync: parameters: obj option * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a form for creating a new message.
        /// 
        /// The `displayNewMessageFormAsync` method opens a form that enables the user to create a new message.
        /// If parameters are specified, the message form fields are automatically populated with the contents of the parameters.
        /// 
        /// If any of the parameters exceed the specified size limits, or if an unknown parameter name is specified, an exception is thrown.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="parameters">- A dictionary containing all values to be filled in for the user in the new form. All parameters are optional.
        /// 
        /// `toRecipients`: An array of strings containing the email addresses or an array containing an {@link Office.EmailAddressDetails | EmailAddressDetails} object
        /// for each of the recipients on the **To** line. The array is limited to a maximum of 100 entries.
        /// 
        /// `ccRecipients`: An array of strings containing the email addresses or an array containing an {@link Office.EmailAddressDetails | EmailAddressDetails} object
        /// for each of the recipients on the **Cc** line. The array is limited to a maximum of 100 entries.
        /// 
        /// `bccRecipients`: An array of strings containing the email addresses or an array containing an {@link Office.EmailAddressDetails | EmailAddressDetails} object
        /// for each of the recipients on the **Bcc** line. The array is limited to a maximum of 100 entries.
        /// 
        /// `subject`: A string containing the subject of the message. The string is limited to a maximum of 255 characters.
        /// 
        /// `htmlBody`: The HTML body of the message. The body content is limited to a maximum size of 32 KB.
        /// 
        /// `attachments`: An array of JSON objects that are either file or item attachments.
        /// 
        /// `attachments.type`: Indicates the type of attachment. Must be file for a file attachment or item for an item attachment.
        /// 
        /// `attachments.name`: A string that contains the name of the attachment, up to 255 characters in length.
        /// 
        /// `attachments.url`: Only used if type is set to file. The URI of the location for the file.
        /// 
        /// `attachments.isInline`: Only used if type is set to file. If true, indicates that the attachment will be shown inline in the
        /// message body, and should not be displayed in the attachment list.
        /// 
        /// `attachments.itemId`: Only used if type is set to item. The EWS item id of the existing e-mail you want to attach to the new message.
        /// This is a string up to 100 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayNewMessageFormAsync: parameters: obj option * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Gets a string that contains a token used to call REST APIs or Exchange Web Services (EWS).
        /// 
        /// The `getCallbackTokenAsync` method makes an asynchronous call to get an opaque token from the Exchange Server that hosts the user's mailbox. 
        /// The lifetime of the callback token is 5 minutes.
        /// 
        /// The token is returned as a string in the `asyncResult.value` property.
        /// 
        /// Calling the `getCallbackTokenAsync` method in read mode requires a minimum permission level of `ReadItem`.
        /// 
        /// Calling the `getCallbackTokenAsync` method in compose mode requires you to have saved the item.
        /// The `saveAsync` method requires a minimum permission level of `ReadWriteItem`.
        /// 
        /// **Important**: For guidance on delegate or shared scenarios, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// *REST Tokens*
        /// 
        /// When a REST token is requested (`options.isRest` = `true`), the resulting token will not work to authenticate EWS calls.
        /// The token will be limited in scope to read-only access to the current item and its attachments, unless the add-in has specified the
        /// `ReadWriteMailbox` permission in its manifest.
        /// If the `ReadWriteMailbox` permission is specified, the resulting token will grant read/write access to mail, calendar, and contacts,
        /// including the ability to send mail.
        /// 
        /// The add-in should use the `restUrl` property to determine the correct URL to use when making REST API calls.
        /// 
        /// This API works for the following scopes:
        /// 
        /// - `Mail.ReadWrite`
        /// 
        /// - `Mail.Send`
        /// 
        /// - `Calendars.ReadWrite`
        /// 
        /// - `Contacts.ReadWrite`
        /// 
        /// *EWS Tokens*
        /// 
        /// When an EWS token is requested (`options.isRest` = `false`), the resulting token will not work to authenticate REST API calls.
        /// The token will be limited in scope to accessing the current item.
        /// 
        /// The add-in should use the `ewsUrl` property to determine the correct URL to use when making EWS calls.
        /// 
        /// You can pass both the token and either an attachment identifier or item identifier to a third-party system. The third-party system uses
        /// the token as a bearer authorization token to call the Exchange Web Services (EWS)
        /// {@link https://docs.microsoft.com/exchange/client-developer/web-service-reference/getattachment-operation | GetAttachment} operation or
        /// {@link https://docs.microsoft.com/exchange/client-developer/web-service-reference/getitem-operation | GetItem} operation to return an
        /// attachment or item. For example, you can create a remote service to
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/get-attachments-of-an-outlook-item | get attachments from the selected item}.
        /// 
        /// **Note**: It is recommended that add-ins use the REST APIs instead of Exchange Web Services whenever possible.
        /// 
        /// [Api set: Mailbox 1.5]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `isRest`: Determines if the token provided will be used for the Outlook REST APIs or Exchange Web Services. Default value is `false`.
        /// `asyncContext`: Any state data that is passed to the asynchronous method.</param>
        /// <param name="callback">- When the method completes, the function passed in the callback parameter is called with a single parameter of
        /// type `Office.AsyncResult`. The token is returned as a string in the `asyncResult.value` property.
        /// If there was an error, the `asyncResult.error` and `asyncResult.diagnostics` properties may provide additional information.</param>
        abstract getCallbackTokenAsync: options: obj * callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Gets a string that contains a token used to get an attachment or item from an Exchange Server.
        /// 
        /// The `getCallbackTokenAsync` method makes an asynchronous call to get an opaque token from the Exchange Server that hosts the user's mailbox. 
        /// The lifetime of the callback token is 5 minutes.
        /// 
        /// The token is returned as a string in the `asyncResult.value` property.
        /// 
        /// You can pass both the token and either an attachment identifier or item identifier to a third-party system. The third-party system uses
        /// the token as a bearer authorization token to call the Exchange Web Services (EWS)
        /// {@link https://docs.microsoft.com/exchange/client-developer/web-service-reference/getattachment-operation | GetAttachment} operation or
        /// {@link https://docs.microsoft.com/exchange/client-developer/web-service-reference/getitem-operation | GetItem} operation to return an
        /// attachment or item. For example, you can create a remote service to
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/get-attachments-of-an-outlook-item | get attachments from the selected item}.
        /// 
        /// Calling the `getCallbackTokenAsync` method in read mode requires a minimum permission level of `ReadItem`.
        /// 
        /// Calling the `getCallbackTokenAsync` method in compose mode requires you to have saved the item.
        /// The `saveAsync` method requires a minimum permission level of `ReadWriteItem`.
        /// 
        /// **Important**: For guidance on delegate or shared scenarios, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// [Api set: All support Read mode; Mailbox 1.3 introduced Compose mode support]</summary>
        /// <param name="callback">- When the method completes, the function passed in the callback parameter is called with a single parameter of
        /// type `Office.AsyncResult`. The token is returned as a string in the `asyncResult.value` property.
        /// If there was an error, the `asyncResult.error` and `asyncResult.diagnostics` properties may provide additional information.</param>
        /// <param name="userContext">- Optional. Any state data that is passed to the asynchronous method.</param>
        abstract getCallbackTokenAsync: callback: (Office.AsyncResult<string> -> unit) * ?userContext: obj -> unit
        /// <summary>Gets a token identifying the user and the Office Add-in.
        /// 
        /// The token is returned as a string in the `asyncResult.value` property.</summary>
        /// <param name="callback">- When the method completes, the function passed in the callback parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.
        /// The token is returned as a string in the `asyncResult.value` property.
        /// If there was an error, the `asyncResult.error` and `asyncResult.diagnostics` properties may provide additional information.</param>
        /// <param name="userContext">- Optional. Any state data that is passed to the asynchronous method.</param>
        abstract getUserIdentityTokenAsync: callback: (Office.AsyncResult<string> -> unit) * ?userContext: obj -> unit
        /// <summary>Makes an asynchronous request to an Exchange Web Services (EWS) service on the Exchange server that hosts the user's mailbox.
        /// 
        /// In these cases, add-ins should use REST APIs to access the user's mailbox instead.
        /// 
        /// The `makeEwsRequestAsync` method sends an EWS request on behalf of the add-in to Exchange.
        /// 
        /// You cannot request Folder Associated Items with the `makeEwsRequestAsync` method.
        /// 
        /// The XML request must specify UTF-8 encoding: `\<?xml version="1.0" encoding="utf-8"?\>`.
        /// 
        /// Your add-in must have the `ReadWriteMailbox` permission to use the `makeEwsRequestAsync` method.
        /// For information about using the `ReadWriteMailbox` permission and the EWS operations that you can call with the `makeEwsRequestAsync` method,
        /// see {@link https://docs.microsoft.com/office/dev/add-ins/outlook/understanding-outlook-add-in-permissions | Specify permissions for mail add-in access to the user's mailbox}.
        /// 
        /// The XML result of the EWS call is provided as a string in the `asyncResult.value` property. 
        /// If the result exceeds 1 MB in size, an error message is returned instead.
        /// 
        /// **Note**: This method is not supported in the following scenarios:
        /// 
        /// - In Outlook on iOS or Android.
        /// 
        /// - When the add-in is loaded in a Gmail mailbox.
        /// 
        /// **Note**: The server administrator must set `OAuthAuthentication` to `true` on the Client Access Server EWS directory to enable the 
        /// `makeEwsRequestAsync` method to make EWS requests.
        /// 
        /// *Version differences*
        /// 
        /// When you use the `makeEwsRequestAsync` method in mail apps running in Outlook versions earlier than version 15.0.4535.1004, you should set 
        /// the encoding value to ISO-8859-1.
        /// 
        /// `<?xml version="1.0" encoding="iso-8859-1"?>`
        /// 
        /// You do not need to set the encoding value when your mail app is running in Outlook on the web.
        /// You can determine whether your mail app is running in Outlook or Outlook on the web by using the `mailbox.diagnostics.hostName` property.
        /// You can determine what version of Outlook is running by using the `mailbox.diagnostics.hostVersion` property.</summary>
        /// <param name="data">- The EWS request.</param>
        /// <param name="callback">- When the method completes, the function passed in the callback parameter is called with a single parameter
        ///   of type `Office.AsyncResult`.
        /// The `value` property of the result is the XML of the EWS request provided as a string. 
        /// If the result exceeds 1 MB in size, an error message is returned instead.</param>
        /// <param name="userContext">- Optional. Any state data that is passed to the asynchronous method.</param>
        abstract makeEwsRequestAsync: data: obj option * callback: (Office.AsyncResult<string> -> unit) * ?userContext: obj -> unit
        /// <summary>Removes the event handlers for a supported event type. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Mailbox object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox#events | events section}.
        /// 
        /// [Api set: Mailbox 1.5]</summary>
        /// <param name="eventType">- The event that should revoke the handler.</param>
        /// <param name="options">- Provides an option for preserving context data of any type, unchanged, for use in a callback.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract removeHandlerAsync: eventType: U2<Office.EventType, string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes the event handlers for a supported event type. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Mailbox object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox#events | events section}.
        /// 
        /// [Api set: Mailbox 1.5]</summary>
        /// <param name="eventType">- The event that should revoke the handler.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract removeHandlerAsync: eventType: U2<Office.EventType, string> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Represents the categories master list on the mailbox.
    /// 
    /// In Outlook, a user can tag messages and appointments by using a category to color-code them.
    /// The user defines categories in a master list on their mailbox. They can then apply one or more categories to an item.
    /// 
    /// **Important**: In delegate or shared scenarios, the delegate can get the categories in the master list but can't add or remove categories.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] MasterCategories =
        /// <summary>Adds categories to the master list on a mailbox. Each category must have a unique name but multiple categories can use the same color.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="categories">- The categories to be added to the master list on the mailbox.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.</param>
        abstract addAsync: categories: ResizeArray<CategoryDetails> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds categories to the master list on a mailbox. Each category must have a unique name but multiple categories can use the same color.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="categories">- The categories to be added to the master list on the mailbox.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.</param>
        abstract addAsync: categories: ResizeArray<CategoryDetails> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Gets the master list of categories on a mailbox.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. If adding categories fails, the `asyncResult.error` property will contain an error code.</param>
        abstract getAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<ResizeArray<CategoryDetails>> -> unit) -> unit
        /// <summary>Gets the master list of categories on a mailbox.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.</param>
        abstract getAsync: callback: (Office.AsyncResult<ResizeArray<CategoryDetails>> -> unit) -> unit
        /// <summary>Removes categories from the master list on a mailbox.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="categories">- The categories to be removed from the master list on the mailbox.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. If removing categories fails, the `asyncResult.error` property will contain an error code.</param>
        abstract removeAsync: categories: ResizeArray<string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes categories from the master list on a mailbox.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="categories">- The categories to be removed from the master list on the mailbox.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. If removing categories fails, the `asyncResult.error` property will contain an error code.</param>
        abstract removeAsync: categories: ResizeArray<string> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Represents a suggested meeting found in an item. Read mode only.
    /// 
    /// The list of meetings suggested in an email message is returned in the `meetingSuggestions` property of the `Entities` object that is returned when
    /// the `getEntities` or `getEntitiesByType` method is called on the active item.
    /// 
    /// The start and end values are string representations of a `Date` object that contains the date and time at which the suggested meeting is to
    /// begin and end. The values are in the default time zone specified for the current user.
    type [<AllowNullLiteral>] MeetingSuggestion =
        /// Gets the attendees for a suggested meeting.
        abstract attendees: ResizeArray<EmailUser> with get, set
        /// Gets the date and time that a suggested meeting is to end.
        abstract ``end``: string with get, set
        /// Gets the location of a suggested meeting.
        abstract location: string with get, set
        /// Gets a string that was identified as a meeting suggestion.
        abstract meetingString: string with get, set
        /// Gets the date and time that a suggested meeting is to begin.
        abstract start: string with get, set
        /// Gets the subject of a suggested meeting.
        abstract subject: string with get, set

    /// A subclass of {@link Office.Item | Item} for messages.
    /// 
    /// **Important**: This is an internal Outlook object, not directly exposed through existing interfaces. 
    /// You should treat this as a mode of `Office.context.mailbox.item`. For more information, refer to the
    /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item | Object Model} page.
    /// 
    /// Child interfaces:
    /// 
    /// - {@link Office.MessageCompose | MessageCompose}
    /// 
    /// - {@link Office.MessageRead | MessageRead}
    type [<AllowNullLiteral>] Message =
        inherit Item

    /// The message compose mode of {@link Office.Item | Office.context.mailbox.item}.
    /// 
    /// **Important**: This is an internal Outlook object, not directly exposed through existing interfaces. 
    /// You should treat this as a mode of `Office.context.mailbox.item`. For more information, refer to the
    /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item | Object Model} page.
    /// 
    /// Parent interfaces:
    /// 
    /// - {@link Office.ItemCompose | ItemCompose}
    /// 
    /// - {@link Office.Message | Message}
    type [<AllowNullLiteral>] MessageCompose =
        inherit Message
        inherit ItemCompose
        /// Gets an object that provides methods to get or update the recipients on the **Bcc** (blind carbon copy) line of a message.
        /// 
        /// Depending on the client/platform (i.e., Windows, Mac, etc.), limits may apply on how many recipients you can get or update.
        /// See the {@link Office.Recipients | Recipients} object for more details.
        /// 
        /// [Api set: Mailbox 1.1]
        abstract bcc: Recipients with get, set
        /// Gets an object that provides methods for manipulating the body of an item.
        /// 
        /// [Api set: Mailbox 1.1]
        abstract body: Body with get, set
        /// Gets an object that provides methods for managing the item's categories.
        /// 
        /// **Important**: In Outlook on the web, you can't use the API to manage categories on a message in Compose mode.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract categories: Categories with get, set
        /// Provides access to the Cc (carbon copy) recipients of a message. The type of object and level of access depend on the mode of the 
        /// current item.
        /// 
        /// The `cc` property returns a `Recipients` object that provides methods to get or update the recipients on the
        /// **Cc** line of the message. However, depending on the client/platform (i.e., Windows, Mac, etc.), limits may apply on how many recipients
        /// you can get or update. See the {@link Office.Recipients | Recipients} object for more details.
        abstract cc: Recipients with get, set
        /// Gets an identifier for the email conversation that contains a particular message.
        /// 
        /// You can get an integer for this property if your mail app is activated in read forms or responses in compose forms. 
        /// If subsequently the user changes the subject of the reply message, upon sending the reply, the conversation ID for that message will change 
        /// and that value you obtained earlier will no longer apply.
        /// 
        /// You get null for this property for a new item in a compose form. 
        /// If the user sets a subject and saves the item, the `conversationId` property will return a value.
        abstract conversationId: string with get, set
        /// Gets the email address of the sender of a message.
        /// 
        /// The `from` property returns a `From` object that provides a method to get the from value.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract from: From with get, set
        /// Gets or sets the custom internet headers of a message.
        /// 
        /// The `internetHeaders` property returns an `InternetHeaders` object that provides methods to manage the internet headers on the message.
        /// 
        /// To learn more, see
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/internet-headers | Get and set internet headers on a message in an Outlook add-in}.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract internetHeaders: InternetHeaders with get, set
        /// Gets the type of item that an instance represents.
        /// 
        /// The `itemType` property returns one of the `ItemType` enumeration values, indicating whether the item object instance is a message or
        /// an appointment.
        abstract itemType: U2<MailboxEnums.ItemType, string> with get, set
        /// Gets the notification messages for an item.
        /// 
        /// [Api set: Mailbox 1.3]
        abstract notificationMessages: NotificationMessages with get, set
        /// Gets the ID of the series that an instance belongs to.
        /// 
        /// In Outlook on the web and desktop clients, the `seriesId` returns the Exchange Web Services (EWS) ID of the parent (series) item
        /// that this item belongs to. However, on iOS and Android, the seriesId returns the REST ID of the parent item.
        /// 
        /// **Note**: The identifier returned by the `seriesId` property is the same as the Exchange Web Services item identifier.
        /// The `seriesId` property is not identical to the Outlook IDs used by the Outlook REST API.
        /// Before making REST API calls using this value, it should be converted using `Office.context.mailbox.convertToRestId`.
        /// For more details, see {@link https://docs.microsoft.com/office/dev/add-ins/outlook/use-rest-api | Use the Outlook REST APIs from an Outlook add-in}.
        /// 
        /// The `seriesId` property returns `null` for items that do not have parent items such as single appointments, series items, or meeting requests
        /// and returns `undefined` for any other items that are not meeting requests.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract seriesId: string with get, set
        /// Gets or sets the description that appears in the subject field of an item.
        /// 
        /// The `subject` property gets or sets the entire subject of the item, as sent by the email server.
        /// 
        /// The `subject` property returns a `Subject` object that provides methods to get and set the subject.
        abstract subject: Subject with get, set
        /// Provides access to the recipients on the **To** line of a message. The type of object and level of access depend on the mode of the
        /// current item.
        /// 
        /// The `to` property returns a `Recipients` object that provides methods to get or update the recipients on the
        /// **To** line of the message. However, depending on the client/platform (i.e., Windows, Mac, etc.), limits may apply on how many recipients
        /// you can get or update. See the {@link Office.Recipients | Recipients} object for more details.
        abstract ``to``: Recipients with get, set
        /// <summary>Adds a file to a message or appointment as an attachment.
        /// 
        /// The `addFileAttachmentAsync` method uploads the file at the specified URI and attaches it to the item in the compose form.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// **Important**: In recent builds of Outlook on Windows, a bug was introduced that incorrectly appends an `Authorization: Bearer` header to
        /// this action (whether using this API or the Outlook UI). To work around this issue, you can try using the `addFileAttachmentFromBase64` API
        /// introduced with requirement set 1.8.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="uri">- The URI that provides the location of the file to attach to the message or appointment. The maximum length is 2048 characters.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `isInline`: If true, indicates that the attachment will be shown inline in the message body, and should not be displayed in the 
        /// attachment list.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. On success, the attachment identifier will be provided in the `asyncResult.value` property. 
        /// If uploading the attachment fails, the `asyncResult` object will contain an `Error` object that provides a description of 
        /// the error.</param>
        abstract addFileAttachmentAsync: uri: string * attachmentName: string * options: obj * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Adds a file to a message or appointment as an attachment.
        /// 
        /// The `addFileAttachmentAsync` method uploads the file at the specified URI and attaches it to the item in the compose form.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// **Important**: In recent builds of Outlook on Windows, a bug was introduced that incorrectly appends an `Authorization: Bearer` header to
        /// this action (whether using this API or the Outlook UI). To work around this issue, you can try using the `addFileAttachmentFromBase64` API
        /// introduced with requirement set 1.8.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="uri">- The URI that provides the location of the file to attach to the message or appointment. The maximum length is 2048 characters.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. On success, the attachment identifier will be provided in the `asyncResult.value` property. 
        /// If uploading the attachment fails, the `asyncResult` object will contain an `Error` object that provides a description of 
        /// the error.</param>
        abstract addFileAttachmentAsync: uri: string * attachmentName: string * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Adds a file to a message or appointment as an attachment.
        /// 
        /// The `addFileAttachmentFromBase64Async` method uploads the file from the base64 encoding and attaches it to the item in the compose form.
        /// This method returns the attachment identifier in the `asyncResult.value` object.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// **Note**: If you're using a data URL API (e.g., `readAsDataURL`), you need to strip out the data URL prefix then send the rest of the string to this API.
        /// For example, if the full string is represented by `data:image/svg+xml;base64,<rest of base64 string>`, remove `data:image/svg+xml;base64,`.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="base64File">- The base64-encoded content of an image or file to be added to an email or event.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `isInline`: If true, indicates that the attachment will be shown inline in the message body and should not be displayed in the attachment list.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        ///             type Office.AsyncResult. On success, the attachment identifier will be provided in the `asyncResult.value` property.
        ///  If uploading the attachment fails, the `asyncResult` object will contain an `Error` object that provides a description of the error.</param>
        abstract addFileAttachmentFromBase64Async: base64File: string * attachmentName: string * options: obj * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Adds a file to a message or appointment as an attachment.
        /// 
        /// The `addFileAttachmentFromBase64Async` method uploads the file from the base64 encoding and attaches it to the item in the compose form.
        /// This method returns the attachment identifier in the `asyncResult.value` object.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// **Note**: If you're using a data URL API (e.g., `readAsDataURL`), you need to strip out the data URL prefix then send the rest of the string to this API.
        /// For example, if the full string is represented by `data:image/svg+xml;base64,<rest of base64 string>`, remove `data:image/svg+xml;base64,`.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="base64File">- The base64-encoded content of an image or file to be added to an email or event.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        ///             type Office.AsyncResult. On success, the attachment identifier will be provided in the `asyncResult.value` property.
        ///  If uploading the attachment fails, the `asyncResult` object will contain an `Error` object that provides a description of the error.</param>
        abstract addFileAttachmentFromBase64Async: base64File: string * attachmentName: string * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Adds an event handler for a supported event. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should invoke the handler.</param>
        /// <param name="handler">- The function to handle the event. The function must accept a single parameter, which is an object literal.
        /// The `type` property on the parameter will match the `eventType` parameter passed to `addHandlerAsync`.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract addHandlerAsync: eventType: U2<Office.EventType, string> * handler: obj option * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an event handler for a supported event. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should invoke the handler.</param>
        /// <param name="handler">- The function to handle the event. The function must accept a single parameter, which is an object literal.
        /// The `type` property on the parameter will match the `eventType` parameter passed to `addHandlerAsync`.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract addHandlerAsync: eventType: U2<Office.EventType, string> * handler: obj option * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an Exchange item, such as a message, as an attachment to the message or appointment.
        /// 
        /// The `addItemAttachmentAsync` method attaches the item with the specified Exchange identifier to the item in the compose form. 
        /// If you specify a callback method, the method is called with one parameter, `asyncResult`, which contains either the attachment identifier or 
        /// a code that indicates any error that occurred while attaching the item. You can use the options parameter to pass state information to the 
        /// callback method, if needed.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// If your Office Add-in is running in Outlook on the web, the `addItemAttachmentAsync` method can attach items to items other than the item that 
        /// you are editing; however, this is not supported and is not recommended.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="itemId">- The Exchange identifier of the item to attach. The maximum length is 100 characters.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. On success, the attachment identifier will be provided in the `asyncResult.value` property. 
        /// If adding the attachment fails, the `asyncResult` object will contain an `Error` object that provides a description of 
        /// the error.</param>
        abstract addItemAttachmentAsync: itemId: obj option * attachmentName: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Adds an Exchange item, such as a message, as an attachment to the message or appointment.
        /// 
        /// The `addItemAttachmentAsync` method attaches the item with the specified Exchange identifier to the item in the compose form. 
        /// If you specify a callback method, the method is called with one parameter, `asyncResult`, which contains either the attachment identifier or 
        /// a code that indicates any error that occurred while attaching the item. You can use the options parameter to pass state information to the 
        /// callback method, if needed.
        /// 
        /// You can subsequently use the identifier with the `removeAttachmentAsync` method to remove the attachment in the same session.
        /// 
        /// If your Office Add-in is running in Outlook on the web, the `addItemAttachmentAsync` method can attach items to items other than the item that 
        /// you are editing; however, this is not supported and is not recommended.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="itemId">- The Exchange identifier of the item to attach. The maximum length is 100 characters.</param>
        /// <param name="attachmentName">- The name of the attachment that is shown while the attachment is uploading. The maximum length is 255 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. On success, the attachment identifier will be provided in the `asyncResult.value` property. 
        /// If adding the attachment fails, the `asyncResult` object will contain an `Error` object that provides a description of 
        /// the error.</param>
        abstract addItemAttachmentAsync: itemId: obj option * attachmentName: string * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// Closes the current item that is being composed
        /// 
        /// The behaviors of the close method depends on the current state of the item being composed. 
        /// If the item has unsaved changes, the client prompts the user to save, discard, or close the action.
        /// 
        /// In the Outlook desktop client, if the message is an inline reply, the close method has no effect.
        /// 
        /// **Note**: In Outlook on the web, if the item is an appointment and it has previously been saved using `saveAsync`, the user is prompted to save, 
        /// discard, or cancel even if no changes have occurred since the item was last saved.
        /// 
        /// [Api set: Mailbox 1.3]
        abstract close: unit -> unit
        /// <summary>Disables the Outlook client signature.
        /// 
        /// For Windows and Mac rich clients, this API sets the signature under the "New Message" and "Replies/Forwards" sections
        /// for the sending account to "(none)", effectively disabling the signature.
        /// For Outlook on the web, the API should disable the signature option for new mails, replies, and forwards.
        /// If the signature is selected, this API call should disable it.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the callback parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract disableClientSignatureAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Disables the Outlook client signature.
        /// 
        /// For Windows and Mac rich clients, this API sets the signature under the "New Message" and "Replies/Forwards" sections
        /// for the sending account to "(none)", effectively disabling the signature.
        /// For Outlook on the web, the API should disable the signature option for new mails, replies, and forwards.
        /// If the signature is selected, this API call should disable it.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the callback parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract disableClientSignatureAsync: ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Gets an attachment from a message or appointment and returns it as an `AttachmentContent` object.
        /// 
        /// The `getAttachmentContentAsync` method gets the attachment with the specified identifier from the item. As a best practice, you should use
        /// the identifier to retrieve an attachment in the same session that the attachment IDs were retrieved with the `getAttachmentsAsync` or
        /// `item.attachments` call. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session.
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="attachmentId">- The identifier of the attachment you want to get.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object. If the call fails, the `asyncResult.error` property will contain
        /// an error code with the reason for the failure.</param>
        abstract getAttachmentContentAsync: attachmentId: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<AttachmentContent> -> unit) -> unit
        /// <summary>Gets an attachment from a message or appointment and returns it as an `AttachmentContent` object.
        /// 
        /// The `getAttachmentContentAsync` method gets the attachment with the specified identifier from the item. As a best practice, you should use
        /// the identifier to retrieve an attachment in the same session that the attachment IDs were retrieved with the `getAttachmentsAsync` or
        /// `item.attachments` call. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session.
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="attachmentId">- The identifier of the attachment you want to get.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object. If the call fails, the `asyncResult.error` property will contain
        /// an error code with the reason for the failure.</param>
        abstract getAttachmentContentAsync: attachmentId: string * ?callback: (Office.AsyncResult<AttachmentContent> -> unit) -> unit
        /// <summary>Gets the item's attachments as an array.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If the call fails, the `asyncResult.error` property will contain an error code with the reason for
        /// the failure.</param>
        abstract getAttachmentsAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<ResizeArray<AttachmentDetailsCompose>> -> unit) -> unit
        /// <summary>Gets the item's attachments as an array.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If the call fails, the `asyncResult.error` property will contain an error code with the reason for
        /// the failure.</param>
        abstract getAttachmentsAsync: ?callback: (Office.AsyncResult<ResizeArray<AttachmentDetailsCompose>> -> unit) -> unit
        /// <summary>Specifies the type of message compose and its coercion type. The message can be new, or a reply or forward.
        /// The coercion type can be HTML or plain text.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. On success, the `asyncResult.value` property contains an object with the item's compose type
        /// and coercion type.</param>
        abstract getComposeTypeAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<obj option> -> unit) -> unit
        /// <summary>Specifies the type of message compose and its coercion type. The message can be new, or a reply or forward.
        /// The coercion type can be HTML or plain text.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. On success, the `asyncResult.value` property contains an object with the item's compose type
        /// and coercion type.</param>
        abstract getComposeTypeAsync: callback: (Office.AsyncResult<obj option> -> unit) -> unit
        /// <summary>Asynchronously gets the ID of a saved item.
        /// 
        /// When invoked, this method returns the item ID via the callback method.
        /// 
        /// **Note**: If your add-in calls `getItemIdAsync` on an item in compose mode (e.g., to get an `itemId` to use with EWS or the REST API),
        /// be aware that when Outlook is in cached mode, it may take some time before the item is synced to the server.
        /// Until the item is synced, the `itemId` is not recognized and using it returns an error.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///   of type `Office.AsyncResult`.</param>
        abstract getItemIdAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously gets the ID of a saved item.
        /// 
        /// When invoked, this method returns the item ID via the callback method.
        /// 
        /// **Note**: If your add-in calls `getItemIdAsync` on an item in compose mode (e.g., to get an `itemId` to use with EWS or the REST API),
        /// be aware that when Outlook is in cached mode, it may take some time before the item is synced to the server.
        /// Until the item is synced, the `itemId` is not recognized and using it returns an error.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///   of type `Office.AsyncResult`.</param>
        abstract getItemIdAsync: callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously returns selected data from the subject or body of a message.
        /// 
        /// If there is no selection but the cursor is in the body or subject, the method returns an empty string for the selected data. 
        /// If a field other than the body or subject is selected, the method returns the `InvalidSelection` error.
        /// 
        /// To access the selected data from the callback method, call `asyncResult.value.data`.
        /// To access the source property that the selection comes from, call `asyncResult.value.sourceProperty`, which will be either `body` or `subject`.
        /// 
        /// [Api set: Mailbox 1.2]</summary>
        /// <param name="coercionType">- Requests a format for the data. If `Text`, the method returns the plain text as a string, removing any HTML tags present. 
        /// If `Html`, the method returns the selected text, whether it is plaintext or HTML.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.</param>
        abstract getSelectedDataAsync: coercionType: U2<Office.CoercionType, string> * options: Office.AsyncContextOptions * callback: (Office.AsyncResult<obj option> -> unit) -> unit
        /// <summary>Asynchronously returns selected data from the subject or body of a message.
        /// 
        /// If there is no selection but the cursor is in the body or subject, the method returns an empty string for the selected data. 
        /// If a field other than the body or subject is selected, the method returns the `InvalidSelection` error.
        /// 
        /// To access the selected data from the callback method, call `asyncResult.value.data`.
        /// To access the source property that the selection comes from, call `asyncResult.value.sourceProperty`, which will be either `body` or `subject`.
        /// 
        /// [Api set: Mailbox 1.2]</summary>
        /// <param name="coercionType">- Requests a format for the data. If `Text`, the method returns the plain text as a string, removing any HTML tags present. 
        /// If `Html`, the method returns the selected text, whether it is plaintext or HTML.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`.</param>
        abstract getSelectedDataAsync: coercionType: U2<Office.CoercionType, string> * callback: (Office.AsyncResult<obj option> -> unit) -> unit
        /// <summary>Gets the properties of an appointment or message in a shared folder.
        /// 
        /// For more information around using this API, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. The `value` property of the result is the properties of the shared item.</param>
        abstract getSharedPropertiesAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<SharedProperties> -> unit) -> unit
        /// <summary>Gets the properties of an appointment or message in a shared folder.
        /// 
        /// For more information around using this API, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. The `value` property of the result is the properties of the shared item.</param>
        abstract getSharedPropertiesAsync: callback: (Office.AsyncResult<SharedProperties> -> unit) -> unit
        /// <summary>Gets if the client signature is enabled.
        /// 
        /// For Windows and Mac rich clients, the API call should return `true` if the default signature for new messages, replies, or forwards is set
        /// to a template for the sending Outlook account.
        /// For Outlook on the web, the API call should return `true` if the signature is enabled for compose types `newMail`, `reply`, or `forward`.
        /// If the settings are set to "(none)" in Mac or Windows rich clients or disabled in Outlook on the Web, the API call should return `false`.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        ///   type `Office.AsyncResult`.</param>
        abstract isClientSignatureEnabledAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<bool> -> unit) -> unit
        /// <summary>Gets if the client signature is enabled.
        /// 
        /// For Windows and Mac rich clients, the API call should return `true` if the default signature for new messages, replies, or forwards is set
        /// to a template for the sending Outlook account.
        /// For Outlook on the web, the API call should return `true` if the signature is enabled for compose types `newMail`, `reply`, or `forward`.
        /// If the settings are set to "(none)" in Mac or Windows rich clients or disabled in Outlook on the Web, the API call should return `false`.
        /// 
        /// [Api set: Mailbox 1.10]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        ///   type `Office.AsyncResult`.</param>
        abstract isClientSignatureEnabledAsync: callback: (Office.AsyncResult<bool> -> unit) -> unit
        /// <summary>Asynchronously loads custom properties for this add-in on the selected item.
        /// 
        /// Custom properties are stored as key/value pairs on a per-app, per-item basis. 
        /// This method returns a `CustomProperties` object in the callback, which provides methods to access the custom properties specific to the 
        /// current item and the current add-in. Custom properties are not encrypted on the item, so this should not be used as secure storage.
        /// 
        /// The custom properties are provided as a `CustomProperties` object in the asyncResult.value property. 
        /// This object can be used to get, set, and remove custom properties from the item and save changes to the custom property set back to 
        /// the server.</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        /// <param name="userContext">- Optional. Developers can provide any object they wish to access in the callback function.
        /// This object can be accessed by the `asyncResult.asyncContext` property in the callback function.</param>
        abstract loadCustomPropertiesAsync: callback: (Office.AsyncResult<CustomProperties> -> unit) * ?userContext: obj -> unit
        /// <summary>Removes an attachment from a message or appointment.
        /// 
        /// The `removeAttachmentAsync` method removes the attachment with the specified identifier from the item. 
        /// As a best practice, you should use the attachment identifier to remove an attachment only if the same mail app has added that attachment 
        /// in the same session. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session. 
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to 
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="attachmentId">- The identifier of the attachment to remove. The maximum string length of the `attachmentId`
        ///   is 200 characters in Outlook on the web and on Windows.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If removing the attachment fails, the `asyncResult.error` property will contain an error code
        /// with the reason for the failure.</param>
        abstract removeAttachmentAsync: attachmentId: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes an attachment from a message or appointment.
        /// 
        /// The `removeAttachmentAsync` method removes the attachment with the specified identifier from the item. 
        /// As a best practice, you should use the attachment identifier to remove an attachment only if the same mail app has added that attachment 
        /// in the same session. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session. 
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to 
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="attachmentId">- The identifier of the attachment to remove. The maximum string length of the `attachmentId`
        ///   is 200 characters in Outlook on the web and on Windows.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If removing the attachment fails, the `asyncResult.error` property will contain an error code
        /// with the reason for the failure.</param>
        abstract removeAttachmentAsync: attachmentId: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes the event handlers for a supported event type. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should revoke the handler.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract removeHandlerAsync: eventType: U2<Office.EventType, string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes the event handlers for a supported event type. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should revoke the handler.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract removeHandlerAsync: eventType: U2<Office.EventType, string> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Asynchronously saves an item.
        /// 
        /// When invoked, this method saves the current message as a draft and returns the item ID via the callback method.
        /// In Outlook on the web or Outlook in online mode, the item is saved to the server.
        /// In Outlook in cached mode, the item is saved to the local cache.
        /// 
        /// Since appointments have no draft state, if `saveAsync` is called on an appointment in compose mode, the item will be saved as a normal
        /// appointment on the user's calendar. For new appointments that have not been saved before, no invitation will be sent.
        /// Saving an existing appointment will send an update to added or removed attendees.
        /// 
        /// **Note**: If your add-in calls `saveAsync` on an item in compose mode in order to get an item ID to use with EWS or the REST API, be aware
        /// that when Outlook is in cached mode, it may take some time before the item is actually synced to the server.
        /// Until the item is synced, using the itemId will return an error.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract saveAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously saves an item.
        /// 
        /// When invoked, this method saves the current message as a draft and returns the item id via the callback method.
        /// In Outlook on the web or Outlook in online mode, the item is saved to the server.
        /// In Outlook in cached mode, the item is saved to the local cache.
        /// 
        /// Since appointments have no draft state, if `saveAsync` is called on an appointment in compose mode, the item will be saved as a normal
        /// appointment on the user's calendar. For new appointments that have not been saved before, no invitation will be sent.
        /// Saving an existing appointment will send an update to added or removed attendees.
        /// 
        /// **Note**: If your add-in calls `saveAsync` on an item in compose mode in order to get an item ID to use with EWS or the REST API, be aware
        /// that when Outlook is in cached mode, it may take some time before the item is actually synced to the server.
        /// Until the item is synced, using the `itemId` will return an error.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract saveAsync: callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Asynchronously inserts data into the body or subject of a message.
        /// 
        /// The `setSelectedDataAsync` method inserts the specified string at the cursor location in the subject or body of the item, or, if text is 
        /// selected in the editor, it replaces the selected text. If the cursor is not in the body or subject field, an error is returned. 
        /// After insertion, the cursor is placed at the end of the inserted content.
        /// 
        /// [Api set: Mailbox 1.2]</summary>
        /// <param name="data">- The data to be inserted. Data is not to exceed 1,000,000 characters. 
        /// If more than 1,000,000 characters are passed in, an `ArgumentOutOfRange` exception is thrown.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        /// `coercionType`: If text, the current style is applied in Outlook on the web and desktop clients. 
        /// If the field is an HTML editor, only the text data is inserted, even if the data is HTML. 
        /// If html and the field supports HTML (the subject doesn't), the current style is applied in Outlook on the web and the default style is
        /// applied in Outlook on desktop clients. If the field is a text field, an `InvalidDataFormat` error is returned.
        /// If `coercionType` is not set, the result depends on the field:
        /// if the field is HTML then HTML is used; if the field is text, then plain text is used.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract setSelectedDataAsync: data: string * options: obj * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Asynchronously inserts data into the body or subject of a message.
        /// 
        /// The `setSelectedDataAsync` method inserts the specified string at the cursor location in the subject or body of the item, or, if text is 
        /// selected in the editor, it replaces the selected text. If the cursor is not in the body or subject field, an error is returned. 
        /// After insertion, the cursor is placed at the end of the inserted content.
        /// 
        /// [Api set: Mailbox 1.2]</summary>
        /// <param name="data">- The data to be inserted. Data is not to exceed 1,000,000 characters. 
        /// If more than 1,000,000 characters are passed in, an `ArgumentOutOfRange` exception is thrown.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        abstract setSelectedDataAsync: data: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// The message read mode of {@link Office.Item | Office.context.mailbox.item}.
    /// 
    /// **Important**: This is an internal Outlook object, not directly exposed through existing interfaces. 
    /// You should treat this as a mode of `Office.context.mailbox.item`. For more information, refer to the
    /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item | Object Model} page.
    /// 
    /// Parent interfaces:
    /// 
    /// - {@link Office.ItemRead | ItemRead}
    /// 
    /// - {@link Office.Message | Message}
    type [<AllowNullLiteral>] MessageRead =
        inherit Message
        inherit ItemRead
        /// Gets the item's attachments as an array.
        abstract attachments: ResizeArray<AttachmentDetails> with get, set
        /// Gets an object that provides methods for manipulating the body of an item.
        /// 
        /// [Api set: Mailbox 1.1]
        abstract body: Body with get, set
        /// Gets an object that provides methods for managing the item's categories.
        /// 
        /// [Api set: Mailbox 1.8]
        abstract categories: Categories with get, set
        /// Provides access to the Cc (carbon copy) recipients of a message. The type of object and level of access depend on the mode of the 
        /// current item.
        /// 
        /// The `cc` property returns an array that contains an {@link Office.EmailAddressDetails | EmailAddressDetails} object for
        /// each recipient listed on the **Cc** line of the message. Collection size limits:
        /// 
        /// - Windows: 500 members
        /// 
        /// - Mac: 100 members
        /// 
        /// - Web browser: 20 members
        /// 
        /// - Other: No limit
        abstract cc: ResizeArray<EmailAddressDetails> with get, set
        /// Gets an identifier for the email conversation that contains a particular message.
        /// 
        /// You can get an integer for this property if your mail app is activated in read forms or responses in compose forms.
        /// If subsequently the user changes the subject of the reply message, upon sending the reply, the conversation ID for that message will change
        /// and that value you obtained earlier will no longer apply.
        /// 
        /// You get null for this property for a new item in a compose form.
        /// If the user sets a subject and saves the item, the `conversationId` property will return a value.
        abstract conversationId: string with get, set
        /// Gets the date and time that an item was created.
        abstract dateTimeCreated: DateTime with get, set
        /// Gets the date and time that an item was last modified.
        /// 
        /// **Note**: This member is not supported in Outlook on iOS or Android.
        abstract dateTimeModified: DateTime with get, set
        /// Gets the date and time that the appointment is to end.
        /// 
        /// The `end` property is a `Date` object expressed as a Coordinated Universal Time (UTC) date and time value. 
        /// You can use the `convertToLocalClientTime` method to convert the `end` property value to the client's local date and time.
        /// 
        /// When you use the `Time.setAsync` method to set the end time, you should use the `convertToUtcClientTime` method to convert the local time on 
        /// the client to UTC for the server.
        abstract ``end``: DateTime with get, set
        /// Gets the email address of the sender of a message.
        /// 
        /// The `from` and `sender` properties represent the same person unless the message is sent by a delegate.
        /// In that case, the `from` property represents the delegator, and the `sender` property represents the delegate.
        /// 
        /// **Note**: The `recipientType` property of the `EmailAddressDetails` object in the `from` property is undefined.
        /// 
        /// The `from` property returns an `EmailAddressDetails` object.
        abstract from: EmailAddressDetails with get, set
        /// Gets the internet message identifier for an email message.
        /// 
        /// **Important**: In the **Sent Items** folder, the `internetMessageId` may not be available yet on recently sent items. In that case,
        /// consider using {@link https://docs.microsoft.com/office/dev/add-ins/outlook/web-services | Exchange Web Services} to get this
        /// {@link https://docs.microsoft.com/exchange/client-developer/web-service-reference/internetmessageid | property from the server}.
        abstract internetMessageId: string with get, set
        /// Gets the Exchange Web Services item class of the selected item.
        /// 
        /// You can create custom message classes that extends a default message class, for example, a custom appointment message class
        /// `IPM.Appointment.Contoso`.
        abstract itemClass: string with get, set
        /// Gets the {@link https://docs.microsoft.com/exchange/client-developer/exchange-web-services/ews-identifiers-in-exchange | Exchange Web Services item identifier}
        /// for the current item.
        /// 
        /// The `itemId` property is not available in compose mode.
        /// If an item identifier is required, the `saveAsync` method can be used to save the item to the store, which will return the item identifier
        /// in the `asyncResult.value` parameter in the callback function.
        /// 
        /// **Note**: The identifier returned by the `itemId` property is the same as the
        /// {@link https://docs.microsoft.com/exchange/client-developer/exchange-web-services/ews-identifiers-in-exchange | Exchange Web Services item identifier}.
        /// The `itemId` property is not identical to the Outlook Entry ID or the ID used by the Outlook REST API.
        /// Before making REST API calls using this value, it should be converted using `Office.context.mailbox.convertToRestId`.
        /// For more details, see {@link https://docs.microsoft.com/office/dev/add-ins/outlook/use-rest-api#get-the-item-id | Use the Outlook REST APIs from an Outlook add-in}.
        abstract itemId: string with get, set
        /// Gets the type of item that an instance represents.
        /// 
        /// The `itemType` property returns one of the `ItemType` enumeration values, indicating whether the item object instance is a message or 
        /// an appointment.
        abstract itemType: U2<MailboxEnums.ItemType, string> with get, set
        /// Gets the location of a meeting request.
        /// 
        /// The `location` property returns a string that contains the location of the appointment.
        abstract location: string with get, set
        /// Gets the subject of an item, with all prefixes removed (including RE: and FWD:).
        /// 
        /// The `normalizedSubject` property gets the subject of the item, with any standard prefixes (such as RE: and FW:) that are added by
        /// email programs. To get the subject of the item with the prefixes intact, use the `subject` property.
        abstract normalizedSubject: string with get, set
        /// Gets the notification messages for an item.
        /// 
        /// [Api set: Mailbox 1.3]
        abstract notificationMessages: NotificationMessages with get, set
        /// Gets the recurrence pattern of an appointment. Gets the recurrence pattern of a meeting request.
        /// Read and compose modes for appointment items. Read mode for meeting request items.
        /// 
        /// The `recurrence` property returns a `Recurrence` object for recurring appointments or meetings requests if an item is a series or an instance
        /// in a series. `null` is returned for single appointments and meeting requests of single appointments.
        /// `undefined` is returned for messages that are not meeting requests.
        /// 
        /// **Note**: Meeting requests have an itemClass value of `IPM.Schedule.Meeting.Request`.
        /// 
        /// **Note**: If the `recurrence` object is null, this indicates that the object is a single appointment or a meeting request of a single appointment
        /// and NOT a part of a series.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract recurrence: Recurrence with get, set
        /// Gets the id of the series that an instance belongs to.
        /// 
        /// In Outlook on the web and desktop clients, the `seriesId` returns the Exchange Web Services (EWS) ID of the parent (series) item
        /// that this item belongs to. However, on iOS and Android, the `seriesId` returns the REST ID of the parent item.
        /// 
        /// **Note**: The identifier returned by the `seriesId` property is the same as the Exchange Web Services item identifier.
        /// The `seriesId` property is not identical to the Outlook IDs used by the Outlook REST API.
        /// Before making REST API calls using this value, it should be converted using `Office.context.mailbox.convertToRestId`.
        /// For more details, see {@link https://docs.microsoft.com/office/dev/add-ins/outlook/use-rest-api | Use the Outlook REST APIs from an Outlook add-in}.
        /// 
        /// The `seriesId` property returns `null` for items that do not have parent items such as single appointments, series items, or meeting requests
        /// and returns `undefined` for any other items that are not meeting requests.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract seriesId: string with get, set
        /// Gets the email address of the sender of an email message.
        /// 
        /// The `from` and `sender` properties represent the same person unless the message is sent by a delegate.
        /// In that case, the `from` property represents the delegator, and the `sender` property represents the delegate.
        /// 
        /// **Note**: The `recipientType` property of the `EmailAddressDetails` object in the `sender` property is undefined.
        abstract sender: EmailAddressDetails with get, set
        /// Gets the date and time that the appointment is to begin.
        /// 
        /// The `start` property is a `Date` object expressed as a Coordinated Universal Time (UTC) date and time value.
        /// You can use the `convertToLocalClientTime` method to convert the value to the client's local date and time.
        abstract start: DateTime with get, set
        /// Gets the description that appears in the subject field of an item.
        /// 
        /// The `subject` property gets or sets the entire subject of the item, as sent by the email server.
        /// 
        /// The `subject` property returns a string. Use the `normalizedSubject` property to get the subject minus any leading prefixes such as RE: and FW:.
        abstract subject: string with get, set
        /// Provides access to the recipients on the **To** line of a message. The type of object and level of access depend on the mode of the
        /// current item.
        /// 
        /// The `to` property returns an array that contains an {@link Office.EmailAddressDetails | EmailAddressDetails} object for
        /// each recipient listed on the **To** line of the message. Collection size limits:
        /// 
        /// - Windows: 500 members
        /// 
        /// - Mac: 100 members
        /// 
        /// - Web browser: 20 members
        /// 
        /// - Other: No limit
        abstract ``to``: ResizeArray<EmailAddressDetails> with get, set
        /// <summary>Adds an event handler for a supported event. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should invoke the handler.</param>
        /// <param name="handler">- The function to handle the event. The function must accept a single parameter, which is an object literal.
        /// The `type` property on the parameter will match the eventType `parameter` passed to `addHandlerAsync`.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract addHandlerAsync: eventType: U2<Office.EventType, string> * handler: obj option * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds an event handler for a supported event. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should invoke the handler.</param>
        /// <param name="handler">- The function to handle the event. The function must accept a single parameter, which is an object literal.
        /// The `type` property on the parameter will match the eventType `parameter` passed to `addHandlerAsync`.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract addHandlerAsync: eventType: U2<Office.EventType, string> * handler: obj option * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a reply form that includes either the sender and all recipients of the selected message or the organizer and all attendees of the
        /// selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyAllForm` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        abstract displayReplyAllForm: formData: U2<string, ReplyFormData> -> unit
        /// <summary>Displays a reply form that includes either the sender and all recipients of the selected message or the organizer and all attendees of the
        /// selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyAllFormAsync` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayReplyAllFormAsync: formData: U2<string, ReplyFormData> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a reply form that includes either the sender and all recipients of the selected message or the organizer and all attendees of the
        /// selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyAllFormAsync` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayReplyAllFormAsync: formData: U2<string, ReplyFormData> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a reply form that includes only the sender of the selected message or the organizer of the selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyForm` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        abstract displayReplyForm: formData: U2<string, ReplyFormData> -> unit
        /// <summary>Displays a reply form that includes only the sender of the selected message or the organizer of the selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyFormAsync` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayReplyFormAsync: formData: U2<string, ReplyFormData> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Displays a reply form that includes only the sender of the selected message or the organizer of the selected appointment.
        /// 
        /// In Outlook on the web, the reply form is displayed as a pop-out form in the 3-column view and a pop-up form in the 2-column or 1-column view.
        /// 
        /// If any of the string parameters exceed their limits, `displayReplyFormAsync` throws an exception.
        /// 
        /// When attachments are specified in the `formData.attachments` parameter, Outlook attempts to download all attachments and attach them to the
        /// reply form. If any attachments fail to be added, an error is shown in the form UI. If this isn't possible, then no error message is thrown.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.9]</summary>
        /// <param name="formData">- A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB
        ///   OR a {@link Office.ReplyFormData | ReplyFormData} object that contains body or attachment data and a callback function.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract displayReplyFormAsync: formData: U2<string, ReplyFormData> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Gets all the internet headers for the message as a string.
        /// 
        /// To learn more, see
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/internet-headers | Get and set internet headers on a message in an Outlook add-in}.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.
        /// On success, the internet headers data is provided in the `asyncResult.value` property as a string.
        /// Refer to {@link https://tools.ietf.org/html/rfc2183 | RFC 2183} for the formatting information of the returned string value.
        /// If the call fails, the `asyncResult.error` property will contain an error code with the reason for the failure.</param>
        abstract getAllInternetHeadersAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Gets all the internet headers for the message as a string.
        /// 
        /// To learn more, see
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/internet-headers | Get and set internet headers on a message in an Outlook add-in}.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.
        /// On success, the internet headers data is provided in the `asyncResult.value` property as a string.
        /// Refer to {@link https://tools.ietf.org/html/rfc2183 | RFC 2183} for the formatting information of the returned string value.
        /// If the call fails, the `asyncResult.error` property will contain an error code with the reason for the failure.</param>
        abstract getAllInternetHeadersAsync: ?callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Gets an attachment from a message or appointment and returns it as an `AttachmentContent` object.
        /// 
        /// The `getAttachmentContentAsync` method gets the attachment with the specified identifier from the item. As a best practice, you should use 
        /// the identifier to retrieve an attachment in the same session that the attachmentIds were retrieved with the `getAttachmentsAsync` or 
        /// `item.attachments` call. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session. 
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to 
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="attachmentId">- The identifier of the attachment you want to get.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object. If the call fails, the `asyncResult.error` property will contain
        /// an error code with the reason for the failure.</param>
        abstract getAttachmentContentAsync: attachmentId: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<AttachmentContent> -> unit) -> unit
        /// <summary>Gets an attachment from a message or appointment and returns it as an `AttachmentContent` object.
        /// 
        /// The `getAttachmentContentAsync` method gets the attachment with the specified identifier from the item. As a best practice, you should use 
        /// the identifier to retrieve an attachment in the same session that the attachmentIds were retrieved with the `getAttachmentsAsync` or 
        /// `item.attachments` call. In Outlook on the web and mobile devices, the attachment identifier is valid only within the same session. 
        /// A session is over when the user closes the app, or if the user starts composing an inline form then subsequently pops out the form to 
        /// continue in a separate window.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="attachmentId">- The identifier of the attachment you want to get.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object. If the call fails, the `asyncResult.error` property will contain
        /// an error code with the reason for the failure.</param>
        abstract getAttachmentContentAsync: attachmentId: string * ?callback: (Office.AsyncResult<AttachmentContent> -> unit) -> unit
        /// Gets the entities found in the selected item's body.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        abstract getEntities: unit -> Entities
        /// <summary>Gets an array of all the entities of the specified entity type found in the selected item's body.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="entityType">- One of the `EntityType` enumeration values.
        /// 
        /// While the minimum permission level to use this method is `Restricted`, some entity types require `ReadItem` to access, as specified in the 
        /// following table.
        /// 
        /// <table>
        /// <tr>
        /// <th>Value of entityType</th>
        /// <th>Type of objects in returned array</th>
        /// <th>Required Permission Level</th>
        /// </tr>
        /// <tr>
        /// <td>Address</td>
        /// <td>String</td>
        /// <td>Restricted</td>
        /// </tr>
        /// <tr>
        /// <td>Contact</td>
        /// <td>Contact</td>
        /// <td>ReadItem</td>
        /// </tr>
        /// <tr>
        /// <td>EmailAddress</td>
        /// <td>String</td>
        /// <td>ReadItem</td>
        /// </tr>
        /// <tr>
        /// <td>MeetingSuggestion</td>
        /// <td>MeetingSuggestion</td>
        /// <td>ReadItem</td>
        /// </tr>
        /// <tr>
        /// <td>PhoneNumber</td>
        /// <td>PhoneNumber</td>
        /// <td>Restricted</td>
        /// </tr>
        /// <tr>
        /// <td>TaskSuggestion</td>
        /// <td>TaskSuggestion</td>
        /// <td>ReadItem</td>
        /// </tr>
        /// <tr>
        /// <td>URL</td>
        /// <td>String</td>
        /// <td>Restricted</td>
        /// </tr>
        /// </table></param>
        abstract getEntitiesByType: entityType: U2<MailboxEnums.EntityType, string> -> ResizeArray<U5<string, Contact, MeetingSuggestion, PhoneNumber, TaskSuggestion>>
        /// <summary>Returns well-known entities in the selected item that pass the named filter defined in the manifest XML file.
        /// 
        /// The `getFilteredEntitiesByName` method returns the entities that match the regular expression defined in the `ItemHasKnownEntity` rule element 
        /// in the manifest XML file with the specified `FilterName` element value.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="name">- The name of the `ItemHasKnownEntity` rule element that defines the filter to match.</param>
        abstract getFilteredEntitiesByName: name: string -> ResizeArray<U5<string, Contact, MeetingSuggestion, PhoneNumber, TaskSuggestion>>
        /// Returns string values in the selected item that match the regular expressions defined in the manifest XML file.
        /// 
        /// The `getRegExMatches` method returns the strings that match the regular expression defined in each `ItemHasRegularExpressionMatch` or
        /// `ItemHasKnownEntity` rule element in the manifest XML file.
        /// For an `ItemHasRegularExpressionMatch` rule, a matching string has to occur in the property of the item that is specified by that rule.
        /// The `PropertyName` simple type defines the supported properties.
        /// 
        /// If you specify an `ItemHasRegularExpressionMatch` rule on the body property of an item, the regular expression should further filter
        /// the body and should not attempt to return the entire body of the item. 
        /// Using a regular expression such as .* to obtain the entire body of an item does not always return the expected results.
        /// Instead, use the `Body.getAsync` method to retrieve the entire body.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        abstract getRegExMatches: unit -> obj option
        /// <summary>Returns string values in the selected item that match the named regular expression defined in the manifest XML file.
        /// 
        /// The `getRegExMatchesByName` method returns the strings that match the regular expression defined in the
        /// `ItemHasRegularExpressionMatch` rule element in the manifest XML file with the specified `RegExName` element value.
        /// 
        /// If you specify an `ItemHasRegularExpressionMatch` rule on the body property of an item, the regular expression should further filter
        /// the body and should not attempt to return the entire body of the item. 
        /// Using a regular expression such as .* to obtain the entire body of an item does not always return the expected results.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.</summary>
        /// <param name="name">- The name of the `ItemHasRegularExpressionMatch` rule element that defines the filter to match.</param>
        abstract getRegExMatchesByName: name: string -> ResizeArray<string>
        /// Gets the entities found in a highlighted match a user has selected. Highlighted matches apply to contextual add-ins.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.6]
        abstract getSelectedEntities: unit -> Entities
        /// Returns string values in a highlighted match that match the regular expressions defined in the manifest XML file. 
        /// Highlighted matches apply to contextual add-ins.
        /// 
        /// The `getSelectedRegExMatches` method returns the strings that match the regular expression defined in
        /// each `ItemHasRegularExpressionMatch` or `ItemHasKnownEntity` rule element in the manifest XML file.
        /// For an `ItemHasRegularExpressionMatch` rule, a matching string has to occur in the property of the item that is specified by that rule.
        /// The `PropertyName` simple type defines the supported properties.
        /// 
        /// If you specify an `ItemHasRegularExpressionMatch` rule on the body property of an item, the regular expression should further filter the body
        /// and should not attempt to return the entire body of the item.
        /// Using a regular expression such as .* to obtain the entire body of an item does not always return the expected results.
        /// Instead, use the `Body.getAsync` method to retrieve the entire body.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.6]
        abstract getSelectedRegExMatches: unit -> obj option
        /// <summary>Gets the properties of an appointment or message in a shared folder.
        /// 
        /// For more information around using this API, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. The `value` property of the result is the properties of the shared item.</param>
        abstract getSharedPropertiesAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<SharedProperties> -> unit) -> unit
        /// <summary>Gets the properties of an appointment or message in a shared folder.
        /// 
        /// For more information around using this API, see the
        /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
        /// 
        /// **Note**: This method is not supported in Outlook on iOS or Android.
        /// 
        /// [Api set: Mailbox 1.8]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. The `value` property of the result is the properties of the shared item.</param>
        abstract getSharedPropertiesAsync: callback: (Office.AsyncResult<SharedProperties> -> unit) -> unit
        /// <summary>Asynchronously loads custom properties for this add-in on the selected item.
        /// 
        /// Custom properties are stored as key/value pairs on a per-app, per-item basis. 
        /// This method returns a `CustomProperties` object in the callback, which provides methods to access the custom properties specific to the
        /// current item and the current add-in. Custom properties are not encrypted on the item, so this should not be used as secure storage.
        /// 
        /// The custom properties are provided as a `CustomProperties` object in the `asyncResult.value` property.
        /// This object can be used to get, set, and remove custom properties from the item and save changes to the custom property set back to the server.</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`.</param>
        /// <param name="userContext">- Optional. Developers can provide any object they wish to access in the callback function. 
        /// This object can be accessed by the `asyncResult.asyncContext` property in the callback function.</param>
        abstract loadCustomPropertiesAsync: callback: (Office.AsyncResult<CustomProperties> -> unit) * ?userContext: obj -> unit
        /// <summary>Removes the event handlers for a supported event type. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should revoke the handler.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract removeHandlerAsync: eventType: U2<Office.EventType, string> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes the event handlers for a supported event type. **Note**: Events are only available with task pane implementation.
        /// 
        /// For supported events, refer to the Item object model
        /// {@link https://docs.microsoft.com/office/dev/add-ins/reference/objectmodel/requirement-set-1.10/office.context.mailbox.item#events | events section}.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="eventType">- The event that should revoke the handler.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract removeHandlerAsync: eventType: U2<Office.EventType, string> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// The definition of the action for a notification message.
    /// 
    /// **Important**: In modern Outlook on the web, the `NotificationMessageAction` object is available in Compose mode only.
    /// 
    /// [Api set: Mailbox 1.10]
    type [<AllowNullLiteral>] NotificationMessageAction =
        /// The type of action to be performed.
        /// `ActionType.ShowTaskPane` is the only supported action.
        abstract actionType: U2<string, MailboxEnums.ActionType> with get, set
        /// The text of the action link.
        abstract actionText: string with get, set
        /// The button defined in the manifest based on the item type.
        abstract commandId: string with get, set

    /// An array of `NotificationMessageDetails` objects are returned by the `NotificationMessages.getAllAsync` method.
    /// 
    /// [Api set: Mailbox 1.3]
    type [<AllowNullLiteral>] NotificationMessageDetails =
        /// The identifier for the notification message.
        abstract key: string option with get, set
        /// Specifies the `ItemNotificationMessageType` of message.
        /// 
        /// If type is `ProgressIndicator` or `ErrorMessage`, an icon is automatically supplied
        /// and the message is not persistent. Therefore the icon and persistent properties are not valid for these types of messages.
        /// Including them will result in an `ArgumentException`.
        /// 
        /// If type is `ProgressIndicator`, the developer should remove or replace the progress indicator when the action is complete.
        abstract ``type``: U2<MailboxEnums.ItemNotificationMessageType, string> with get, set
        /// A reference to an icon that is defined in the manifest in the `Resources` section. It appears in the infobar area.
        /// It is only applicable if the type is `InformationalMessage`. Specifying this parameter for an unsupported type results in an exception.
        /// 
        /// **Note**: At present, the custom icon is displayed in Outlook on Windows only and not on other clients (e.g., Mac, web browser).
        abstract icon: string option with get, set
        /// The text of the notification message. Maximum length is 150 characters.
        /// If the developer passes in a longer string, an `ArgumentOutOfRange` exception is thrown.
        abstract message: string with get, set
        /// Specifies if the message should be persistent. Only applicable when type is `InformationalMessage`.
        /// If true, the message remains until removed by this add-in or dismissed by the user.
        /// If false, it is removed when the user navigates to a different item.
        /// For error notifications, the message persists until the user sees it once.
        /// Specifying this parameter for an unsupported type throws an exception.
        abstract persistent: Boolean option with get, set
        /// Specifies actions for the message. Limit: 1 action. This limit doesn't count the "Dismiss" action which is included by default.
        /// Only applicable when the type is `InsightMessage`.
        /// Specifying this property for an unsupported type or including too many actions throws an error.
        /// 
        /// **Important**: In modern Outlook on the web, the `actions` property is available in Compose mode only.
        /// 
        /// [Api set: Mailbox 1.10]
        abstract actions: ResizeArray<NotificationMessageAction> option with get, set

    /// The `NotificationMessages` object is returned as the `notificationMessages` property of an item.
    /// 
    /// [Api set: Mailbox 1.3]
    type [<AllowNullLiteral>] NotificationMessages =
        /// <summary>Adds a notification to an item.
        /// 
        /// There are a maximum of 5 notifications per message. Setting more will return a `NumberOfNotificationMessagesExceeded` error.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="key">- A developer-specified key used to reference this notification message.
        ///  Developers can use it to modify this message later. It can't be longer than 32 characters.</param>
        /// <param name="JSONmessage">- A JSON object that contains the notification message to be added to the item.
        /// It contains a `NotificationMessageDetails` object.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`.</param>
        abstract addAsync: key: string * JSONmessage: NotificationMessageDetails * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds a notification to an item.
        /// 
        /// There are a maximum of 5 notifications per message. Setting more will return a `NumberOfNotificationMessagesExceeded` error.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="key">- A developer-specified key used to reference this notification message.
        ///  Developers can use it to modify this message later. It can't be longer than 32 characters.</param>
        /// <param name="JSONmessage">- A JSON object that contains the notification message to be added to the item.
        /// It contains a `NotificationMessageDetails` object.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`.</param>
        abstract addAsync: key: string * JSONmessage: NotificationMessageDetails * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Returns all keys and messages for an item.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`. The `value` property of the result is an array of `NotificationMessageDetails` objects.</param>
        abstract getAllAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<ResizeArray<NotificationMessageDetails>> -> unit) -> unit
        /// <summary>Returns all keys and messages for an item.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`. The `value` property of the result is an array of `NotificationMessageDetails` objects.</param>
        abstract getAllAsync: ?callback: (Office.AsyncResult<ResizeArray<NotificationMessageDetails>> -> unit) -> unit
        /// <summary>Removes a notification message for an item.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="key">- The key for the notification message to remove.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`.</param>
        abstract removeAsync: key: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Removes a notification message for an item.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="key">- The key for the notification message to remove.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`.</param>
        abstract removeAsync: key: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Replaces a notification message that has a given key with another message.
        /// 
        /// If a notification message with the specified key doesn't exist, `replaceAsync` will add the notification.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="key">- The key for the notification message to replace. It can't be longer than 32 characters.</param>
        /// <param name="JSONmessage">- A JSON object that contains the new notification message to replace the existing message. 
        /// It contains a `NotificationMessageDetails` object.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`.</param>
        abstract replaceAsync: key: string * JSONmessage: NotificationMessageDetails * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Replaces a notification message that has a given key with another message.
        /// 
        /// If a notification message with the specified key doesn't exist, `replaceAsync` will add the notification.
        /// 
        /// [Api set: Mailbox 1.3]</summary>
        /// <param name="key">- The key for the notification message to replace. It can't be longer than 32 characters.</param>
        /// <param name="JSONmessage">- A JSON object that contains the new notification message to replace the existing message. 
        /// It contains a `NotificationMessageDetails` object.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`.</param>
        abstract replaceAsync: key: string * JSONmessage: NotificationMessageDetails * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Represents the appointment organizer, even if an alias or a delegate was used to create the appointment. 
    /// This object provides a method to get the organizer value of an appointment in an Outlook add-in.
    /// 
    /// [Api set: Mailbox 1.7]
    type [<AllowNullLiteral>] Organizer =
        /// <summary>Gets the organizer value of an appointment as an {@link Office.EmailAddressDetails | EmailAddressDetails} object
        /// in the `asyncResult.value` property.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        ///  `asyncResult`, which is an `AsyncResult` object. The `value` property of the result is the appointment's organizer value,
        ///  as an `EmailAddressDetails` object.</param>
        abstract getAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<EmailAddressDetails> -> unit) -> unit
        /// <summary>Gets the organizer value of an appointment as an {@link Office.EmailAddressDetails | EmailAddressDetails} object
        /// in the `asyncResult.value` property.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        ///  `asyncResult`, which is an `AsyncResult` object. The `value` property of the result is the appointment's organizer value,
        ///  as an `EmailAddressDetails` object.</param>
        abstract getAsync: ?callback: (Office.AsyncResult<EmailAddressDetails> -> unit) -> unit

    /// Represents a phone number identified in an item. Read mode only.
    /// 
    /// An array of `PhoneNumber` objects containing the phone numbers found in an email message is returned in the `phoneNumbers` property of the
    /// `Entities` object that is returned when you call the `getEntities` method on the selected item.
    type [<AllowNullLiteral>] PhoneNumber =
        /// Gets a string containing a phone number. This string contains only the digits of the telephone number and excludes characters
        /// like parentheses and hyphens, if they exist in the original item.
        abstract phoneString: string with get, set
        /// Gets the text that was identified in an item as a phone number.
        abstract originalPhoneString: string with get, set
        /// Gets a string that identifies the type of phone number: Home, Work, Mobile, Unspecified.
        abstract ``type``: string with get, set

    /// Represents recipients of an item. Compose mode only.
    /// 
    /// [Api set: Mailbox 1.1]
    type [<AllowNullLiteral>] Recipients =
        /// <summary>Adds a recipient list to the existing recipients for an appointment or message.
        /// 
        /// The recipients parameter can be an array of one of the following:
        /// 
        /// - Strings containing SMTP email addresses
        /// 
        /// - {@link Office.EmailUser | EmailUser} objects
        /// 
        /// - {@link Office.EmailAddressDetails | EmailAddressDetails} objects
        /// 
        /// Maximum number that can be added:
        /// 
        /// - Windows: 100 recipients.
        /// **Note**: Can call API repeatedly but the maximum number of recipients in the target field on the item is 500 recipients.
        /// 
        /// - Mac, web browser: 100 recipients
        /// 
        /// - Other: No limit
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="recipients">- The recipients to add to the recipients list.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`. If adding the recipients fails, the `asyncResult.error` property will contain an error code.</param>
        abstract addAsync: recipients: ResizeArray<U3<string, EmailUser, EmailAddressDetails>> * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Adds a recipient list to the existing recipients for an appointment or message.
        /// 
        /// The recipients parameter can be an array of one of the following:
        /// 
        /// - Strings containing SMTP email addresses
        /// 
        /// - {@link Office.EmailUser | EmailUser} objects
        /// 
        /// - {@link Office.EmailAddressDetails | EmailAddressDetails} objects
        /// 
        /// Maximum number that can be added:
        /// 
        /// - Windows: 100 recipients.
        /// **Note**: Can call API repeatedly but the maximum number of recipients in the target field on the item is 500 recipients.
        /// 
        /// - Mac, web browser: 100 recipients
        /// 
        /// - Other: No limit
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="recipients">- The recipients to add to the recipients list.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`. If adding the recipients fails, the `asyncResult.error` property will contain an error code.</param>
        abstract addAsync: recipients: ResizeArray<U3<string, EmailUser, EmailAddressDetails>> * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Gets a recipient list for an appointment or message.
        /// 
        /// When the call completes, the `asyncResult.value` property will contain an array of {@link Office.EmailAddressDetails | EmailAddressDetails}
        /// objects. Collection size limits:
        /// 
        /// - Windows, Mac, web browser: 500 members
        /// 
        /// - Other: No limit
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`. The `value` property of the result is an array of `EmailAddressDetails` objects.</param>
        abstract getAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<ResizeArray<EmailAddressDetails>> -> unit) -> unit
        /// <summary>Gets a recipient list for an appointment or message.
        /// 
        /// When the call completes, the `asyncResult.value` property will contain an array of {@link Office.EmailAddressDetails | EmailAddressDetails}
        /// objects. Collection size limits:
        /// 
        /// - Windows, Mac, web browser: 500 members
        /// 
        /// - Other: No limit
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`. The `value` property of the result is an array of `EmailAddressDetails` objects.</param>
        abstract getAsync: callback: (Office.AsyncResult<ResizeArray<EmailAddressDetails>> -> unit) -> unit
        /// <summary>Sets a recipient list for an appointment or message.
        /// 
        /// The `setAsync` method overwrites the current recipient list.
        /// 
        /// The recipients parameter can be an array of one of the following:
        /// 
        /// - Strings containing SMTP email addresses
        /// 
        /// - {@link Office.EmailUser | EmailUser} objects
        /// 
        /// - {@link Office.EmailAddressDetails | EmailAddressDetails} objects
        /// 
        /// Maximum number that can be set:
        /// 
        /// - Windows, Mac, web browser: 100 recipients
        /// 
        /// - Other: No limit
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="recipients">- The recipients to add to the recipients list.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. If setting the recipients fails the `asyncResult.error` property will contain a code that
        /// indicates any error that occurred while adding the data.</param>
        abstract setAsync: recipients: ResizeArray<U3<string, EmailUser, EmailAddressDetails>> * options: Office.AsyncContextOptions * callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets a recipient list for an appointment or message.
        /// 
        /// The `setAsync` method overwrites the current recipient list.
        /// 
        /// The recipients parameter can be an array of one of the following:
        /// 
        /// - Strings containing SMTP email addresses
        /// 
        /// - {@link Office.EmailUser | EmailUser} objects
        /// 
        /// - {@link Office.EmailAddressDetails | EmailAddressDetails} objects
        /// 
        /// Maximum number that can be set:
        /// 
        /// - Windows, Mac, web browser: 100 recipients
        /// 
        /// - Other: No limit
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="recipients">- The recipients to add to the recipients list.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter of 
        /// type `Office.AsyncResult`. If setting the recipients fails the `asyncResult.error` property will contain a code that
        /// indicates any error that occurred while adding the data.</param>
        abstract setAsync: recipients: ResizeArray<U3<string, EmailUser, EmailAddressDetails>> * callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Provides change status of recipients fields when the `Office.EventType.RecipientsChanged` event is raised.
    /// 
    /// [Api set: Mailbox 1.7]
    type [<AllowNullLiteral>] RecipientsChangedEventArgs =
        /// Gets an object that indicates change state of recipients fields. 
        /// 
        /// [Api set: Mailbox 1.7]
        abstract changedRecipientFields: RecipientsChangedFields with get, set
        /// Gets the type of the event. For details, refer to {@link https://docs.microsoft.com/javascript/api/office/office.eventtype | Office.EventType}.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract ``type``: string with get, set

    /// Represents `RecipientsChangedEventArgs.changedRecipientFields` object.
    /// 
    /// [Api set: Mailbox 1.7]
    type [<AllowNullLiteral>] RecipientsChangedFields =
        /// Gets if recipients in the **bcc** field were changed.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract bcc: bool with get, set
        /// Gets if recipients in the **cc** field were changed.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract cc: bool with get, set
        /// Gets if optional attendees were changed.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract optionalAttendees: bool with get, set
        /// Gets if required attendees were changed.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract requiredAttendees: bool with get, set
        /// Gets if resources were changed.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract resources: bool with get, set
        /// Gets if recipients in the **to** field were changed.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract ``to``: bool with get, set

    /// The `Recurrence` object provides methods to get and set the recurrence pattern of appointments but only get the recurrence pattern of 
    /// meeting requests. 
    /// It will have a dictionary with the following keys: `seriesTime`, `recurrenceType`, `recurrenceProperties`, and `recurrenceTimeZone` (optional).
    /// 
    /// [Api set: Mailbox 1.7]
    type [<AllowNullLiteral>] Recurrence =
        /// Gets or sets the properties of the recurring appointment series.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract recurrenceProperties: RecurrenceProperties option with get, set
        /// Gets or sets the properties of the recurring appointment series.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract recurrenceTimeZone: RecurrenceTimeZone option with get, set
        /// Gets or sets the type of the recurring appointment series.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract recurrenceType: U2<MailboxEnums.RecurrenceType, string> with get, set
        /// The {@link Office.SeriesTime | SeriesTime} object enables you to manage the start and end dates of the recurring appointment series and
        /// the usual start and end times of instances. **This object is not in UTC time.** 
        /// Instead, it is set in the time zone specified by the `recurrenceTimeZone` value or defaulted to the item's time zone.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract seriesTime: SeriesTime with get, set
        /// <summary>Returns the current recurrence object of an appointment series.
        /// 
        /// This method returns the entire `Recurrence` object for the appointment series.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object. The `value` property of the result is a `Recurrence` object.</param>
        abstract getAsync: options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<Recurrence> -> unit) -> unit
        /// <summary>Returns the current recurrence object of an appointment series.
        /// 
        /// This method returns the entire `Recurrence` object for the appointment series.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object. The `value` property of the result is a `Recurrence` object.</param>
        abstract getAsync: ?callback: (Office.AsyncResult<Recurrence> -> unit) -> unit
        /// <summary>Sets the recurrence pattern of an appointment series.
        /// 
        /// **Note**: `setAsync` should only be available for series items and not instance items.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="recurrencePattern">- A recurrence object.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract setAsync: recurrencePattern: Recurrence * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets the recurrence pattern of an appointment series.
        /// 
        /// **Note**: `setAsync` should only be available for series items and not instance items.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="recurrencePattern">- A recurrence object.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter,
        /// `asyncResult`, which is an `Office.AsyncResult` object.</param>
        abstract setAsync: recurrencePattern: Recurrence * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Provides updated recurrence object that raised the `Office.EventType.RecurrenceChanged` event. 
    /// 
    /// [Api set: Mailbox 1.7]
    type [<AllowNullLiteral>] RecurrenceChangedEventArgs =
        /// Gets the updated recurrence object. 
        /// 
        /// [Api set: Mailbox 1.7]
        abstract recurrence: Recurrence with get, set
        /// Gets the type of the event. For details, refer to {@link https://docs.microsoft.com/javascript/api/office/office.eventtype | Office.EventType}.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract ``type``: string with get, set

    /// Represents the properties of the recurrence.
    /// 
    /// [Api set: Mailbox 1.7]
    type [<AllowNullLiteral>] RecurrenceProperties =
        /// Represents the period between instances of the same recurring series.
        abstract interval: float with get, set
        /// Represents the day of the month.
        abstract dayOfMonth: float option with get, set
        /// Represents the day of the week or type of day, for example, weekend day vs weekday.
        abstract dayOfWeek: U2<MailboxEnums.Days, string> option with get, set
        /// Represents the set of days for this recurrence. Valid values are: 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', and 'Sun'.
        abstract days: U2<ResizeArray<MailboxEnums.Days>, ResizeArray<string>> option with get, set
        /// Represents the number of the week in the selected month e.g., 'first' for first week of the month.
        abstract weekNumber: U2<MailboxEnums.WeekNumber, string> option with get, set
        /// Represents the month.
        abstract month: U2<MailboxEnums.Month, string> option with get, set
        /// Represents your chosen first day of the week otherwise the default is the value in the current user's settings. 
        /// Valid values are: 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', and 'Sun'.
        abstract firstDayOfWeek: U2<MailboxEnums.Days, string> option with get, set

    /// Represents the time zone of the recurrence.
    /// 
    /// [Api set: Mailbox 1.7]
    type [<AllowNullLiteral>] RecurrenceTimeZone =
        /// Represents the name of the recurrence time zone.
        abstract name: U2<MailboxEnums.RecurrenceTimeZone, string> with get, set
        /// Integer value representing the difference in minutes between the local time zone and UTC at the date that the meeting series began.
        abstract offset: float option with get, set

    /// A file or item attachment. Used when displaying a reply form.
    type [<AllowNullLiteral>] ReplyFormAttachment =
        /// Indicates the type of attachment. Must be file for a file attachment or item for an item attachment.
        abstract ``type``: string with get, set
        /// A string that contains the name of the attachment, up to 255 characters in length.
        abstract name: string with get, set
        /// Only used if type is set to file. The URI of the location for the file.
        abstract url: string option with get, set
        /// Only used if type is set to file. If true, indicates that the attachment will be shown inline in the message body, and should not be 
        /// displayed in the attachment list.
        abstract inLine: bool option with get, set
        /// Only used if type is set to item. The EWS item id of the attachment. This is a string up to 100 characters.
        abstract itemId: string option with get, set

    /// A ReplyFormData object that contains body or attachment data and a callback function. Used when displaying a reply form.
    type [<AllowNullLiteral>] ReplyFormData =
        /// A string that contains text and HTML and that represents the body of the reply form. The string is limited to 32 KB.
        abstract htmlBody: string option with get, set
        /// An array of {@link Office.ReplyFormAttachment | ReplyFormAttachment} that are either file or item attachments.
        abstract attachments: ResizeArray<ReplyFormAttachment> option with get, set
        /// When the reply display call completes, the function passed in the callback parameter is called with a single parameter, 
        /// `asyncResult`, which is an `Office.AsyncResult` object.
        abstract callback: (Office.AsyncResult<obj option> -> unit) option with get, set
        /// An object literal that contains the following property.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.
        abstract options: Office.AsyncContextOptions option with get, set

    /// The settings created by using the methods of the `RoamingSettings` object are saved per add-in and per user.
    /// That is, they are available only to the add-in that created them, and only from the user's mailbox in which they are saved.
    /// 
    /// While the Outlook add-in API limits access to these settings to only the add-in that created them, these settings should not be considered
    /// secure storage. They can be accessed by Exchange Web Services or Extended MAPI.
    /// They should not be used to store sensitive information such as user credentials or security tokens.
    /// 
    /// The name of a setting is a String, while the value can be a String, Number, Boolean, null, Object, or Array.
    /// 
    /// The `RoamingSettings` object is accessible via the `roamingSettings` property in the `Office.context` namespace.
    /// 
    /// **Important**:
    /// 
    /// - The `RoamingSettings` object is initialized from the persisted storage only when the add-in is first loaded.
    /// For task panes, this means that it is only initialized when the task pane first opens.
    /// If the task pane navigates to another page or reloads the current page, the in-memory object is reset to its initial values, even if
    /// your add-in has persisted changes.
    /// The persisted changes will not be available until the task pane (or item in the case of UI-less add-ins) is closed and reopened.
    /// 
    /// - When set and saved through Outlook on Windows or Mac, these settings are reflected in Outlook on the web only after a browser refresh.
    type [<AllowNullLiteral>] RoamingSettings =
        /// <summary>Retrieves the specified setting.</summary>
        /// <param name="name">- The case-sensitive name of the setting to retrieve.</param>
        abstract get: name: string -> obj option
        /// <summary>Removes the specified setting</summary>
        /// <param name="name">- The case-sensitive name of the setting to remove.</param>
        abstract remove: name: string -> unit
        /// <summary>Saves the settings.
        /// 
        /// Any settings previously saved by an add-in are loaded when it is initialized, so during the lifetime of the session you can just use 
        /// the set and get methods to work with the in-memory copy of the settings property bag. 
        /// When you want to persist the settings so that they are available the next time the add-in is used, use the saveAsync method.</summary>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`.</param>
        abstract saveAsync: ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets or creates the specified setting.
        /// 
        /// The `set` method creates a new setting of the specified name if it does not already exist, or sets an existing setting of the specified name. 
        /// The value is stored in the document as the serialized JSON representation of its data type.
        /// 
        /// A maximum of 32KB is available for the settings of each add-in.
        /// 
        /// Any changes made to settings using the set function will not be saved to the server until the `saveAsync` function is called.</summary>
        /// <param name="name">- The case-sensitive name of the setting to set or create.</param>
        /// <param name="value">- Specifies the value to be stored.</param>
        abstract set: name: string * value: obj option -> unit

    /// The `SeriesTime` object provides methods to get and set the dates and times of appointments in a recurring series and get the dates and times
    /// of meeting requests in a recurring series.
    /// 
    /// [Api set: Mailbox 1.7]
    type [<AllowNullLiteral>] SeriesTime =
        /// Gets the duration in minutes of a usual instance in a recurring appointment series.
        /// 
        /// [Api set: Mailbox 1.7]
        abstract getDuration: unit -> float
        /// Gets the end date of a recurrence pattern in the following
        /// {@link https://www.iso.org/iso-8601-date-and-time-format.html | ISO 8601} date format: "YYYY-MM-DD".
        /// 
        /// [Api set: Mailbox 1.7]
        abstract getEndDate: unit -> string
        /// Gets the end time of a usual appointment or meeting request instance of a recurrence pattern in whichever time zone that the user or 
        /// add-in set the recurrence pattern using the following {@link https://www.iso.org/iso-8601-date-and-time-format.html | ISO 8601} format: 
        /// "THH:mm:ss:mmm".
        /// 
        /// [Api set: Mailbox 1.7]
        abstract getEndTime: unit -> string
        /// Gets the start date of a recurrence pattern in the following
        /// {@link https://www.iso.org/iso-8601-date-and-time-format.html | ISO 8601} date format: "YYYY-MM-DD".
        /// 
        /// [Api set: Mailbox 1.7]
        abstract getStartDate: unit -> string
        /// Gets the start time of a usual appointment instance of a recurrence pattern in whichever time zone that the user/add-in set the
        /// recurrence pattern using the following {@link https://www.iso.org/iso-8601-date-and-time-format.html | ISO 8601} format: "THH:mm:ss:mmm".
        /// 
        /// [Api set: Mailbox 1.7]
        abstract getStartTime: unit -> string
        /// <summary>Sets the duration of all appointments in a recurrence pattern. This will also change the end time of the recurrence pattern.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="minutes">- The length of the appointment in minutes.</param>
        abstract setDuration: minutes: float -> unit
        /// <summary>Sets the end date of a recurring appointment series.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="year">- The year value of the end date.</param>
        /// <param name="month">- The month value of the end date. Valid range is 0-11 where 0 represents the 1st month and 11 represents the 12th month.</param>
        /// <param name="day">- The day value of the end date.</param>
        abstract setEndDate: year: float * month: float * day: float -> unit
        /// <summary>Sets the end date of a recurring appointment series.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="date">- End date of the recurring appointment series represented in the
        /// {@link https://www.iso.org/iso-8601-date-and-time-format.html | ISO 8601} date format: "YYYY-MM-DD".</param>
        abstract setEndDate: date: string -> unit
        /// <summary>Sets the start date of a recurring appointment series.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="year">- The year value of the start date.</param>
        /// <param name="month">- The month value of the start date. Valid range is 0-11 where 0 represents the 1st month and 11 represents the 12th month.</param>
        /// <param name="day">- The day value of the start date.</param>
        abstract setStartDate: year: float * month: float * day: float -> unit
        /// <summary>Sets the start date of a recurring appointment series.
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="date">- Start date of the recurring appointment series represented in the
        /// {@link https://www.iso.org/iso-8601-date-and-time-format.html | ISO 8601} date format: "YYYY-MM-DD".</param>
        abstract setStartDate: date: string -> unit
        /// <summary>Sets the start time of all instances of a recurring appointment series in whichever time zone the recurrence pattern is set 
        /// (the item's time zone is used by default).
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="hours">- The hour value of the start time. Valid range: 0-24.</param>
        /// <param name="minutes">- The minute value of the start time. Valid range: 0-59.</param>
        abstract setStartTime: hours: float * minutes: float -> unit
        /// <summary>Sets the start time of all instances of a recurring appointment series in whichever time zone the recurrence pattern is set 
        /// (the item's time zone is used by default).
        /// 
        /// [Api set: Mailbox 1.7]</summary>
        /// <param name="time">- Start time of all instances represented by standard datetime string format: "THH:mm:ss:mmm".</param>
        abstract setStartTime: time: string -> unit

    /// Represents the properties of an appointment or message in a shared folder.
    /// 
    /// For more information on how this object is used, see the
    /// {@link https://docs.microsoft.com/office/dev/add-ins/outlook/delegate-access | delegate access} article.
    /// 
    /// [Api set: Mailbox 1.8]
    type [<AllowNullLiteral>] SharedProperties =
        /// The email address of the owner of a shared item.
        abstract owner: string with get, set
        /// The REST API's base URL (currently https://outlook.office.com/api).
        /// 
        /// Use with `targetMailbox` to construct the REST operation's URL.
        /// 
        /// Example usage: `targetRestUrl + "/{api_version}/users/" + targetMailbox + "/{REST_operation}"`
        abstract targetRestUrl: string with get, set
        /// The location of the owner's mailbox for the delegate's access. This location may differ based on the Outlook client.
        /// 
        /// Use with `targetRestUrl` to construct the REST operation's URL.
        /// 
        /// Example usage: `targetRestUrl + "/{api_version}/users/" + targetMailbox + "/{REST_operation}"`
        abstract targetMailbox: string with get, set
        /// The permissions that the delegate has on a shared folder.
        abstract delegatePermissions: MailboxEnums.DelegatePermissions with get, set

    /// Provides methods to get and set the subject of an appointment or message in an Outlook add-in.
    /// 
    /// [Api set: Mailbox 1.1]
    type [<AllowNullLiteral>] Subject =
        /// <summary>Gets the subject of an appointment or message.
        /// 
        /// The `getAsync` method starts an asynchronous call to the Exchange server to get the subject of an appointment or message.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`. The `value` property of the result is the subject of the item.</param>
        abstract getAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Gets the subject of an appointment or message.
        /// 
        /// The getAsync method starts an asynchronous call to the Exchange server to get the subject of an appointment or message.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`. The `value` property of the result is the subject of the item.</param>
        abstract getAsync: callback: (Office.AsyncResult<string> -> unit) -> unit
        /// <summary>Sets the subject of an appointment or message.
        /// 
        /// The `setAsync` method starts an asynchronous call to the Exchange server to set the subject of an appointment or message.
        /// Setting the subject overwrites the current subject, but leaves any prefixes, such as "Fwd:" or "Re:" in place.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="subject">- The subject of the appointment or message. The string is limited to 255 characters.</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`. If setting the subject fails, the `asyncResult.error` property will contain an error code.</param>
        abstract setAsync: subject: string * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets the subject of an appointment or message.
        /// 
        /// The `setAsync` method starts an asynchronous call to the Exchange server to set the subject of an appointment or message.
        /// Setting the subject overwrites the current subject, but leaves any prefixes, such as "Fwd:" or "Re:" in place.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="subject">- The subject of the appointment or message. The string is limited to 255 characters.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter
        /// of type `Office.AsyncResult`. If setting the subject fails, the `asyncResult.error` property will contain an error code.</param>
        abstract setAsync: subject: string * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Represents a suggested task identified in an item. Read mode only.
    /// 
    /// The list of tasks suggested in an email message is returned in the `taskSuggestions` property of the {@link Office.Entities | Entities} object
    /// that is returned when the `getEntities` or `getEntitiesByType` method is called on the active item.
    type [<AllowNullLiteral>] TaskSuggestion =
        /// Gets the users that should be assigned a suggested task.
        abstract assignees: ResizeArray<EmailUser> with get, set
        /// Gets the text of an item that was identified as a task suggestion.
        abstract taskString: string with get, set

    /// The `Time` object is returned as the start or end property of an appointment in compose mode.
    /// 
    /// [Api set: Mailbox 1.1]
    type [<AllowNullLiteral>] Time =
        /// <summary>Gets the start or end time of an appointment.
        /// 
        /// The date and time is provided as a `Date` object in the `asyncResult.value` property. The value is in Coordinated Universal Time (UTC).
        /// You can convert the UTC time to the local client time by using the `convertToLocalClientTime` method.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- When the method completes, the function passed in the `callback` parameter is called with a single parameter
        ///  of type `Office.AsyncResult`. The `value` property of the result is a `Date` object.</param>
        abstract getAsync: options: Office.AsyncContextOptions * callback: (Office.AsyncResult<DateTime> -> unit) -> unit
        /// <summary>Gets the start or end time of an appointment.
        /// 
        /// The date and time is provided as a `Date` object in the `asyncResult.value` property. The value is in Coordinated Universal Time (UTC).
        /// You can convert the UTC time to the local client time by using the `convertToLocalClientTime` method.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="callback">- When the method completes, the function passed in the callback parameter is called with a single parameter
        ///  of type `Office.AsyncResult`. The `value` property of the result is a `Date` object.</param>
        abstract getAsync: callback: (Office.AsyncResult<DateTime> -> unit) -> unit
        /// <summary>Sets the start or end time of an appointment.
        /// 
        /// If the `setAsync` method is called on the start property, the `end` property will be adjusted to maintain the duration of the appointment as
        /// previously set. If the `setAsync` method is called on the `end` property, the duration of the appointment will be extended to the new end time.
        /// 
        /// The time must be in UTC; you can get the correct UTC time by using the `convertToUtcClientTime` method.
        /// 
        /// **Important**: In the Windows client, you can't use this function to update the start or end of a recurrence.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="dateTime">- A date-time object in Coordinated Universal Time (UTC).</param>
        /// <param name="options">- An object literal that contains one or more of the following properties.
        /// `asyncContext`: Developers can provide any object they wish to access in the callback method.</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If setting the date and time fails, the `asyncResult.error` property will contain an error code.</param>
        abstract setAsync: dateTime: DateTime * options: Office.AsyncContextOptions * ?callback: (Office.AsyncResult<unit> -> unit) -> unit
        /// <summary>Sets the start or end time of an appointment.
        /// 
        /// If the `setAsync` method is called on the start property, the `end` property will be adjusted to maintain the duration of the appointment as
        /// previously set. If the `setAsync` method is called on the `end` property, the duration of the appointment will be extended to the new end time.
        /// 
        /// The time must be in UTC; you can get the correct UTC time by using the `convertToUtcClientTime` method.
        /// 
        /// **Important**: In the Windows client, you can't use this function to update the start or end of a recurrence.
        /// 
        /// [Api set: Mailbox 1.1]</summary>
        /// <param name="dateTime">- A date-time object in Coordinated Universal Time (UTC).</param>
        /// <param name="callback">- Optional. When the method completes, the function passed in the `callback` parameter is called with a single parameter of
        /// type `Office.AsyncResult`. If setting the date and time fails, the `asyncResult.error` property will contain an error code.</param>
        abstract setAsync: dateTime: DateTime * ?callback: (Office.AsyncResult<unit> -> unit) -> unit

    /// Information about the user associated with the mailbox. This includes their account type, display name, email address, and time zone.
    type [<AllowNullLiteral>] UserProfile =
        /// Gets the account type of the user associated with the mailbox. 
        /// 
        /// **Note**: This member is currently only supported in Outlook 2016 or later on Mac, build 16.9.1212 and greater.
        /// 
        /// [Api set: Mailbox 1.6]
        abstract accountType: string with get, set
        /// Gets the user's display name.
        abstract displayName: string with get, set
        /// Gets the user's SMTP email address.
        abstract emailAddress: string with get, set
        /// Gets the user's time zone in Windows format.
        /// 
        /// The system time zone is usually returned. However, in Outlook on the web, the default time zone in the calendar preferences is returned instead.
        abstract timeZone: string with get, set

    type [<AllowNullLiteral>] IExportsOnReady =
        abstract host: HostType with get, set
        abstract platform: PlatformType with get, set

    type [<AllowNullLiteral>] DialogAddEventHandler =
        abstract message: string with get, set
        abstract origin: string option with get, set

    type [<AllowNullLiteral>] DialogAddEventHandler2 =
        abstract error: float with get, set

    type [<StringEnum>] [<RequireQualifiedAccess>] DocumentGetActiveViewAsyncAsyncResult =
        | Edit
        | Read

    type [<AllowNullLiteral>] TableBindingGetFormatsAsyncAsyncResult =
        abstract cells: obj option with get, set
        abstract format: obj option with get, set

module rec OfficeExtension =

    type [<AllowNullLiteral>] IExports =
        abstract ClientObject: ClientObjectStatic
        abstract ClientRequestContext: ClientRequestContextStatic
        abstract EmbeddedSession: EmbeddedSessionStatic
        abstract ClientResult: ClientResultStatic
        abstract config: IExportsConfig
        abstract Error: ErrorStatic
        abstract ErrorCodes: ErrorCodesStatic
        abstract Promise: Office.IPromiseConstructor
        abstract TrackedObjects: TrackedObjectsStatic
        abstract EventHandlers: EventHandlersStatic
        abstract EventHandlerResult: EventHandlerResultStatic

    /// An abstract proxy object that represents an object in an Office document. 
    /// You create proxy objects from the context (or from other proxy objects), add commands to a queue to act on the object, and then synchronize the 
    /// proxy object state with the document by calling `context.sync()`.
    type [<AllowNullLiteral>] ClientObject =
        /// The request context associated with the object
        abstract context: ClientRequestContext with get, set
        /// Returns a boolean value for whether the corresponding object is a null object. You must call `context.sync()` before reading the 
        /// isNullObject property.
        abstract isNullObject: bool with get, set

    /// An abstract proxy object that represents an object in an Office document. 
    /// You create proxy objects from the context (or from other proxy objects), add commands to a queue to act on the object, and then synchronize the 
    /// proxy object state with the document by calling `context.sync()`.
    type [<AllowNullLiteral>] ClientObjectStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> ClientObject

    /// Specifies which properties of an object should be loaded. This load happens when the sync() method is executed.
    /// This synchronizes the states between Office objects and corresponding JavaScript proxy objects.
    type [<AllowNullLiteral>] LoadOption =
        /// A comma-delimited string, or array of strings, that specifies the properties to load.
        abstract select: U2<string, ResizeArray<string>> option with get, set
        /// A comma-delimited string, or array of strings, that specifies the navigation properties to load.
        abstract expand: U2<string, ResizeArray<string>> option with get, set
        /// Only usable on collection types. Specifies the maximum number of collection items that can be included in the result.
        abstract top: float option with get, set
        /// Only usable on collection types. Specifies the number of items in the collection that are to be skipped and not included in the result. 
        /// If top is specified, the result set will start after skipping the specified number of items.
        abstract skip: float option with get, set

    /// Provides an option for suppressing an error when the object that is used to set multiple properties tries to set read-only properties.
    type [<AllowNullLiteral>] UpdateOptions =
        /// Throw an error if the passed-in property list includes read-only properties (default = true).
        abstract throwOnReadOnly: bool option with get, set

    /// Additional options passed into `{Host}.run(...)`.
    type [<AllowNullLiteral>] RunOptions<'T> =
        /// The URL of the remote workbook and the request headers to be sent.
        abstract session: U2<RequestUrlAndHeaderInfo, 'T> option with get, set
        /// A previously-created context, or API object, or array of objects. 
        /// The batch will use the same RequestContext as the passed-in object, which means that any changes applied to the object will be picked up 
        /// by `context.sync()`.
        abstract previousObjects: U3<ClientObject, ResizeArray<ClientObject>, ClientRequestContext> option with get, set

    /// Contains debug information about the request context.
    type [<AllowNullLiteral>] RequestContextDebugInfo =
        /// The statements to be executed in the host.
        /// 
        /// These statements may not match the code exactly as written, but will be a close approximation.
        abstract pendingStatements: ResizeArray<string> with get, set

    /// An abstract RequestContext object that facilitates requests to the host Office application. 
    /// The `Excel.run` and `Word.run` methods provide a request context.
    type [<AllowNullLiteral>] ClientRequestContext =
        /// Collection of objects that are tracked for automatic adjustments based on surrounding changes in the document.
        abstract trackedObjects: TrackedObjects with get, set
        /// Request headers
        abstract requestHeaders: ClientRequestContextRequestHeaders with get, set
        /// <summary>Queues up a command to load the specified properties of the object. You must call `context.sync()` before reading the properties.</summary>
        /// <param name="object">The object whose properties are loaded.</param>
        /// <param name="option">A comma-delimited string, or array of strings, that specifies the properties to load, or an 
        /// {@link OfficeExtension.LoadOption} object.</param>
        abstract load: ``object``: ClientObject * ?option: U3<string, ResizeArray<string>, LoadOption> -> unit
        /// <summary>Queues up a command to recursively load the specified properties of the object and its navigation properties.
        /// 
        /// You must call `context.sync()` before reading the properties.</summary>
        /// <param name="object">The object to be loaded.</param>
        /// <param name="options">The key-value pairing of load options for the types, such as 
        /// `{ "Workbook": "worksheets,tables",  "Worksheet": "tables",  "Tables": "name" }`</param>
        /// <param name="maxDepth">The maximum recursive depth.</param>
        abstract loadRecursive: ``object``: ClientObject * options: ClientRequestContextLoadRecursiveOptions * ?maxDepth: float -> unit
        /// Adds a trace message to the queue. If the promise returned by `context.sync()` is rejected due to an error, this adds a ".traceMessages" 
        /// array to the OfficeExtension.Error object, containing all trace messages that were executed. 
        /// These messages can help you monitor the program execution sequence and detect the cause of the error.
        abstract trace: message: string -> unit
        /// Synchronizes the state between JavaScript proxy objects and the Office document, by executing instructions queued on the request context 
        /// and retrieving properties of loaded Office objects for use in your code. 
        /// This method returns a promise, which is resolved when the synchronization is complete.
        abstract sync: ?passThroughValue: 'T -> Promise<'T>
        /// Debug information
        abstract debugInfo: RequestContextDebugInfo

    type [<AllowNullLiteral>] ClientRequestContextLoadRecursiveOptions =
        [<Emit "$0[$1]{{=$2}}">] abstract Item: typeName: string -> U3<string, ResizeArray<string>, LoadOption> with get, set

    /// An abstract RequestContext object that facilitates requests to the host Office application. 
    /// The `Excel.run` and `Word.run` methods provide a request context.
    type [<AllowNullLiteral>] ClientRequestContextStatic =
        [<Emit "new $0($1...)">] abstract Create: ?url: string -> ClientRequestContext

    type [<AllowNullLiteral>] EmbeddedOptions =
        abstract sessionKey: string option with get, set
        abstract container: HTMLElement option with get, set
        abstract id: string option with get, set
        abstract timeoutInMilliseconds: float option with get, set
        abstract height: string option with get, set
        abstract width: string option with get, set

    type [<AllowNullLiteral>] EmbeddedSession =
        abstract init: unit -> Promise<obj option>

    type [<AllowNullLiteral>] EmbeddedSessionStatic =
        [<Emit "new $0($1...)">] abstract Create: url: string * ?options: EmbeddedOptions -> EmbeddedSession

    /// Contains the result for methods that return primitive types. The object's value property is retrieved from the document after `context.sync()` is invoked.
    type [<AllowNullLiteral>] ClientResult<'T> =
        /// The value of the result that is retrieved from the document after `context.sync()` is invoked.
        abstract value: 'T with get, set

    /// Contains the result for methods that return primitive types. The object's value property is retrieved from the document after `context.sync()` is invoked.
    type [<AllowNullLiteral>] ClientResultStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> ClientResult<'T>

    /// Provides information about an error.
    type [<AllowNullLiteral>] DebugInfo =
        /// Error code string, such as "InvalidArgument".
        abstract code: string with get, set
        /// The error message passed through from the host Office application.
        abstract message: string with get, set
        /// Inner error, if applicable.
        abstract innerError: U2<DebugInfo, string> option with get, set
        /// The object type and property or method name (or similar information), if available.
        abstract errorLocation: string option with get, set
        /// The statement that caused the error, if available.
        /// 
        /// This statement will never contain any potentially-sensitive data and may not match the code exactly as written, 
        /// but will be a close approximation.
        abstract statements: string option with get, set
        /// The statements that closely precede and follow the statement that caused the error, if available.
        /// 
        /// These statements will never contain any potentially-sensitive data and may not match the code exactly as written, 
        /// but will be a close approximation.
        abstract surroundingStatements: ResizeArray<string> option with get, set
        /// All statements in the batch request (including any potentially-sensitive information that was specified in the request), if available.
        /// 
        /// These statements may not match the code exactly as written, but will be a close approximation.
        abstract fullStatements: ResizeArray<string> option with get, set

    /// The error object returned by `context.sync()`, if a promise is rejected due to an error while processing the request.
    type [<AllowNullLiteral>] Error =
        /// Error name: "OfficeExtension.Error".
        abstract name: string with get, set
        /// The error message passed through from the host Office application.
        abstract message: string with get, set
        /// Stack trace, if applicable.
        abstract stack: string with get, set
        /// Error code string, such as "InvalidArgument".
        abstract code: string with get, set
        /// Trace messages (if any) that were added via a `context.trace()` invocation before calling `context.sync()`. 
        /// If there was an error, this contains all trace messages that were executed before the error occurred. 
        /// These messages can help you monitor the program execution sequence and detect the case of the error.
        abstract traceMessages: ResizeArray<string> with get, set
        /// Debug info (useful for detailed logging of the error, i.e., via `JSON.stringify(...)`).
        abstract debugInfo: DebugInfo with get, set
        /// Inner error, if applicable.
        abstract innerError: Error with get, set

    /// The error object returned by `context.sync()`, if a promise is rejected due to an error while processing the request.
    type [<AllowNullLiteral>] ErrorStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> Error

    type [<AllowNullLiteral>] ErrorCodes =
        interface end

    type [<AllowNullLiteral>] ErrorCodesStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> ErrorCodes
        abstract accessDenied: string with get, set
        abstract generalException: string with get, set
        abstract activityLimitReached: string with get, set
        abstract invalidObjectPath: string with get, set
        abstract propertyNotLoaded: string with get, set
        abstract valueNotLoaded: string with get, set
        abstract invalidRequestContext: string with get, set
        abstract invalidArgument: string with get, set
        abstract runMustReturnPromise: string with get, set
        abstract cannotRegisterEvent: string with get, set
        abstract apiNotFound: string with get, set
        abstract connectionFailure: string with get, set

    type IPromise<'T> =
        Promise<'T>

    /// Collection of tracked objects, contained within a request context. See "context.trackedObjects" for more information.
    type [<AllowNullLiteral>] TrackedObjects =
        /// Track a new object for automatic adjustment based on surrounding changes in the document. Only some object types require this. 
        /// If you are using an object across ".sync" calls and outside the sequential execution of a ".run" batch, 
        /// and get an "InvalidObjectPath" error when setting a property or invoking a method on the object, you needed to have added the object 
        /// to the tracked object collection when the object was first created. 
        /// 
        /// This method also has the following signature: 
        /// 
        /// `add(objects: ClientObject[]): void;` Where objects is an array of objects to be tracked.
        abstract add: ``object``: ClientObject -> unit
        /// Track a set of objects  for automatic adjustment based on surrounding changes in the document. Only some object types require this. 
        /// If you are using an object across ".sync" calls and outside the sequential execution of a ".run" batch, 
        /// and get an "InvalidObjectPath" error when setting a property or invoking a method on the object, you needed to have added the object 
        /// to the tracked object collection when the object was first created.
        abstract add: objects: ResizeArray<ClientObject> -> unit
        /// Release the memory associated with an object that was previously added to this collection. 
        /// Having many tracked objects slows down the host application, so please remember to free any objects you add, once you're done using them. 
        /// You will need to call `context.sync()` before the memory release takes effect.
        /// 
        /// This method also has the following signature: 
        /// 
        /// `remove(objects: ClientObject[]): void;` Where objects is an array of objects to be removed.
        abstract remove: ``object``: ClientObject -> unit
        /// Release the memory associated with an object that was previously added to this collection. 
        /// Having many tracked objects slows down the host application, so please remember to free any objects you add, once you're done using them. 
        /// You will need to call `context.sync()` before the memory release takes effect.
        abstract remove: objects: ResizeArray<ClientObject> -> unit

    /// Collection of tracked objects, contained within a request context. See "context.trackedObjects" for more information.
    type [<AllowNullLiteral>] TrackedObjectsStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> TrackedObjects

    type [<AllowNullLiteral>] EventHandlers<'T> =
        /// <summary>Adds a function to be called when the event is triggered.</summary>
        /// <param name="handler">A promise-based function that takes in any relevant event arguments.</param>
        abstract add: handler: ('T -> Promise<obj option>) -> EventHandlerResult<'T>
        /// <summary>Removes the specified function from the event handler list so that it will not be called on subsequent events. 
        /// 
        /// **Note**: The same {@link OfficeExtension.ClientRequestContext | RequestContext} object that the handler was added in must be used when removing the handler. 
        /// More information can be found in {@link https://docs.microsoft.com/office/dev/add-ins/excel/excel-add-ins-events#remove-an-event-handler | Remove an event handler}.</summary>
        /// <param name="handler">A reference to a function previously provided to the `add` method as an event handler.</param>
        abstract remove: handler: ('T -> Promise<obj option>) -> unit

    type [<AllowNullLiteral>] EventHandlersStatic =
        [<Emit "new $0($1...)">] abstract Create: context: ClientRequestContext * parentObject: ClientObject * name: string * eventInfo: EventInfo<'T> -> EventHandlers<'T>

    type [<AllowNullLiteral>] EventHandlerResult<'T> =
        /// The request context associated with the object
        abstract context: ClientRequestContext with get, set
        abstract remove: unit -> unit

    type [<AllowNullLiteral>] EventHandlerResultStatic =
        [<Emit "new $0($1...)">] abstract Create: context: ClientRequestContext * handlers: EventHandlers<'T> * handler: ('T -> Promise<obj option>) -> EventHandlerResult<'T>

    type [<AllowNullLiteral>] EventInfo<'T> =
        abstract registerFunc: ((obj option -> unit) -> Promise<obj option>) with get, set
        abstract unregisterFunc: ((obj option -> unit) -> Promise<obj option>) with get, set
        abstract eventArgsTransformFunc: (obj option -> Promise<'T>) with get, set

    /// Request URL and headers
    type [<AllowNullLiteral>] RequestUrlAndHeaderInfo =
        /// Request URL
        abstract url: string with get, set
        /// Request headers
        abstract headers: ClientRequestContextRequestHeaders option with get, set

    type [<AllowNullLiteral>] IExportsConfig =
        /// Determines whether to log additional error information upon failure.
        /// 
        /// When this property is set to true, the error object will include a "debugInfo.fullStatements" property that lists all statements in the 
        /// batch request, including all statements that precede and follow the point of failure.
        /// 
        /// Setting this property to true will negatively impact performance and will log all statements in the batch request, including any statements 
        /// that may contain potentially-sensitive data.
        /// It is recommended that you only set this property to true during debugging and that you never log the value of 
        /// error.debugInfo.fullStatements to an external database or analytics service.
        abstract extendedErrorLogging: bool with get, set

    type [<AllowNullLiteral>] ClientRequestContextRequestHeaders =
        [<Emit "$0[$1]{{=$2}}">] abstract Item: name: string -> string with get, set

module rec OfficeCore =

    type [<AllowNullLiteral>] IExports =
        abstract RequestContext: RequestContextStatic

    type [<AllowNullLiteral>] RequestContext =
        inherit OfficeExtension.ClientRequestContext

    type [<AllowNullLiteral>] RequestContextStatic =
        [<Emit "new $0($1...)">] abstract Create: ?url: U3<string, OfficeExtension.RequestUrlAndHeaderInfo, obj option> -> RequestContext

module rec Visio =

    type [<AllowNullLiteral>] IExports =
        abstract Application: ApplicationStatic
        abstract Document: DocumentStatic
        abstract DocumentView: DocumentViewStatic
        abstract Page: PageStatic
        abstract PageView: PageViewStatic
        abstract PageCollection: PageCollectionStatic
        abstract ShapeCollection: ShapeCollectionStatic
        abstract Shape: ShapeStatic
        abstract ShapeView: ShapeViewStatic
        abstract ShapeDataItemCollection: ShapeDataItemCollectionStatic
        abstract ShapeDataItem: ShapeDataItemStatic
        abstract HyperlinkCollection: HyperlinkCollectionStatic
        abstract Hyperlink: HyperlinkStatic
        abstract CommentCollection: CommentCollectionStatic
        abstract Comment: CommentStatic
        abstract Selection: SelectionStatic
        abstract RequestContext: RequestContextStatic
        /// <summary>Executes a batch script that performs actions on the Visio object model, using a new request context. When the promise is resolved, any tracked objects that were automatically allocated during execution will be released.</summary>
        /// <param name="batch">- A function that takes in an Visio.RequestContext and returns a promise (typically, just the result of "context.sync()"). The context parameter facilitates requests to the Visio application. Since the Office add-in and the Visio application run in two different processes, the request context is required to get access to the Visio object model from the add-in.</param>
        abstract run: batch: (Visio.RequestContext -> Promise<'T>) -> Promise<'T>
        /// <summary>Executes a batch script that performs actions on the Visio object model, using the request context of a previously-created API object.</summary>
        /// <param name="object">- A previously-created API object. The batch will use the same request context as the passed-in object, which means that any changes applied to the object will be picked up by "context.sync()".</param>
        /// <param name="batch">- A function that takes in an Visio.RequestContext and returns a promise (typically, just the result of "context.sync()"). When the promise is resolved, any tracked objects that were automatically allocated during execution will be released.</param>
        abstract run: ``object``: U2<OfficeExtension.ClientObject, OfficeExtension.EmbeddedSession> * batch: (Visio.RequestContext -> Promise<'T>) -> Promise<'T>
        /// <summary>Executes a batch script that performs actions on the Visio object model, using the request context of previously-created API objects.</summary>
        /// <param name="objects">- An array of previously-created API objects. The array will be validated to make sure that all of the objects share the same context. The batch will use this shared request context, which means that any changes applied to these objects will be picked up by "context.sync()".</param>
        /// <param name="batch">- A function that takes in a Visio.RequestContext and returns a promise (typically, just the result of "context.sync()"). When the promise is resolved, any tracked objects that were automatically allocated during execution will be released.</param>
        abstract run: objects: ResizeArray<OfficeExtension.ClientObject> * batch: (Visio.RequestContext -> Promise<'T>) -> Promise<'T>
        /// <summary>Executes a batch script that performs actions on the Visio object model, using the RequestContext of a previously-created object. When the promise is resolved, any tracked objects that were automatically allocated during execution will be released.</summary>
        /// <param name="contextObject">- A previously-created Visio.RequestContext. This context will get re-used by the batch function (instead of having a new context created). This means that the batch will be able to pick up changes made to existing API objects, if those objects were derived from this same context.</param>
        /// <param name="batch">- A function that takes in a RequestContext and returns a promise (typically, just the result of "context.sync()"). The context parameter facilitates requests to the Visio application. Since the Office add-in and the Visio application run in two different processes, the RequestContext is required to get access to the Visio object model from the add-in.</param>
        abstract run: contextObject: OfficeExtension.ClientRequestContext * batch: (Visio.RequestContext -> Promise<'T>) -> Promise<'T>

    /// Provides information about the shape that raised the ShapeMouseEnter event.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeMouseEnterEventArgs =
        /// Gets the name of the page which has the shape object that raised the ShapeMouseEnter event.
        /// 
        /// [Api set:  1.1]
        abstract pageName: string with get, set
        /// Gets the name of the shape object that raised the ShapeMouseEnter event.
        /// 
        /// [Api set:  1.1]
        abstract shapeName: string with get, set

    /// Provides information about the shape that raised the ShapeMouseLeave event.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeMouseLeaveEventArgs =
        /// Gets the name of the page which has the shape object that raised the ShapeMouseLeave event.
        /// 
        /// [Api set:  1.1]
        abstract pageName: string with get, set
        /// Gets the name of the shape object that raised the ShapeMouseLeave event.
        /// 
        /// [Api set:  1.1]
        abstract shapeName: string with get, set

    /// Provides information about the page that raised the PageLoadComplete event.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] PageLoadCompleteEventArgs =
        /// Gets the name of the page that raised the PageLoad event.
        /// 
        /// [Api set:  1.1]
        abstract pageName: string with get, set
        /// Gets the success or failure of the PageLoadComplete event.
        /// 
        /// [Api set:  1.1]
        abstract success: bool with get, set

    /// Provides information about the document that raised the DataRefreshComplete event.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] DataRefreshCompleteEventArgs =
        /// Gets the document object that raised the DataRefreshComplete event.
        /// 
        /// [Api set:  1.1]
        abstract document: Visio.Document with get, set
        /// Gets the success or failure of the DataRefreshComplete event.
        /// 
        /// [Api set:  1.1]
        abstract success: bool with get, set

    /// Provides information about the shape collection that raised the SelectionChanged event.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] SelectionChangedEventArgs =
        /// Gets the name of the page which has the ShapeCollection object that raised the SelectionChanged event.
        /// 
        /// [Api set:  1.1]
        abstract pageName: string with get, set
        /// Gets the array of shape names that raised the SelectionChanged event.
        /// 
        /// [Api set:  1.1]
        abstract shapeNames: ResizeArray<string> with get, set

    /// Provides information about the success or failure of the DocumentLoadComplete event.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] DocumentLoadCompleteEventArgs =
        /// Gets the success or failure of the DocumentLoadComplete event.
        /// 
        /// [Api set:  1.1]
        abstract success: bool with get, set

    /// Provides information about the page that raised the PageRenderComplete event.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] PageRenderCompleteEventArgs =
        /// Gets the name of the page that raised the PageLoad event.
        /// 
        /// [Api set:  1.1]
        abstract pageName: string with get, set
        /// Gets the success/failure of the PageRender event.
        /// 
        /// [Api set:  1.1]
        abstract success: bool with get, set

    /// Represents the Application.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] Application =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Show or hide the iFrame application borders.
        /// 
        /// [Api set:  1.1]
        abstract showBorders: bool with get, set
        /// Show or hide the standard toolbars.
        /// 
        /// [Api set:  1.1]
        abstract showToolbars: bool with get, set
        /// <summary>Sets multiple properties of an object at the same time. You can pass either a plain object with the appropriate properties, or another API object of the same type.</summary>
        /// <param name="properties">A JavaScript object with properties that are structured isomorphically to the properties of the object on which the method is called.</param>
        /// <param name="options">Provides an option to suppress errors if the properties object tries to set any read-only properties.</param>
        abstract set: properties: Interfaces.ApplicationUpdateData * ?options: OfficeExtension.UpdateOptions -> unit
        /// Sets multiple properties on the object at the same time, based on an existing loaded object.
        abstract set: properties: Visio.Application -> unit
        /// <summary>Sets the visibility of a specific toolbar in the application.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="id">The type of the Toolbar</param>
        /// <param name="show">Whether the toolbar is visibile or not.</param>
        abstract showToolbar: id: Visio.ToolBarType * show: bool -> unit
        /// <summary>Sets the visibility of a specific toolbar in the application.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="id">The type of the Toolbar</param>
        /// <param name="show">Whether the toolbar is visibile or not.</param>
        abstract showToolbar: id: ApplicationShowToolbarId * show: bool -> unit
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: Visio.Interfaces.ApplicationLoadOptions -> Visio.Application
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.Application
        abstract load: ?option: ApplicationLoadOption -> Visio.Application
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.Application object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.ApplicationData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.ApplicationData

    type [<StringEnum>] [<RequireQualifiedAccess>] ApplicationShowToolbarId =
        | [<CompiledName "CommandBar">] CommandBar
        | [<CompiledName "PageNavigationBar">] PageNavigationBar
        | [<CompiledName "StatusBar">] StatusBar

    type [<AllowNullLiteral>] ApplicationLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the Application.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ApplicationStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> Application

    /// Represents the Document class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] Document =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Represents a Visio application instance that contains this document. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract application: Visio.Application
        /// Represents a collection of pages associated with the document. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract pages: Visio.PageCollection
        /// Returns the DocumentView object. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract view: Visio.DocumentView
        /// <summary>Sets multiple properties of an object at the same time. You can pass either a plain object with the appropriate properties, or another API object of the same type.</summary>
        /// <param name="properties">A JavaScript object with properties that are structured isomorphically to the properties of the object on which the method is called.</param>
        /// <param name="options">Provides an option to suppress errors if the properties object tries to set any read-only properties.</param>
        abstract set: properties: Interfaces.DocumentUpdateData * ?options: OfficeExtension.UpdateOptions -> unit
        /// Sets multiple properties on the object at the same time, based on an existing loaded object.
        abstract set: properties: Visio.Document -> unit
        /// Returns the Active Page of the document.
        /// 
        /// [Api set:  1.1]
        abstract getActivePage: unit -> Visio.Page
        /// <summary>Set the Active Page of the document.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="PageName">Name of the page</param>
        abstract setActivePage: PageName: string -> unit
        /// Triggers the refresh of the data in the Diagram, for all pages.
        /// 
        /// [Api set:  1.1]
        abstract startDataRefresh: unit -> unit
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: Visio.Interfaces.DocumentLoadOptions -> Visio.Document
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.Document
        abstract load: ?option: DocumentLoadOption -> Visio.Document
        /// Occurs when the data is refreshed in the diagram.
        /// 
        /// [Api set:  1.1]
        abstract onDataRefreshComplete: OfficeExtension.EventHandlers<Visio.DataRefreshCompleteEventArgs>
        /// Occurs when the Document is loaded, refreshed, or changed.
        /// 
        /// [Api set:  1.1]
        abstract onDocumentLoadComplete: OfficeExtension.EventHandlers<Visio.DocumentLoadCompleteEventArgs>
        /// Occurs when the page is finished loading.
        /// 
        /// [Api set:  1.1]
        abstract onPageLoadComplete: OfficeExtension.EventHandlers<Visio.PageLoadCompleteEventArgs>
        /// Occurs when the current selection of shapes changes.
        /// 
        /// [Api set:  1.1]
        abstract onSelectionChanged: OfficeExtension.EventHandlers<Visio.SelectionChangedEventArgs>
        /// Occurs when the user moves the mouse pointer into the bounding box of a shape.
        /// 
        /// [Api set:  1.1]
        abstract onShapeMouseEnter: OfficeExtension.EventHandlers<Visio.ShapeMouseEnterEventArgs>
        /// Occurs when the user moves the mouse out of the bounding box of a shape.
        /// 
        /// [Api set:  1.1]
        abstract onShapeMouseLeave: OfficeExtension.EventHandlers<Visio.ShapeMouseLeaveEventArgs>
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.Document object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.DocumentData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.DocumentData

    type [<AllowNullLiteral>] DocumentLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the Document class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] DocumentStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> Document

    /// Represents the DocumentView class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] DocumentView =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Disable Hyperlinks.
        /// 
        /// [Api set:  1.1]
        abstract disableHyperlinks: bool with get, set
        /// Disable Pan.
        /// 
        /// [Api set:  1.1]
        abstract disablePan: bool with get, set
        /// Disable PanZoomWindow.
        /// 
        /// [Api set:  1.1]
        abstract disablePanZoomWindow: bool with get, set
        /// Disable Zoom.
        /// 
        /// [Api set:  1.1]
        abstract disableZoom: bool with get, set
        /// Hide Diagram Boundary.
        /// 
        /// [Api set:  1.1]
        abstract hideDiagramBoundary: bool with get, set
        /// <summary>Sets multiple properties of an object at the same time. You can pass either a plain object with the appropriate properties, or another API object of the same type.</summary>
        /// <param name="properties">A JavaScript object with properties that are structured isomorphically to the properties of the object on which the method is called.</param>
        /// <param name="options">Provides an option to suppress errors if the properties object tries to set any read-only properties.</param>
        abstract set: properties: Interfaces.DocumentViewUpdateData * ?options: OfficeExtension.UpdateOptions -> unit
        /// Sets multiple properties on the object at the same time, based on an existing loaded object.
        abstract set: properties: Visio.DocumentView -> unit
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: Visio.Interfaces.DocumentViewLoadOptions -> Visio.DocumentView
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.DocumentView
        abstract load: ?option: DocumentViewLoadOption -> Visio.DocumentView
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.DocumentView object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.DocumentViewData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.DocumentViewData

    type [<AllowNullLiteral>] DocumentViewLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the DocumentView class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] DocumentViewStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> DocumentView

    /// Represents the Page class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] Page =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// All shapes in the Page, including subshapes. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract allShapes: Visio.ShapeCollection
        /// Returns the Comments Collection.  Read-only.
        /// 
        /// [Api set:  1.1]
        abstract comments: Visio.CommentCollection
        /// All top-level shapes in the Page.Read-only.
        /// 
        /// [Api set:  1.1]
        abstract shapes: Visio.ShapeCollection
        /// Returns the view of the page. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract view: Visio.PageView
        /// Returns the height of the page. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract height: float
        /// Index of the Page. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract index: float
        /// Whether the page is a background page or not. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract isBackground: bool
        /// Page name. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract name: string
        /// Returns the width of the page. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract width: float
        /// <summary>Sets multiple properties of an object at the same time. You can pass either a plain object with the appropriate properties, or another API object of the same type.</summary>
        /// <param name="properties">A JavaScript object with properties that are structured isomorphically to the properties of the object on which the method is called.</param>
        /// <param name="options">Provides an option to suppress errors if the properties object tries to set any read-only properties.</param>
        abstract set: properties: Interfaces.PageUpdateData * ?options: OfficeExtension.UpdateOptions -> unit
        /// Sets multiple properties on the object at the same time, based on an existing loaded object.
        abstract set: properties: Visio.Page -> unit
        /// Set the page as Active Page of the document.
        /// 
        /// [Api set:  1.1]
        abstract activate: unit -> unit
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: Visio.Interfaces.PageLoadOptions -> Visio.Page
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.Page
        abstract load: ?option: PageLoadOption -> Visio.Page
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.Page object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.PageData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.PageData

    type [<AllowNullLiteral>] PageLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the Page class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] PageStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> Page

    /// Represents the PageView class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] PageView =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Get and set Page's Zoom level. The value can be between 10 and 400 and denotes the percentage of zoom.
        /// 
        /// [Api set:  1.1]
        abstract zoom: float with get, set
        /// <summary>Sets multiple properties of an object at the same time. You can pass either a plain object with the appropriate properties, or another API object of the same type.</summary>
        /// <param name="properties">A JavaScript object with properties that are structured isomorphically to the properties of the object on which the method is called.</param>
        /// <param name="options">Provides an option to suppress errors if the properties object tries to set any read-only properties.</param>
        abstract set: properties: Interfaces.PageViewUpdateData * ?options: OfficeExtension.UpdateOptions -> unit
        /// Sets multiple properties on the object at the same time, based on an existing loaded object.
        abstract set: properties: Visio.PageView -> unit
        /// <summary>Pans the Visio drawing to place the specified shape in the center of the view.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="ShapeId">ShapeId to be seen in the center.</param>
        abstract centerViewportOnShape: ShapeId: float -> unit
        /// Fit Page to current window.
        /// 
        /// [Api set:  1.1]
        abstract fitToWindow: unit -> unit
        /// Returns the position object that specifies the position of the page in the view.
        /// 
        /// [Api set:  1.1]
        abstract getPosition: unit -> OfficeExtension.ClientResult<Visio.Position>
        /// Represents the Selection in the page.
        /// 
        /// [Api set:  1.1]
        abstract getSelection: unit -> Visio.Selection
        /// <summary>To check if the shape is in view of the page or not.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="Shape">Shape to be checked.</param>
        abstract isShapeInViewport: Shape: Visio.Shape -> OfficeExtension.ClientResult<bool>
        /// <summary>Sets the position of the page in the view.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="Position">Position object that specifies the new position of the page in the view.</param>
        abstract setPosition: Position: Visio.Position -> unit
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: Visio.Interfaces.PageViewLoadOptions -> Visio.PageView
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.PageView
        abstract load: ?option: PageViewLoadOption -> Visio.PageView
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.PageView object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.PageViewData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.PageViewData

    type [<AllowNullLiteral>] PageViewLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the PageView class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] PageViewStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> PageView

    /// Represents a collection of Page objects that are part of the document.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] PageCollection =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Gets the loaded child items in this collection.
        abstract items: ResizeArray<Visio.Page>
        /// Gets the number of pages in the collection.
        /// 
        /// [Api set:  1.1]
        abstract getCount: unit -> OfficeExtension.ClientResult<float>
        /// <summary>Gets a page using its key (name or Id).
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="key">Key is the name or Id of the page to be retrieved.</param>
        abstract getItem: key: U2<float, string> -> Visio.Page
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: obj -> Visio.PageCollection
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.PageCollection
        abstract load: ?option: OfficeExtension.LoadOption -> Visio.PageCollection
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original `Visio.PageCollection` object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.PageCollectionData`) that contains an "items" array with shallow copies of any loaded properties from the collection's items.
        abstract toJSON: unit -> Visio.Interfaces.PageCollectionData

    /// Represents a collection of Page objects that are part of the document.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] PageCollectionStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> PageCollection

    /// Represents the Shape Collection.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeCollection =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Gets the loaded child items in this collection.
        abstract items: ResizeArray<Visio.Shape>
        /// Gets the number of Shapes in the collection.
        /// 
        /// [Api set:  1.1]
        abstract getCount: unit -> OfficeExtension.ClientResult<float>
        /// <summary>Gets a Shape using its key (name or Index).
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="key">Key is the Name or Index of the shape to be retrieved.</param>
        abstract getItem: key: U2<float, string> -> Visio.Shape
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: obj -> Visio.ShapeCollection
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.ShapeCollection
        abstract load: ?option: OfficeExtension.LoadOption -> Visio.ShapeCollection
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original `Visio.ShapeCollection` object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.ShapeCollectionData`) that contains an "items" array with shallow copies of any loaded properties from the collection's items.
        abstract toJSON: unit -> Visio.Interfaces.ShapeCollectionData

    /// Represents the Shape Collection.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeCollectionStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> ShapeCollection

    /// Represents the Shape class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] Shape =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Returns the Comments Collection. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract comments: Visio.CommentCollection
        /// Returns the Hyperlinks collection for a Shape object. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract hyperlinks: Visio.HyperlinkCollection
        /// Returns the Shape's Data Section. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract shapeDataItems: Visio.ShapeDataItemCollection
        /// Gets SubShape Collection. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract subShapes: Visio.ShapeCollection
        /// Returns the view of the shape. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract view: Visio.ShapeView
        /// Shape's identifier. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract id: float
        /// Shape's name. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract name: string
        /// Returns true, if shape is selected. User can set true to select the shape explicitly.
        /// 
        /// [Api set:  1.1]
        abstract select: bool with get, set
        /// Shape's text. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract text: string
        /// <summary>Sets multiple properties of an object at the same time. You can pass either a plain object with the appropriate properties, or another API object of the same type.</summary>
        /// <param name="properties">A JavaScript object with properties that are structured isomorphically to the properties of the object on which the method is called.</param>
        /// <param name="options">Provides an option to suppress errors if the properties object tries to set any read-only properties.</param>
        abstract set: properties: Interfaces.ShapeUpdateData * ?options: OfficeExtension.UpdateOptions -> unit
        /// Sets multiple properties on the object at the same time, based on an existing loaded object.
        abstract set: properties: Visio.Shape -> unit
        /// Returns the BoundingBox object that specifies bounding box of the shape.
        /// 
        /// [Api set:  1.1]
        abstract getBounds: unit -> OfficeExtension.ClientResult<Visio.BoundingBox>
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: Visio.Interfaces.ShapeLoadOptions -> Visio.Shape
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.Shape
        abstract load: ?option: ShapeLoadOption -> Visio.Shape
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.Shape object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.ShapeData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.ShapeData

    type [<AllowNullLiteral>] ShapeLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the Shape class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> Shape

    /// Represents the ShapeView class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeView =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Represents the highlight around the shape.
        /// 
        /// [Api set:  1.1]
        abstract highlight: Visio.Highlight with get, set
        /// <summary>Sets multiple properties of an object at the same time. You can pass either a plain object with the appropriate properties, or another API object of the same type.</summary>
        /// <param name="properties">A JavaScript object with properties that are structured isomorphically to the properties of the object on which the method is called.</param>
        /// <param name="options">Provides an option to suppress errors if the properties object tries to set any read-only properties.</param>
        abstract set: properties: Interfaces.ShapeViewUpdateData * ?options: OfficeExtension.UpdateOptions -> unit
        /// Sets multiple properties on the object at the same time, based on an existing loaded object.
        abstract set: properties: Visio.ShapeView -> unit
        /// <summary>Adds an overlay on top of the shape.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="OverlayType">An Overlay Type. Can be 'Text' or 'Image'.</param>
        /// <param name="Content">Content of Overlay.</param>
        /// <param name="OverlayHorizontalAlignment">Horizontal Alignment of Overlay. Can be 'Left', 'Center', or 'Right'.</param>
        /// <param name="OverlayVerticalAlignment">Vertical Alignment of Overlay. Can be 'Top', 'Middle', 'Bottom'.</param>
        /// <param name="Width">Overlay Width.</param>
        /// <param name="Height">Overlay Height.</param>
        abstract addOverlay: OverlayType: Visio.OverlayType * Content: string * OverlayHorizontalAlignment: Visio.OverlayHorizontalAlignment * OverlayVerticalAlignment: Visio.OverlayVerticalAlignment * Width: float * Height: float -> OfficeExtension.ClientResult<float>
        /// <summary>Adds an overlay on top of the shape.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="OverlayType">An Overlay Type. Can be 'Text' or 'Image'.</param>
        /// <param name="Content">Content of Overlay.</param>
        /// <param name="OverlayHorizontalAlignment">Horizontal Alignment of Overlay. Can be 'Left', 'Center', or 'Right'.</param>
        /// <param name="OverlayVerticalAlignment">Vertical Alignment of Overlay. Can be 'Top', 'Middle', 'Bottom'.</param>
        /// <param name="Width">Overlay Width.</param>
        /// <param name="Height">Overlay Height.</param>
        abstract addOverlay: OverlayType: ShapeViewAddOverlayOverlayType * Content: string * OverlayHorizontalAlignment: ShapeViewAddOverlayOverlayHorizontalAlignment * OverlayVerticalAlignment: ShapeViewAddOverlayOverlayVerticalAlignment * Width: float * Height: float -> OfficeExtension.ClientResult<float>
        /// <summary>Removes particular overlay or all overlays on the Shape.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="OverlayId">An Overlay Id. Removes the specific overlay id from the shape.</param>
        abstract removeOverlay: OverlayId: float -> unit
        /// <summary>Shows particular overlay on the Shape.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="overlayId">overlay id in context</param>
        /// <param name="show">to show or hide</param>
        abstract showOverlay: overlayId: float * show: bool -> unit
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: Visio.Interfaces.ShapeViewLoadOptions -> Visio.ShapeView
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.ShapeView
        abstract load: ?option: ShapeViewLoadOption -> Visio.ShapeView
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.ShapeView object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.ShapeViewData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.ShapeViewData

    type [<StringEnum>] [<RequireQualifiedAccess>] ShapeViewAddOverlayOverlayType =
        | [<CompiledName "Text">] Text
        | [<CompiledName "Image">] Image
        | [<CompiledName "Html">] Html

    type [<StringEnum>] [<RequireQualifiedAccess>] ShapeViewAddOverlayOverlayHorizontalAlignment =
        | [<CompiledName "Left">] Left
        | [<CompiledName "Center">] Center
        | [<CompiledName "Right">] Right

    type [<StringEnum>] [<RequireQualifiedAccess>] ShapeViewAddOverlayOverlayVerticalAlignment =
        | [<CompiledName "Top">] Top
        | [<CompiledName "Middle">] Middle
        | [<CompiledName "Bottom">] Bottom

    type [<AllowNullLiteral>] ShapeViewLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the ShapeView class.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeViewStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> ShapeView

    /// Represents the Position of the object in the view.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] Position =
        /// An integer that specifies the x-coordinate of the object, which is the signed value of the distance in pixels from the viewport's center to the left boundary of the page.
        /// 
        /// [Api set:  1.1]
        abstract x: float with get, set
        /// An integer that specifies the y-coordinate of the object, which is the signed value of the distance in pixels from the viewport's center to the top boundary of the page.
        /// 
        /// [Api set:  1.1]
        abstract y: float with get, set

    /// Represents the BoundingBox of the shape.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] BoundingBox =
        /// The distance between the top and bottom edges of the bounding box of the shape, excluding any data graphics associated with the shape.
        /// 
        /// [Api set:  1.1]
        abstract height: float with get, set
        /// The distance between the left and right edges of the bounding box of the shape, excluding any data graphics associated with the shape.
        /// 
        /// [Api set:  1.1]
        abstract width: float with get, set
        /// An integer that specifies the x-coordinate of the bounding box.
        /// 
        /// [Api set:  1.1]
        abstract x: float with get, set
        /// An integer that specifies the y-coordinate of the bounding box.
        /// 
        /// [Api set:  1.1]
        abstract y: float with get, set

    /// Represents the highlight data added to the shape.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] Highlight =
        /// A string that specifies the color of the highlight. It must have the form "#RRGGBB", where each letter represents a hexadecimal digit between 0 and F, and where RR is the red value between 0 and 0xFF (255), GG the green value between 0 and 0xFF (255), and BB is the blue value between 0 and 0xFF (255).
        /// 
        /// [Api set:  1.1]
        abstract color: string with get, set
        /// A positive integer that specifies the width of the highlight's stroke in pixels.
        /// 
        /// [Api set:  1.1]
        abstract width: float with get, set

    /// Represents the ShapeDataItemCollection for a given Shape.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeDataItemCollection =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Gets the loaded child items in this collection.
        abstract items: ResizeArray<Visio.ShapeDataItem>
        /// Gets the number of Shape Data Items.
        /// 
        /// [Api set:  1.1]
        abstract getCount: unit -> OfficeExtension.ClientResult<float>
        /// <summary>Gets the ShapeDataItem using its name.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="key">Key is the name of the ShapeDataItem to be retrieved.</param>
        abstract getItem: key: string -> Visio.ShapeDataItem
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: obj -> Visio.ShapeDataItemCollection
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.ShapeDataItemCollection
        abstract load: ?option: OfficeExtension.LoadOption -> Visio.ShapeDataItemCollection
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original `Visio.ShapeDataItemCollection` object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.ShapeDataItemCollectionData`) that contains an "items" array with shallow copies of any loaded properties from the collection's items.
        abstract toJSON: unit -> Visio.Interfaces.ShapeDataItemCollectionData

    /// Represents the ShapeDataItemCollection for a given Shape.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeDataItemCollectionStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> ShapeDataItemCollection

    /// Represents the ShapeDataItem.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeDataItem =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// A string that specifies the format of the shape data item. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract format: string
        /// A string that specifies the formatted value of the shape data item. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract formattedValue: string
        /// A string that specifies the label of the shape data item. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract label: string
        /// A string that specifies the value of the shape data item. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract value: string
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: Visio.Interfaces.ShapeDataItemLoadOptions -> Visio.ShapeDataItem
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.ShapeDataItem
        abstract load: ?option: ShapeDataItemLoadOption -> Visio.ShapeDataItem
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.ShapeDataItem object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.ShapeDataItemData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.ShapeDataItemData

    type [<AllowNullLiteral>] ShapeDataItemLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the ShapeDataItem.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] ShapeDataItemStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> ShapeDataItem

    /// Represents the Hyperlink Collection.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] HyperlinkCollection =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Gets the loaded child items in this collection.
        abstract items: ResizeArray<Visio.Hyperlink>
        /// Gets the number of hyperlinks.
        /// 
        /// [Api set:  1.1]
        abstract getCount: unit -> OfficeExtension.ClientResult<float>
        /// <summary>Gets a Hyperlink using its key (name or Id).
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="Key">Key is the name or index of the Hyperlink to be retrieved.</param>
        abstract getItem: Key: U2<float, string> -> Visio.Hyperlink
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: obj -> Visio.HyperlinkCollection
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.HyperlinkCollection
        abstract load: ?option: OfficeExtension.LoadOption -> Visio.HyperlinkCollection
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original `Visio.HyperlinkCollection` object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.HyperlinkCollectionData`) that contains an "items" array with shallow copies of any loaded properties from the collection's items.
        abstract toJSON: unit -> Visio.Interfaces.HyperlinkCollectionData

    /// Represents the Hyperlink Collection.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] HyperlinkCollectionStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> HyperlinkCollection

    /// Represents the Hyperlink.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] Hyperlink =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Gets the address of the Hyperlink object. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract address: string
        /// Gets the description of a hyperlink. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract description: string
        /// Gets the extra URL request information used to resolve the hyperlink's URL. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract extraInfo: string
        /// Gets the sub-address of the Hyperlink object. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract subAddress: string
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: Visio.Interfaces.HyperlinkLoadOptions -> Visio.Hyperlink
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.Hyperlink
        abstract load: ?option: HyperlinkLoadOption -> Visio.Hyperlink
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.Hyperlink object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.HyperlinkData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.HyperlinkData

    type [<AllowNullLiteral>] HyperlinkLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the Hyperlink.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] HyperlinkStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> Hyperlink

    /// Represents the CommentCollection for a given Shape.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] CommentCollection =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Gets the loaded child items in this collection.
        abstract items: ResizeArray<Visio.Comment>
        /// Gets the number of Comments.
        /// 
        /// [Api set:  1.1]
        abstract getCount: unit -> OfficeExtension.ClientResult<float>
        /// <summary>Gets the Comment using its name.
        /// 
        /// [Api set:  1.1]</summary>
        /// <param name="key">Key is the name of the Comment to be retrieved.</param>
        abstract getItem: key: string -> Visio.Comment
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: obj -> Visio.CommentCollection
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.CommentCollection
        abstract load: ?option: OfficeExtension.LoadOption -> Visio.CommentCollection
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original `Visio.CommentCollection` object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.CommentCollectionData`) that contains an "items" array with shallow copies of any loaded properties from the collection's items.
        abstract toJSON: unit -> Visio.Interfaces.CommentCollectionData

    /// Represents the CommentCollection for a given Shape.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] CommentCollectionStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> CommentCollection

    /// Represents the Comment.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] Comment =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// A string that specifies the name of the author of the comment.
        /// 
        /// [Api set:  1.1]
        abstract author: string with get, set
        /// A string that specifies the date when the comment was created.
        /// 
        /// [Api set:  1.1]
        abstract date: string with get, set
        /// A string that contains the comment text.
        /// 
        /// [Api set:  1.1]
        abstract text: string with get, set
        /// <summary>Sets multiple properties of an object at the same time. You can pass either a plain object with the appropriate properties, or another API object of the same type.</summary>
        /// <param name="properties">A JavaScript object with properties that are structured isomorphically to the properties of the object on which the method is called.</param>
        /// <param name="options">Provides an option to suppress errors if the properties object tries to set any read-only properties.</param>
        abstract set: properties: Interfaces.CommentUpdateData * ?options: OfficeExtension.UpdateOptions -> unit
        /// Sets multiple properties on the object at the same time, based on an existing loaded object.
        abstract set: properties: Visio.Comment -> unit
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: Visio.Interfaces.CommentLoadOptions -> Visio.Comment
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.Comment
        abstract load: ?option: CommentLoadOption -> Visio.Comment
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.Comment object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.CommentData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.CommentData

    type [<AllowNullLiteral>] CommentLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the Comment.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] CommentStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> Comment

    /// Represents the Selection in the page.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] Selection =
        inherit OfficeExtension.ClientObject
        /// The request context associated with the object. This connects the add-in's process to the Office host application's process.
        abstract context: RequestContext with get, set
        /// Gets the Shapes of the Selection. Read-only.
        /// 
        /// [Api set:  1.1]
        abstract shapes: Visio.ShapeCollection
        /// Queues up a command to load the specified properties of the object. You must call "context.sync()" before reading the properties.
        abstract load: ?option: U2<string, ResizeArray<string>> -> Visio.Selection
        abstract load: ?option: SelectionLoadOption -> Visio.Selection
        /// Overrides the JavaScript `toJSON()` method in order to provide more useful output when an API object is passed to `JSON.stringify()`. (`JSON.stringify`, in turn, calls the `toJSON` method of the object that is passed to it.)
        /// Whereas the original Visio.Selection object is an API object, the `toJSON` method returns a plain JavaScript object (typed as `Visio.Interfaces.SelectionData`) that contains shallow copies of any loaded child properties from the original object.
        abstract toJSON: unit -> Visio.Interfaces.SelectionData

    type [<AllowNullLiteral>] SelectionLoadOption =
        abstract select: string option with get, set
        abstract expand: string option with get, set

    /// Represents the Selection in the page.
    /// 
    /// [Api set:  1.1]
    type [<AllowNullLiteral>] SelectionStatic =
        [<Emit "new $0($1...)">] abstract Create: unit -> Selection

    type [<StringEnum>] [<RequireQualifiedAccess>] OverlayHorizontalAlignment =
        | [<CompiledName "Left">] Left
        | [<CompiledName "Center">] Center
        | [<CompiledName "Right">] Right

    type [<StringEnum>] [<RequireQualifiedAccess>] OverlayVerticalAlignment =
        | [<CompiledName "Top">] Top
        | [<CompiledName "Middle">] Middle
        | [<CompiledName "Bottom">] Bottom

    type [<StringEnum>] [<RequireQualifiedAccess>] OverlayType =
        | [<CompiledName "Text">] Text
        | [<CompiledName "Image">] Image
        | [<CompiledName "Html">] Html

    type [<StringEnum>] [<RequireQualifiedAccess>] ToolBarType =
        | [<CompiledName "CommandBar">] CommandBar
        | [<CompiledName "PageNavigationBar">] PageNavigationBar
        | [<CompiledName "StatusBar">] StatusBar

    type [<StringEnum>] [<RequireQualifiedAccess>] ErrorCodes =
        | [<CompiledName "AccessDenied">] AccessDenied
        | [<CompiledName "GeneralException">] GeneralException
        | [<CompiledName "InvalidArgument">] InvalidArgument
        | [<CompiledName "ItemNotFound">] ItemNotFound
        | [<CompiledName "NotImplemented">] NotImplemented
        | [<CompiledName "UnsupportedOperation">] UnsupportedOperation

    module Interfaces =

        /// Provides ways to load properties of only a subset of members of a collection.
        type [<AllowNullLiteral>] CollectionLoadOptions =
            /// Specify the number of items in the queried collection to be included in the result.
            abstract ``$top``: float option with get, set
            /// Specify the number of items in the collection that are to be skipped and not included in the result. If top is specified, the selection of result will start after skipping the specified number of items.
            abstract ``$skip``: float option with get, set

        /// An interface for updating data on the Application object, for use in "application.set({ ... })".
        type [<AllowNullLiteral>] ApplicationUpdateData =
            /// Show or hide the iFrame application borders.
            /// 
            /// [Api set:  1.1]
            abstract showBorders: bool option with get, set
            /// Show or hide the standard toolbars.
            /// 
            /// [Api set:  1.1]
            abstract showToolbars: bool option with get, set

        /// An interface for updating data on the Document object, for use in "document.set({ ... })".
        type [<AllowNullLiteral>] DocumentUpdateData =
            /// Represents a Visio application instance that contains this document.
            /// 
            /// [Api set:  1.1]
            abstract application: Visio.Interfaces.ApplicationUpdateData option with get, set
            /// Returns the DocumentView object.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.DocumentViewUpdateData option with get, set

        /// An interface for updating data on the DocumentView object, for use in "documentView.set({ ... })".
        type [<AllowNullLiteral>] DocumentViewUpdateData =
            /// Disable Hyperlinks.
            /// 
            /// [Api set:  1.1]
            abstract disableHyperlinks: bool option with get, set
            /// Disable Pan.
            /// 
            /// [Api set:  1.1]
            abstract disablePan: bool option with get, set
            /// Disable PanZoomWindow.
            /// 
            /// [Api set:  1.1]
            abstract disablePanZoomWindow: bool option with get, set
            /// Disable Zoom.
            /// 
            /// [Api set:  1.1]
            abstract disableZoom: bool option with get, set
            /// Hide Diagram Boundary.
            /// 
            /// [Api set:  1.1]
            abstract hideDiagramBoundary: bool option with get, set

        /// An interface for updating data on the Page object, for use in "page.set({ ... })".
        type [<AllowNullLiteral>] PageUpdateData =
            /// Returns the view of the page.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.PageViewUpdateData option with get, set

        /// An interface for updating data on the PageView object, for use in "pageView.set({ ... })".
        type [<AllowNullLiteral>] PageViewUpdateData =
            /// Get and set Page's Zoom level. The value can be between 10 and 400 and denotes the percentage of zoom.
            /// 
            /// [Api set:  1.1]
            abstract zoom: float option with get, set

        /// An interface for updating data on the PageCollection object, for use in "pageCollection.set({ ... })".
        type [<AllowNullLiteral>] PageCollectionUpdateData =
            abstract items: ResizeArray<Visio.Interfaces.PageData> option with get, set

        /// An interface for updating data on the ShapeCollection object, for use in "shapeCollection.set({ ... })".
        type [<AllowNullLiteral>] ShapeCollectionUpdateData =
            abstract items: ResizeArray<Visio.Interfaces.ShapeData> option with get, set

        /// An interface for updating data on the Shape object, for use in "shape.set({ ... })".
        type [<AllowNullLiteral>] ShapeUpdateData =
            /// Returns the view of the shape.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.ShapeViewUpdateData option with get, set
            /// Returns true, if shape is selected. User can set true to select the shape explicitly.
            /// 
            /// [Api set:  1.1]
            abstract select: bool option with get, set

        /// An interface for updating data on the ShapeView object, for use in "shapeView.set({ ... })".
        type [<AllowNullLiteral>] ShapeViewUpdateData =
            /// Represents the highlight around the shape.
            /// 
            /// [Api set:  1.1]
            abstract highlight: Visio.Highlight option with get, set

        /// An interface for updating data on the ShapeDataItemCollection object, for use in "shapeDataItemCollection.set({ ... })".
        type [<AllowNullLiteral>] ShapeDataItemCollectionUpdateData =
            abstract items: ResizeArray<Visio.Interfaces.ShapeDataItemData> option with get, set

        /// An interface for updating data on the HyperlinkCollection object, for use in "hyperlinkCollection.set({ ... })".
        type [<AllowNullLiteral>] HyperlinkCollectionUpdateData =
            abstract items: ResizeArray<Visio.Interfaces.HyperlinkData> option with get, set

        /// An interface for updating data on the CommentCollection object, for use in "commentCollection.set({ ... })".
        type [<AllowNullLiteral>] CommentCollectionUpdateData =
            abstract items: ResizeArray<Visio.Interfaces.CommentData> option with get, set

        /// An interface for updating data on the Comment object, for use in "comment.set({ ... })".
        type [<AllowNullLiteral>] CommentUpdateData =
            /// A string that specifies the name of the author of the comment.
            /// 
            /// [Api set:  1.1]
            abstract author: string option with get, set
            /// A string that specifies the date when the comment was created.
            /// 
            /// [Api set:  1.1]
            abstract date: string option with get, set
            /// A string that contains the comment text.
            /// 
            /// [Api set:  1.1]
            abstract text: string option with get, set

        /// An interface describing the data returned by calling "application.toJSON()".
        type [<AllowNullLiteral>] ApplicationData =
            /// Show or hide the iFrame application borders.
            /// 
            /// [Api set:  1.1]
            abstract showBorders: bool option with get, set
            /// Show or hide the standard toolbars.
            /// 
            /// [Api set:  1.1]
            abstract showToolbars: bool option with get, set

        /// An interface describing the data returned by calling "document.toJSON()".
        type [<AllowNullLiteral>] DocumentData =
            /// Represents a Visio application instance that contains this document. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract application: Visio.Interfaces.ApplicationData option with get, set
            /// Represents a collection of pages associated with the document. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract pages: ResizeArray<Visio.Interfaces.PageData> option with get, set
            /// Returns the DocumentView object. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.DocumentViewData option with get, set

        /// An interface describing the data returned by calling "documentView.toJSON()".
        type [<AllowNullLiteral>] DocumentViewData =
            /// Disable Hyperlinks.
            /// 
            /// [Api set:  1.1]
            abstract disableHyperlinks: bool option with get, set
            /// Disable Pan.
            /// 
            /// [Api set:  1.1]
            abstract disablePan: bool option with get, set
            /// Disable PanZoomWindow.
            /// 
            /// [Api set:  1.1]
            abstract disablePanZoomWindow: bool option with get, set
            /// Disable Zoom.
            /// 
            /// [Api set:  1.1]
            abstract disableZoom: bool option with get, set
            /// Hide Diagram Boundary.
            /// 
            /// [Api set:  1.1]
            abstract hideDiagramBoundary: bool option with get, set

        /// An interface describing the data returned by calling "page.toJSON()".
        type [<AllowNullLiteral>] PageData =
            /// All shapes in the Page, including subshapes. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract allShapes: ResizeArray<Visio.Interfaces.ShapeData> option with get, set
            /// Returns the Comments Collection.  Read-only.
            /// 
            /// [Api set:  1.1]
            abstract comments: ResizeArray<Visio.Interfaces.CommentData> option with get, set
            /// All top-level shapes in the Page.Read-only.
            /// 
            /// [Api set:  1.1]
            abstract shapes: ResizeArray<Visio.Interfaces.ShapeData> option with get, set
            /// Returns the view of the page. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.PageViewData option with get, set
            /// Returns the height of the page. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract height: float option with get, set
            /// Index of the Page. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract index: float option with get, set
            /// Whether the page is a background page or not. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract isBackground: bool option with get, set
            /// Page name. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract name: string option with get, set
            /// Returns the width of the page. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract width: float option with get, set

        /// An interface describing the data returned by calling "pageView.toJSON()".
        type [<AllowNullLiteral>] PageViewData =
            /// Get and set Page's Zoom level. The value can be between 10 and 400 and denotes the percentage of zoom.
            /// 
            /// [Api set:  1.1]
            abstract zoom: float option with get, set

        /// An interface describing the data returned by calling "pageCollection.toJSON()".
        type [<AllowNullLiteral>] PageCollectionData =
            abstract items: ResizeArray<Visio.Interfaces.PageData> option with get, set

        /// An interface describing the data returned by calling "shapeCollection.toJSON()".
        type [<AllowNullLiteral>] ShapeCollectionData =
            abstract items: ResizeArray<Visio.Interfaces.ShapeData> option with get, set

        /// An interface describing the data returned by calling "shape.toJSON()".
        type [<AllowNullLiteral>] ShapeData =
            /// Returns the Comments Collection. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract comments: ResizeArray<Visio.Interfaces.CommentData> option with get, set
            /// Returns the Hyperlinks collection for a Shape object. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract hyperlinks: ResizeArray<Visio.Interfaces.HyperlinkData> option with get, set
            /// Returns the Shape's Data Section. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract shapeDataItems: ResizeArray<Visio.Interfaces.ShapeDataItemData> option with get, set
            /// Gets SubShape Collection. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract subShapes: ResizeArray<Visio.Interfaces.ShapeData> option with get, set
            /// Returns the view of the shape. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.ShapeViewData option with get, set
            /// Shape's identifier. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract id: float option with get, set
            /// Shape's name. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract name: string option with get, set
            /// Returns true, if shape is selected. User can set true to select the shape explicitly.
            /// 
            /// [Api set:  1.1]
            abstract select: bool option with get, set
            /// Shape's text. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract text: string option with get, set

        /// An interface describing the data returned by calling "shapeView.toJSON()".
        type [<AllowNullLiteral>] ShapeViewData =
            /// Represents the highlight around the shape.
            /// 
            /// [Api set:  1.1]
            abstract highlight: Visio.Highlight option with get, set

        /// An interface describing the data returned by calling "shapeDataItemCollection.toJSON()".
        type [<AllowNullLiteral>] ShapeDataItemCollectionData =
            abstract items: ResizeArray<Visio.Interfaces.ShapeDataItemData> option with get, set

        /// An interface describing the data returned by calling "shapeDataItem.toJSON()".
        type [<AllowNullLiteral>] ShapeDataItemData =
            /// A string that specifies the format of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract format: string option with get, set
            /// A string that specifies the formatted value of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract formattedValue: string option with get, set
            /// A string that specifies the label of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract label: string option with get, set
            /// A string that specifies the value of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract value: string option with get, set

        /// An interface describing the data returned by calling "hyperlinkCollection.toJSON()".
        type [<AllowNullLiteral>] HyperlinkCollectionData =
            abstract items: ResizeArray<Visio.Interfaces.HyperlinkData> option with get, set

        /// An interface describing the data returned by calling "hyperlink.toJSON()".
        type [<AllowNullLiteral>] HyperlinkData =
            /// Gets the address of the Hyperlink object. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract address: string option with get, set
            /// Gets the description of a hyperlink. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract description: string option with get, set
            /// Gets the extra URL request information used to resolve the hyperlink's URL. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract extraInfo: string option with get, set
            /// Gets the sub-address of the Hyperlink object. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract subAddress: string option with get, set

        /// An interface describing the data returned by calling "commentCollection.toJSON()".
        type [<AllowNullLiteral>] CommentCollectionData =
            abstract items: ResizeArray<Visio.Interfaces.CommentData> option with get, set

        /// An interface describing the data returned by calling "comment.toJSON()".
        type [<AllowNullLiteral>] CommentData =
            /// A string that specifies the name of the author of the comment.
            /// 
            /// [Api set:  1.1]
            abstract author: string option with get, set
            /// A string that specifies the date when the comment was created.
            /// 
            /// [Api set:  1.1]
            abstract date: string option with get, set
            /// A string that contains the comment text.
            /// 
            /// [Api set:  1.1]
            abstract text: string option with get, set

        /// An interface describing the data returned by calling "selection.toJSON()".
        type [<AllowNullLiteral>] SelectionData =
            /// Gets the Shapes of the Selection. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract shapes: ResizeArray<Visio.Interfaces.ShapeData> option with get, set

        /// Represents the Application.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] ApplicationLoadOptions =
            abstract ``$all``: bool option with get, set
            /// Show or hide the iFrame application borders.
            /// 
            /// [Api set:  1.1]
            abstract showBorders: bool option with get, set
            /// Show or hide the standard toolbars.
            /// 
            /// [Api set:  1.1]
            abstract showToolbars: bool option with get, set

        /// Represents the Document class.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] DocumentLoadOptions =
            abstract ``$all``: bool option with get, set
            /// Represents a Visio application instance that contains this document.
            /// 
            /// [Api set:  1.1]
            abstract application: Visio.Interfaces.ApplicationLoadOptions option with get, set
            /// Returns the DocumentView object.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.DocumentViewLoadOptions option with get, set

        /// Represents the DocumentView class.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] DocumentViewLoadOptions =
            abstract ``$all``: bool option with get, set
            /// Disable Hyperlinks.
            /// 
            /// [Api set:  1.1]
            abstract disableHyperlinks: bool option with get, set
            /// Disable Pan.
            /// 
            /// [Api set:  1.1]
            abstract disablePan: bool option with get, set
            /// Disable PanZoomWindow.
            /// 
            /// [Api set:  1.1]
            abstract disablePanZoomWindow: bool option with get, set
            /// Disable Zoom.
            /// 
            /// [Api set:  1.1]
            abstract disableZoom: bool option with get, set
            /// Hide Diagram Boundary.
            /// 
            /// [Api set:  1.1]
            abstract hideDiagramBoundary: bool option with get, set

        /// Represents the Page class.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] PageLoadOptions =
            abstract ``$all``: bool option with get, set
            /// Returns the view of the page.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.PageViewLoadOptions option with get, set
            /// Returns the height of the page. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract height: bool option with get, set
            /// Index of the Page. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract index: bool option with get, set
            /// Whether the page is a background page or not. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract isBackground: bool option with get, set
            /// Page name. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract name: bool option with get, set
            /// Returns the width of the page. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract width: bool option with get, set

        /// Represents the PageView class.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] PageViewLoadOptions =
            abstract ``$all``: bool option with get, set
            /// Get and set Page's Zoom level. The value can be between 10 and 400 and denotes the percentage of zoom.
            /// 
            /// [Api set:  1.1]
            abstract zoom: bool option with get, set

        /// Represents a collection of Page objects that are part of the document.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] PageCollectionLoadOptions =
            abstract ``$all``: bool option with get, set
            /// For EACH ITEM in the collection: Returns the view of the page.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.PageViewLoadOptions option with get, set
            /// For EACH ITEM in the collection: Returns the height of the page. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract height: bool option with get, set
            /// For EACH ITEM in the collection: Index of the Page. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract index: bool option with get, set
            /// For EACH ITEM in the collection: Whether the page is a background page or not. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract isBackground: bool option with get, set
            /// For EACH ITEM in the collection: Page name. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract name: bool option with get, set
            /// For EACH ITEM in the collection: Returns the width of the page. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract width: bool option with get, set

        /// Represents the Shape Collection.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] ShapeCollectionLoadOptions =
            abstract ``$all``: bool option with get, set
            /// For EACH ITEM in the collection: Returns the view of the shape.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.ShapeViewLoadOptions option with get, set
            /// For EACH ITEM in the collection: Shape's identifier. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract id: bool option with get, set
            /// For EACH ITEM in the collection: Shape's name. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract name: bool option with get, set
            /// For EACH ITEM in the collection: Returns true, if shape is selected. User can set true to select the shape explicitly.
            /// 
            /// [Api set:  1.1]
            abstract select: bool option with get, set
            /// For EACH ITEM in the collection: Shape's text. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract text: bool option with get, set

        /// Represents the Shape class.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] ShapeLoadOptions =
            abstract ``$all``: bool option with get, set
            /// Returns the view of the shape.
            /// 
            /// [Api set:  1.1]
            abstract view: Visio.Interfaces.ShapeViewLoadOptions option with get, set
            /// Shape's identifier. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract id: bool option with get, set
            /// Shape's name. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract name: bool option with get, set
            /// Returns true, if shape is selected. User can set true to select the shape explicitly.
            /// 
            /// [Api set:  1.1]
            abstract select: bool option with get, set
            /// Shape's text. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract text: bool option with get, set

        /// Represents the ShapeView class.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] ShapeViewLoadOptions =
            abstract ``$all``: bool option with get, set
            /// Represents the highlight around the shape.
            /// 
            /// [Api set:  1.1]
            abstract highlight: bool option with get, set

        /// Represents the ShapeDataItemCollection for a given Shape.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] ShapeDataItemCollectionLoadOptions =
            abstract ``$all``: bool option with get, set
            /// For EACH ITEM in the collection: A string that specifies the format of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract format: bool option with get, set
            /// For EACH ITEM in the collection: A string that specifies the formatted value of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract formattedValue: bool option with get, set
            /// For EACH ITEM in the collection: A string that specifies the label of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract label: bool option with get, set
            /// For EACH ITEM in the collection: A string that specifies the value of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract value: bool option with get, set

        /// Represents the ShapeDataItem.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] ShapeDataItemLoadOptions =
            abstract ``$all``: bool option with get, set
            /// A string that specifies the format of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract format: bool option with get, set
            /// A string that specifies the formatted value of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract formattedValue: bool option with get, set
            /// A string that specifies the label of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract label: bool option with get, set
            /// A string that specifies the value of the shape data item. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract value: bool option with get, set

        /// Represents the Hyperlink Collection.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] HyperlinkCollectionLoadOptions =
            abstract ``$all``: bool option with get, set
            /// For EACH ITEM in the collection: Gets the address of the Hyperlink object. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract address: bool option with get, set
            /// For EACH ITEM in the collection: Gets the description of a hyperlink. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract description: bool option with get, set
            /// For EACH ITEM in the collection: Gets the extra URL request information used to resolve the hyperlink's URL. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract extraInfo: bool option with get, set
            /// For EACH ITEM in the collection: Gets the sub-address of the Hyperlink object. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract subAddress: bool option with get, set

        /// Represents the Hyperlink.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] HyperlinkLoadOptions =
            abstract ``$all``: bool option with get, set
            /// Gets the address of the Hyperlink object. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract address: bool option with get, set
            /// Gets the description of a hyperlink. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract description: bool option with get, set
            /// Gets the extra URL request information used to resolve the hyperlink's URL. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract extraInfo: bool option with get, set
            /// Gets the sub-address of the Hyperlink object. Read-only.
            /// 
            /// [Api set:  1.1]
            abstract subAddress: bool option with get, set

        /// Represents the CommentCollection for a given Shape.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] CommentCollectionLoadOptions =
            abstract ``$all``: bool option with get, set
            /// For EACH ITEM in the collection: A string that specifies the name of the author of the comment.
            /// 
            /// [Api set:  1.1]
            abstract author: bool option with get, set
            /// For EACH ITEM in the collection: A string that specifies the date when the comment was created.
            /// 
            /// [Api set:  1.1]
            abstract date: bool option with get, set
            /// For EACH ITEM in the collection: A string that contains the comment text.
            /// 
            /// [Api set:  1.1]
            abstract text: bool option with get, set

        /// Represents the Comment.
        /// 
        /// [Api set:  1.1]
        type [<AllowNullLiteral>] CommentLoadOptions =
            abstract ``$all``: bool option with get, set
            /// A string that specifies the name of the author of the comment.
            /// 
            /// [Api set:  1.1]
            abstract author: bool option with get, set
            /// A string that specifies the date when the comment was created.
            /// 
            /// [Api set:  1.1]
            abstract date: bool option with get, set
            /// A string that contains the comment text.
            /// 
            /// [Api set:  1.1]
            abstract text: bool option with get, set

    /// The RequestContext object facilitates requests to the Visio application. Since the Office add-in and the Visio application run in two different processes, the request context is required to get access to the Visio object model from the add-in.
    type [<AllowNullLiteral>] RequestContext =
        inherit OfficeCore.RequestContext
        abstract document: Document

    /// The RequestContext object facilitates requests to the Visio application. Since the Office add-in and the Visio application run in two different processes, the request context is required to get access to the Visio object model from the add-in.
    type [<AllowNullLiteral>] RequestContextStatic =
        [<Emit "new $0($1...)">] abstract Create: ?url: U2<string, OfficeExtension.EmbeddedSession> -> RequestContext