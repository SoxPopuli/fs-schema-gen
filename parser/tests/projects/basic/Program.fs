module DeeplyNested =
    module Nested2 =
        module Nested3 =
            module Nested4 =
                module Nested5 =
                    module Nested6 =
                        type DeeplyNestedRecord = { X: int }

type RecordType = {
    A: int
    B: float
    C: string
    D: double
    E: decimal
    F: bool
    List: int list
    Array: int array
    Tuple: (int * float)
    Opt: int option
    Res: Result<int, string>
    Nested: NestedRecord option
    DeeplyNested: DeeplyNested.Nested2.Nested3.Nested4.Nested5.Nested6.DeeplyNestedRecord
    Anonymous: {| Anon: int; Second: float; B: string |}
}

and NestedRecord = { X: int }

