module Utils

module String =
    let join sep (parts: string seq) =
        System.String.Join(sep, parts)
