module Parser.TypeTree

open Tomlyn.Model

(*
        root
       /    \
    First  Second
          /      \
         A        B
*)

type TreeNode =
    | Branch of Children: TypeTree list
    | Leaf of TypePath: string

and TypeTree = { FieldName: string; Node: TreeNode }

let private getChildrenFromTable makeNode (tbl: TomlTable) =
    tbl |> Seq.map (fun kv -> makeNode kv.Key kv.Value) |> Seq.toList

[<TailCall>]
let rec private makeNode (k: string) (v: obj) =
    match v with
    | :? string as s -> { FieldName = k; Node = Leaf s }
    | :? TomlTable as tbl ->
        let children = tbl |> getChildrenFromTable makeNode

        {
            FieldName = k
            Node = Branch children
        }
    | _ ->
        let typeName = v.GetType().FullName
        failwith $"Unrecognised type {typeName} for key {k}"

let fromTable (table: TomlTable) = {
    FieldName = "root"
    Node = Branch(getChildrenFromTable makeNode table)
}
