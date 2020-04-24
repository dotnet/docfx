namespace NetCoreProject

type MyType() =
    override this.Equals other = failwith ""
    override this.GetHashCode() = 0

module Foo =
    let myObj =
        { new obj() with
            member x.Equals y = failwith "" }
