module Student.Reduktionssemantik


open Types.ReduktionssemantikTypes


////a)
let rec isWord<'a>(r: RegEx<'a>): Option<List<'a>> =
    failwith "TODO"

////b)
let rec reduceStep<'a>(r: RegEx<'a>): List<RegEx<'a>> =
    failwith "TODO"

////c)
let rec reduce<'a when 'a: equality>(r: RegEx<'a>) (n: Nat): List<RegEx<'a>> =
    failwith "TODO"

////d)
let rec words<'a when 'a: equality>(r: RegEx<'a>) (n: Nat): List<List<'a>> =
    failwith "TODO"

////e)
let rec generates<'a when 'a: equality>(r: RegEx<'a>) (word: List<'a>) (n: Nat): Bool =
    failwith "TODO"
