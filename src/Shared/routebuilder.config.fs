namespace Swate.Components.Shared

// Do not rename this file or change its content without adressing nuget package release process!

module Config =

    // Why the #if...#else...#endif?
    // -> This is used to switch between development and production when transpiling for npm package.
    // Why "routebuilder.config.fs" and "routebuilder.nupkg.config.fs"?
    // -> When packing nuget packages, this if-else block would be copied into the package. The executing environment would then need to set the
    //      "PUBLISH_COMPONENTS" variable. Therefore we ignore this file and instead pack "routebuilder.nupkg.config.fs" which is then correctly usable.
    let URL_PREFIX =
        #if PUBLISH_COMPONENTS
        URLs.PRODUCTION_URL
        #else
        ""
        #endif
