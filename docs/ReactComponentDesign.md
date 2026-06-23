# Guidelines for isolated React components (`src/Components`)

Components in `src/Components` are expected to be reusable and application-agnostic.

# File Rules

- **Component folder**: Each component should be placed in a folder named after the component.
    - e.g. `src/Components/src/TermSearch` contains `TermSearch.tsx`, `TermSearchConfigProvider.fs` and `TermSearchConfigSetter.fs`
    - e.g. `src/Components/src/Authentication` contains `Authentication.fs`, `AccountManager.fs`, `Helper.fs` and `Types.fs`
    - e.g. higher level nested components MAY be placed in subfolders and add the subfolder name to the namespace, e.g. `/Notes/NoteSearch/NoteSearchComponent.fs` with namespace `namespace Swate.Components.Notes.NoteSearch`.
- **PascalCase file names**: `MyComponent.tsx` for the component "MyComponent" and `MyComponent.stories.tsx` for its Storybook tests.
- **File namespace**: Component files MUST have a namespace that follows the folder structure and use a `type <FileName> =` declaration. Non-component files MUST follow folder structure down to the file and use module declaration.

    For example:

    -  `src/Components/src/Authentication/Authentication.fs` is a component file and should have:

        ```fsharp
        namespace Swate.Components.Authentication

        [<Erase; Mangle(false)>]
        type Authentication =
            // ...
        ```

    - `src/Components/src/Authentication/Types.fs` is NOT a component file and should have:

        ```fsharp
        module Swate.Components.Authentication.Types

        // ...
        ```
- **Colocated stories**: Storybook files must be colocated with the component and named `<Component>.stories.tsx`.
- **Types**
    - Types used by multiple files MUST be placed in a separate file `Types.fs`. 
    - Types only used inside a single file can MUST be placed inside a private `module <FileName>Types` in the same file. 
    - If there are multiple domains requiring types, place them in a `Types/<DomainName>.fs` file.
- **Helper functions**: Helper functions follow the same rules as Types.
- **Subcomponents**: 
    - Large subcomponents SHOULD be split into logical subcomponents.
    - Subcomponents MUST be defined as static members of the main component class if they are sufficiently simple.
    - Subcomponents MUST be placed in separate component-files in the same folder if they are complex.

# Syntax Rules

- **PascalCase**: MUST use PascalCase for **component** and **object** names.
- **CamelCase**: MUST use camelCase for **props** and **functions**.
- **`[<ReactComponent>]`**: If a component returns a `ReactElement`, it MUST be decorated with `[<ReactComponent>]`.
- **Components as static members**: Any components MUST be defined as static members of a class.
    - They MUST use tupled args. This allows us to use optional params with `?optionalParam` syntax, as well as named params with `namedParam = namedParamValue` syntax.
    - These Classes MUST have the `[<Erase; Mangle(false)>]` attribute to improve interop with native TypeScript.
    - Public component names MUST be Standalone descriptive if they are intended for TypeScript interop. The main component name MUST be equal to the file name. Subcomponent names MUST be descriptive of their purpose and not include the main component name as a prefix. For example, if the main component is `MyComponent`, a subcomponent should be named `Header` instead of `MyComponentHeader`. In addition, subcomponents SHOULD be private.

    ```fsharp
    [<Erase; Mangle(false)>]
    type MyComponent =
        [<ReactComponent>]
        static member MyComponent () : ReactElement =
            // ...
    ```
- **React context**: If a component needs to share state or configuration with a React context. It MUST define the base context in a separate file `**/<ComponentName>/Context.fs` or `**/<ComponentName>/Contexts/<ContextName>Context.fs` or `**/<ComponentName>/<ContextName>Context.fs` if there are multiple contexts associated with the component, with the following properties.
    - File namespace MUST follow folder structure and be named `module Swate.Components.<Path>.<ContextName>Context` or if there is a single context `module Swate.Components.<Path>.Context`.
    - MAY contain the relevant public types.
    - MAY contain helper functions inside a MAYBE public module. This module should be named `<ContextName>ContextHelper`.
    - It MUST NOT contain any React components.
    - It MUST contain the `React.createContext` implementation in PascalCase with the suffix `Ctx`. For example, `ExampleCtx`.
        - It must be a root level `let` binding.
    - It MUST contain a hook to easily bind to the context. This hook MUST be named `use<ContextName>Ctx`. For example, `useExampleCtx`.
    - Example for an context "Example" for a component "ExampleComponent":

        ```fsharp
        module Swate.Components.ExampleComponent.Context

        // ..

        type PublicBananaType = {
            // ...
        }

        module ExampleContextHelper =
            // ...

        let ExampleCtx = React.createContext<PublicBananaType> (PublicBananaType.init ())

        [<Hook>]
        let useExampleCtx () = React.useContext ExampleCtx
        ```

# Design Rules

- Keep states at the lowest level possible. Every state change forces a re-render of the component and all its children. If a state is only used in a subcomponent, it should be defined in that subcomponent. 
