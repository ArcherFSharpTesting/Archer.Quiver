﻿module Archer.Quiver.TestAdapter.TestCaseCache

open Archer.CoreTypes.InternalTypes

let private cache = System.Collections.Generic.Dictionary<string, ITest>()

let addCache (test: ITest) =
    let key = (test |> getTestFullName)
    cache[key] <- test
    test
    
let getFromCache (name: string) =
    cache[name]