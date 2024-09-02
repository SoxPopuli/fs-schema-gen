module Parser.TypeTree

open Tomlyn.Model
open ItemType
open Utils

(*
        root
       /    \
    First  Second
          /      \
         A        B
*)


type TypeTree =
    | Branch of FieldName: string * Children: TypeTree list
    | Leaf of FieldName: string * TypePath: ItemType.ItemType

let private getChildrenFromTable makeNode (tbl: #Table) =
    tbl |> Seq.map (fun kv -> makeNode kv.Key kv.Value) |> Seq.toList

let rec private makeNode (entityLoader: EntityLoader) (k: string) (v: obj) =
    match v with
    | :? string as s ->
        let path = s.Split '.'

        let typ =
            entityLoader.FindEntityByPath path
            |> function
                | None -> failwith $"Type {s} not found"
                | Some t -> ItemType.fromEntity t

        Leaf(k, typ)
    | :? Table as tbl ->
        let children = getChildrenFromTable (makeNode entityLoader) tbl
        Branch(k, children)
    | _ ->
        let typeName = v.GetType().FullName
        failwith $"Unrecognised type {typeName} for key {k}"

let fromTable entityLoader rootName (table: #Table) =
    Branch(rootName, getChildrenFromTable (makeNode entityLoader) table)

let applyOverrides (baseTree: ItemType) (overrides: TypeTree) =
    // Breadth First Search for both trees in parallel
    // If an entry gets replaced, skip to the next
    // Finish when the tree has been fully traversed

    let rec replaceNode baseNode overrideNode =
        match (baseNode, overrideNode) with
        | Record(recordName, recordFields), Branch(branchName, branchFields) -> failwith "TODO"
        | Record(recordName, recordFields), Leaf(leafName, leafType) -> failwith "TODO"
        | AnonymousRecord(recordFields), Branch(branchName, branchFields) -> failwith "TODO"
        | AnonymousRecord(recordFields), Leaf(leafName, leafType) -> failwith "TODO"
        | other, Branch(branchName, branchType) ->
            // Don't think this state is legal? 👮
            failwith "TODO"
        | _, Leaf(_, leafType) -> leafType

    ()
