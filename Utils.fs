module Utils

open System.Collections.Generic

type Table = IDictionary<string, obj>

module String =
    let join sep (parts: string seq) = System.String.Join(sep, parts)
