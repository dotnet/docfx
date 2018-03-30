namespace NetCoreProject

module Module2 =

    exception ExceptionInModule2

    let func2 () =
        ()

    type Type2 () =
        /// <summary>Method2 summary.</summary>
        /// <param name="arg1">arg1 text.</param>
        /// <returns>Returns text.</returns>
        member __.Method2 (arg1: string) =
            arg1

        /// <summary>Summary for Property2.</summary>
        /// <value>Value text.</value>
        member __.Property2 =
            Module1.Type1()


    /// <summary>Type3 summary.</summary>
    /// <typeparam name="'G">Generic text.</typeparam>
    /// <param name="arg1">arg1 text.</param>
    type Type3<'G> (arg1: 'G) =

        /// Property1 summary.
        member __.Property1
            with get() = arg1
            and set (value: 'G) = ()

