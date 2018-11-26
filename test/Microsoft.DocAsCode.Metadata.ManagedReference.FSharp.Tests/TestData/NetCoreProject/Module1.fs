namespace NetCoreProject


/// Module1 summary.
module Module1 =

    exception ExceptionInModule1

    let func1 () =
        ()

    /// <summary>Func2 summary.</summary>
    /// <param name="arg1">arg1 text.</param>
    /// <param name="arg2">arg2 text.</param>
    /// <returns>Returns text.</returns>
    /// <remarks>Remarks text.</remarks>
    let Func2 (arg1: string) (arg2: int) =
        Some (arg1, arg2)

    type Type1() =
        member __.Method1 () =
            ()

