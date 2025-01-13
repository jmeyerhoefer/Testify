module Decorator


open System


type Decorator () =
    static let foregroundColorCode (color: ConsoleColor): int =
        match color with
            | ConsoleColor.Black    -> 30
            | ConsoleColor.Red      -> 31
            | ConsoleColor.Green    -> 32
            | ConsoleColor.Yellow   -> 33
            | ConsoleColor.Blue     -> 34
            | ConsoleColor.Magenta  -> 35
            | ConsoleColor.Cyan     -> 36
            | ConsoleColor.White    -> 37
            | _ -> 0

    static let backgroundColorCode (color: ConsoleColor): int =
        match color with
            | ConsoleColor.Black    -> 40
            | ConsoleColor.Red      -> 41
            | ConsoleColor.Green    -> 42
            | ConsoleColor.Yellow   -> 43
            | ConsoleColor.Blue     -> 44
            | ConsoleColor.Magenta  -> 45
            | ConsoleColor.Cyan     -> 46
            | ConsoleColor.White    -> 47
            | _ -> 0

    static member ForegroundColor (color: ConsoleColor) (message: string): string =
        $"\u001b[%d{foregroundColorCode color}m%s{message}\u001b[0m"

    static member ForegroundColorRGB (redValue: int, greenValue: int, blueValue: int) (message: string): string =
        $"\u001b[38;2;%d{redValue % 256};%d{greenValue % 256};%d{blueValue % 256}m%s{message}\u001b[0m"

    static member BackgroundColor (color: ConsoleColor) (message: string): string =
        $"\u001b[%d{backgroundColorCode color}m%s{message}\u001b[0m"

    static member BackgroundColorRGB (redValue: int, greenValue: int, blueValue: int) (message: string): string =
        $"\u001b[48;2;%d{redValue % 256};%d{greenValue % 256};%d{blueValue % 256}m%s{message}\u001b[0m"

    static member ForeAndBackgroundColor (foregroundColor: ConsoleColor) (backgroundColor: ConsoleColor) (message: string): string =
        $"\u001b[%d{foregroundColorCode foregroundColor};%d{backgroundColorCode backgroundColor}m%s{message}\u001b[0;0m"

    static member Bold (message: string): string =
        $"\u001b[1m%s{message}\u001b[0m"

    static member Italic (message: string): string =
        $"\u001b[3m%s{message}\u001b[0m"

    static member Underline (message: string): string =
        $"\u001b[4m%s{message}\u001b[0m"