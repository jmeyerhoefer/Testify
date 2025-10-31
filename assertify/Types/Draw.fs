module Types.Draw


open System
open Types.TurtleTypes


let rec iterate<'a> (f: 'a -> 'a) (n: Nat) (x: 'a) =
    if n = 0N then x
    else iterate f (n - 1N) (f x)

let rec consfirst<'a> (elem: 'a) (xs: List<List<'a>>): List<List<'a>> =
    match xs with
    | [] -> [[elem]]
    | x::xs -> (elem :: x) :: xs

let deg2rad (x: Double): Double =
    (float x) * Math.PI / 180.0

// convert to lines
let rec convert (p: Program): List<List<Double * Double>> =
    let rec h (p: Program) (offset: Double * Double) (ang: Double) (dropped: Bool) (acc: List<List<Double * Double>>) =
        match p with
        | [] -> consfirst offset acc
        | D::ps -> h ps offset ang true acc
        | (F len)::ps ->
            let dx = (cos (deg2rad ang) * (float len))
            let dy = (sin (deg2rad ang) * (float len))
            let offset' = (fst offset + dx, snd offset + dy)
            let acc' = if dropped then consfirst offset acc else acc
            h ps offset' ang dropped acc'
        | (L dir)::ps -> h ps offset ((ang - dir) % 360.0) dropped acc
    h p (0.0, 0.0) 0.0 false []

let draw (p: Program) =
    let lineCoords = convert p

    let paths =
        [ for line in lineCoords do
            [ for point in line do
                yield $"%f{fst point},%f{snd point}" ] ]

    let ht = List.map (fun s -> (List.head s, String.concat " " (List.tail s))) paths
    let svg = String.concat "\n" (List.map (fun x -> $"<path d=\"M %s{fst x} L %s{snd x}\" />") ht)

    // calculate viewBox (x,y,width,height)
    let all = List.concat lineCoords
    let (minx, maxx, miny, maxy) = (List.minBy fst all |> fst, List.maxBy fst all |> fst, List.minBy snd all |> snd, List.maxBy snd all |> snd)
    let viewBox = $"%d{int minx - 10} %d{int miny - 10} %d{int (maxx - minx) + 20} %d{int (maxy - miny) + 20}"

    let template =
      sprintf
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n\
         <svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" version=\"1.1\" baseProfile=\"full\" viewBox=\"%s\" stroke-width=\"0.5\" fill=\"white\" stroke=\"#000\">\n\
         %s\n\
         </svg>"
    System.IO.File.WriteAllText("image.svg", template viewBox svg)
    ()