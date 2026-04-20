module Mini

// type definitions

type Array<'a> = 'a array

type Bool = bool

type Char = char

type Double = double

type Exception = exn

type Int = int

type Integer = bigint

type String = string

type Unit = unit

// The type of lists is predefined in F#.
// type List<'a> = Nil | Cons of 'a * List<'a>

// exception handling

exception Error of String

/// raises an exception with the given error message
let error<'T>(s : string) : 'T = raise (Error s)

exception Panic of String

/// simply panics
let panic<'T>(s : string) : 'T = raise (Panic s)

// strings

/// Convert a string into a list of characters.
let explode (s : String) =
    [for c in s -> c]

/// Convert a list of characters into a string.
let implode (xs : char list) =
    let sb = System.Text.StringBuilder(xs.Length)
    xs |> List.iter (sb.Append >> ignore)
    sb.ToString()

// input and output

/// Outputs a single character to the console
let putchar (c : char): unit = System.Console.Write(c)
/// reads a single character from the console
let getchar (): char = System.Console.ReadKey().KeyChar

/// writes a string to the console
let putstring (s: string): unit = printf "%s" s
/// writes a string to the console and starts a new line
let putline (s: string): unit    = printfn "%s" s

exception EOF

/// Reads a line from the console.
/// Raises an EOF exception if no more line is available.
let getline (): string =
    match System.Console.ReadLine () with
    | null -> raise EOF
    | s -> s

// file input and output

let readFromFile (filepath : String) =
    System.IO.File.ReadAllText filepath
let writeToFile (filepath : String, contents : String) =
    System.IO.File.WriteAllText (filepath, contents)

/// Converts a value to a string representation
let show<'T> (a: 'T): string = sprintf "%A" a
/// writes a string to the console and starts a new line
let print<'T> (a: 'T): unit = printfn "%A" a

exception Div
exception Mod

/// The type Nat represents natural numbers starting at 0.
[<StructuralEquality;StructuralComparison;StructuredFormatDisplay("{n}N")>]
type Nat =
    private | Nat of n: bigint

    /// create a natural number from a bigint value
    static member Make(n : bigint): Nat =
        if n < 0I then failwithf "Cannot convert negative number into natural number: %A" n
        else Nat n

    /// create a natural number from an int value
    static member Make(n : int): Nat = Nat.Make (bigint n)

    /// converts to bigint
    member self.ToBigInteger(): bigint =
        let (Nat n) = self in n
    //// converts this number to a string
    override self.ToString(): string =
        let (Nat n) = self in n.ToString()
    // backwards compatibility, remove for WS19/20
    member self.ToString' = self.ToString

    /// cast to int
    static member op_Explicit(x: Nat): int =
        match x with
        | Nat i -> int i

    /// cast to float
    static member op_Explicit(x: Nat): float =
        match x with
        | Nat i -> float i

    /// cast to byte
    static member op_Explicit(x: Nat): byte =
        match x with
        | Nat i -> byte i

    /// cast to bigint
    static member op_Explicit(x: Nat): bigint =
        match x with
        | Nat i -> i

    /// Parse a string to a natural number.
    /// Raises a System.FormatException if the number is not correctly formatted.
    static member Parse (s : String): Nat =
        if s.Length = 0 then
            raise (System.FormatException "The value could not be parsed.")
        elif s.[s.Length - 1] = 'N' then
            Nat.Make (System.Numerics.BigInteger.Parse s.[0 .. s.Length - 2])
        else
            Nat.Make (System.Numerics.BigInteger.Parse s)

    /// The neutral element for addition (used by some generic functions)
    static member Zero = Nat 0I
    /// The neutral element for multiplication (used by some generic functions)
    static member One = Nat 1I
    /// Addition of two natural numbers
    static member (+) (Nat a, Nat b) = Nat (a + b)
    /// Substraction of two natural numbers.
    /// If b > a then a - b = 0.
    static member (-) (Nat a, Nat b) = Nat.Make(max 0I (a - b))
    /// Multiplication of two natural numbers
    static member (*) (Nat a, Nat b) = Nat (a * b)
    /// Floored division (e.g. 11 / 3 = 3)
    static member (/) (Nat a, Nat b) =
        try Nat (a / b) with :? System.DivideByZeroException -> raise Div
    /// Remainder of division (Modulo) for natural numbers (e.g. 11 % 3 = 2)
    static member (%) (Nat a, Nat b) =
        try Nat (a % b) with :? System.DivideByZeroException -> raise Mod
    /// exponentiation, to the power of
    static member Pow (Nat a, Nat b) = pown (Nat a) (int b)

    /// Convert a char to a natural number
    static member ord (c : Char): Nat = Nat (bigint (int c))

/// Module for writing natural numbers with an "N" postfix (e.g 0N, 99N, ...)
module NumericLiteralN =
    let FromZero () = Nat 0I
    let FromOne () = Nat 1I
    let FromInt32 (a : int) = Nat.Make(bigint(a))
    let FromInt64 (a : int64) = Nat.Make(bigint(a))
    let FromString (s : string) = Nat.Make(System.Numerics.BigInteger.Parse(s))

exception Read

/// Converts a string to a natural number.
/// Raises a Read exception if the string is not formatted correctly.
let readNat (s : String): Nat =
    try Nat.Parse s with
        | :? System.FormatException -> raise Read
