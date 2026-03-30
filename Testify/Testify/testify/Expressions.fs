namespace MiniLib.Testify


open System.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Reflection


[<RequireQualifiedAccess>]
module Expressions =
    let private isFunctionType (valueType: System.Type) : bool =
        let isAssignable = typeof<System.Delegate>.IsAssignableFrom valueType

        let isFunction =
            valueType.IsGenericType
            && valueType.GetGenericTypeDefinition () = typedefof<FSharpFunc<_, _>>

        isAssignable || isFunction

    let private tryGetConstant (expr: Expr) : (objnull * System.Type) option =
        match expr with
        | Patterns.Value (value, valueType)
        | Patterns.ValueWithName (value, valueType, _) ->
            Some (value, valueType)
        | _ -> None

    let toString (expr: Expr) : string =
        Swensen.Unquote.Operators.decompile expr

    let substitute (substitution: Var -> Expr option) (expr: Expr) : Expr =
        expr.Substitute substitution

    let substituteVar (variable: Var) (replacement: Expr) (expr: Expr) : Expr =
        substitute (fun current ->
            if current = variable then
                Some replacement
            else
                None) expr

    let private preserveFriendlyName (variable: Var) (expr: Expr) : Expr =
        match tryGetConstant expr with
        | Some (_, valueType) when
            isFunctionType valueType
            && not (System.String.IsNullOrWhiteSpace variable.Name) ->
                Expr.Var variable
        | _ -> expr

    let private simplifyShape (simplify: Expr -> Expr) (expr: Expr) : Expr =
        match expr with
        | ExprShape.ShapeVar _ ->
            expr
        | ExprShape.ShapeLambda (variable, body) ->
            Expr.Lambda (variable, simplify body)
        | ExprShape.ShapeCombination (shape, args) ->
            ExprShape.RebuildShapeCombination (shape, args |> List.map simplify)

    let private tryFindSingleFieldIndexByType
        (fieldType: System.Type)
        (fields: PropertyInfo array)
        : int option =
        let matchingIndexes =
            fields
            |> Array.indexed
            |> Array.choose (fun (index, field) ->
                if field.PropertyType = fieldType then Some index
                else None)

        if matchingIndexes.Length = 1 then
            Some matchingIndexes[0]
        else
            None

    let private tryFindFieldIndexByName
        (name: string)
        (fields: PropertyInfo array)
        : int option =
        fields
        |> Array.tryFindIndex (fun field -> field.Name = name)

    let private tryGetGetterPropertyName
        (methodInfo: MethodInfo)
        : string option =
        if methodInfo.IsSpecialName && methodInfo.Name.StartsWith "get_" then
            Some (methodInfo.Name.Substring "get_".Length)
        else
            None

    let private tryGetFieldSource
        (expr: Expr)
        : (PropertyInfo array * Expr array) option =
        match expr with
        | Patterns.NewUnionCase (unionCase, fieldValues) ->
            Some (unionCase.GetFields (), fieldValues |> List.toArray)
        | Patterns.NewRecord (recordType, fieldValues) ->
            Some (FSharpType.GetRecordFields recordType, fieldValues |> List.toArray)
        | _ ->
            None

    let private tryReduceFieldAccessTarget
        (target: Expr)
        (memberName: string option)
        (memberType: System.Type)
        : Expr option =
        tryGetFieldSource target
        |> Option.bind (fun (fields, values) ->
            memberName
            |> Option.bind (fun name -> tryFindFieldIndexByName name fields)
            |> Option.orElseWith (fun () -> tryFindSingleFieldIndexByType memberType fields)
            |> Option.bind (fun index -> Array.tryItem index values))

    let private tryGetAccessorTarget
        (instanceTarget: Expr option)
        (arguments: Expr list)
        : Expr option =
        match instanceTarget, arguments with
        | Some target, [] ->
            Some target
        | None, [ target ] ->
            Some target
        | _ ->
            None

    let private tryReduceConstantPropertyAccess
        (target: Expr)
        (propertyInfo: PropertyInfo)
        : Expr option =
        match tryGetConstant target with
        | Some (targetValue, _) ->
            Some (Expr.Value (propertyInfo.GetValue targetValue, propertyInfo.PropertyType))
        | None ->
            None

    let private tryReduceConstantMethodCall
        (target: Expr)
        (methodInfo: MethodInfo)
        : Expr option =
        match tryGetConstant target with
        | Some (targetValue, _) ->
            Some (Expr.Value (methodInfo.Invoke (targetValue, [||]), methodInfo.ReturnType))
        | None ->
            None

    let private trySimplifyFieldAccess (expr: Expr) : Expr option =
        match expr with
        | Patterns.PropertyGet (instanceTarget, propertyInfo, arguments) ->
            match tryGetAccessorTarget instanceTarget arguments with
            | Some target ->
                tryReduceFieldAccessTarget target (Some propertyInfo.Name) propertyInfo.PropertyType
                |> Option.orElseWith (fun () ->
                    match instanceTarget, arguments with
                    | Some constantTarget, [] ->
                        tryReduceConstantPropertyAccess constantTarget propertyInfo
                    | _ ->
                        None)
            | None ->
                None
        | Patterns.Call (instanceTarget, methodInfo, arguments)
            when methodInfo.GetParameters().Length = 0 ->
            match tryGetAccessorTarget instanceTarget arguments with
            | Some target ->
                tryReduceFieldAccessTarget
                    target
                    (tryGetGetterPropertyName methodInfo)
                    methodInfo.ReturnType
                |> Option.orElseWith (fun () ->
                    match instanceTarget, arguments with
                    | Some constantTarget, [] ->
                        tryReduceConstantMethodCall constantTarget methodInfo
                    | _ ->
                        None)
            | None ->
                None
        | _ ->
            None

    let private substituteByVar
        (replacements: (Var * Expr) array)
        (body: Expr)
        : Expr =
        body.Substitute (fun variable ->
            replacements
            |> Array.tryPick (fun (candidate, replacement) ->
                if candidate = variable then Some replacement
                else None))

    let rec simplify (expr: Expr) : Expr =
        let simplified =
            match expr with
            | Patterns.Let (variable, value, body) ->
                let value' = simplify value
                let body' = simplify body
                substituteVar variable (preserveFriendlyName variable value') body'
            | Patterns.Application (Patterns.Lambda (variable, body), argument) ->
                let argument' = simplify argument
                substituteVar variable argument' body
                |> simplify
            | _ -> simplifyShape simplify expr

        match simplified with
        | Patterns.Let (variable, value, body) ->
            substituteVar variable value body
            |> simplify
        | Patterns.Application (Patterns.Lambda (variable, body), argument) ->
            substituteVar variable argument body
            |> simplify
        | Patterns.TupleGet (Patterns.NewTuple elements, index) ->
            elements[index]
            |> simplify
        | Patterns.TupleGet (tupleExpr, index) ->
            match tryGetConstant tupleExpr with
            | Some (tupleValue, tupleType) when FSharpType.IsTuple tupleType ->
                match tupleValue with
                | null -> simplified
                | value ->
                    let fieldValue = FSharpValue.GetTupleFields(value)[index]
                    let fieldType = FSharpType.GetTupleElements(tupleType)[index]
                    Expr.Value (fieldValue, fieldType)
                    |> simplify
            | _ -> simplified
        | Patterns.Coerce (inner, targetType) when inner.Type = targetType -> simplify inner
        | _ ->
            match trySimplifyFieldAccess simplified with
            | Some reduced -> simplify reduced
            | None -> simplified

    let readable (expr: Expr) : string =
        expr
        |> simplify
        |> toString

    let private failApply
        (expr: Expr)
        (message: string)
        : 'T =
        failwith $"{message}, Expr: {readable expr}"

    let apply (args: objnull list) (expr: Expr) : Expr =
        let substituteGroup (groupArgs: objnull list) (vars: Var list) (body: Expr) : Expr =
            let replacements =
                (vars, groupArgs)
                ||> List.map2 (fun variable value ->
                    variable, Expr.Value (value, variable.Type))
                |> List.toArray

            substituteByVar replacements body

        let rebuildLambdas (remainingGroups: Var list list) (body: Expr) : Expr =
            let buildGroup (vars: Var list) (body: Expr) : Expr =
                match vars with
                | [] ->
                    body
                | [ var ] ->
                    Expr.Lambda (var, body)
                | tupleVars ->
                    let tupleType =
                        tupleVars
                        |> List.map _.Type
                        |> List.toArray

                        |> FSharpType.MakeTupleType

                    let tupleVar = Var ("tupleArg", tupleType)

                    let replacements =
                        tupleVars
                        |> List.mapi (fun index variable ->
                            variable,
                            Expr.TupleGet (Expr.Var tupleVar, index))
                        |> List.toArray

                    let rewrittenBody =
                        substituteByVar replacements body

                    Expr.Lambda (tupleVar, rewrittenBody)

            List.foldBack buildGroup remainingGroups body

        match expr with
        | DerivedPatterns.Lambdas (argGroups, body) ->
            let rec loop
                (remainingArgs: objnull list)
                (remainingGroups: Var list list)
                (currentBody: Expr)
                : Expr =
                match remainingArgs, remainingGroups with
                | [], _ ->
                    rebuildLambdas remainingGroups currentBody
                | _, [] ->
                    failApply
                        expr
                        $"Too many arguments applied. Number of remaining arguments: \
                        {remainingArgs.Length}"
                | _, vars :: _ when remainingArgs.Length < vars.Length ->
                    failApply
                        expr
                        $"Not enough arguments to apply tupled argument group. \
                        Needed {vars.Length}, got {remainingArgs.Length}"
                | _, vars :: rest ->
                    let groupArgs, tailArgs =
                        remainingArgs
                        |> List.splitAt vars.Length

                    let nextBody =
                        substituteGroup groupArgs vars currentBody

                    loop tailArgs rest nextBody

            loop args argGroups body
        | _ ->
            if List.isEmpty args then expr
            else failApply expr "Expression is not a lambda and cannot be applied"

    let applyUntyped (args: objnull list) (expr: Expr<'T>) : Expr =
        apply args expr
