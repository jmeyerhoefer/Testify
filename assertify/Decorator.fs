module Decorator


open System


/// <summary>Provides utilities for decorating console output, such as applying colors, styles, and formatting to text strings.</summary>
type Decorator () =
    /// <summary>Gets the ANSI escape code for a foreground color.</summary>
    /// <param name="color">The console color for the foreground text.</param>
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


    /// <summary>Gets the ANSI escape code for a background color.</summary>
    /// <param name="color">The console color for the background text.</param>
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


    /// <summary>Applies a foreground color to the given message.</summary>
    /// <param name="color">The console color for the foreground text.</param>
    /// <param name="message">The message to be styled.</param>
    static member ForegroundColor (color: ConsoleColor) (message: string): string =
        $"\u001b[%d{foregroundColorCode color}m%s{message}\u001b[0m"


    /// <summary>Applies an RGB-based foreground color to the given message.</summary>
    /// <param name="redValue">The red component of the RGB color (0-255).</param>
    /// <param name="greenValue">The green component of the RGB color (0-255).</param>
    /// <param name="blueValue">The blue component of the RGB color (0-255).</param>
    /// <param name="message">The message to be styled.</param>
    static member ForegroundColorRGB (redValue: int, greenValue: int, blueValue: int) (message: string): string =
        $"\u001b[38;2;%d{redValue % 256};%d{greenValue % 256};%d{blueValue % 256}m%s{message}\u001b[0m"


    /// <summary>Applies a background color to the given message.</summary>
    /// <param name="color">The console color for the background.</param>
    /// <param name="message">The message to be styled.</param>
    static member BackgroundColor (color: ConsoleColor) (message: string): string =
        $"\u001b[%d{backgroundColorCode color}m%s{message}\u001b[0m"


    /// <summary>Applies an RGB-based background color to the given message.</summary>
    /// <param name="redValue">The red component of the RGB color (0-255).</param>
    /// <param name="greenValue">The green component of the RGB color (0-255).</param>
    /// <param name="blueValue">The blue component of the RGB color (0-255).</param>
    /// <param name="message">The message to be styled.</param>
    static member BackgroundColorRGB (redValue: int, greenValue: int, blueValue: int) (message: string): string =
        $"\u001b[48;2;%d{redValue % 256};%d{greenValue % 256};%d{blueValue % 256}m%s{message}\u001b[0m"


    /// <summary>Applies both foreground and background colors to the given message.</summary>
    /// <param name="foregroundColor">The console color for the foreground text.</param>
    /// <param name="backgroundColor">The console color for the background.</param>
    /// <param name="message">The message to be styled.</param>
    static member ForeAndBackgroundColor (foregroundColor: ConsoleColor) (backgroundColor: ConsoleColor) (message: string): string =
        $"\u001b[%d{foregroundColorCode foregroundColor};%d{backgroundColorCode backgroundColor}m%s{message}\u001b[0;0m"


    /// <summary>Makes the given message bold.</summary>
    /// <param name="message">The message to be styled.</param>
    static member Bold (message: string): string =
        $"\u001b[1m%s{message}\u001b[0m"


    /// <summary>Makes the given message italicized.</summary>
    /// <param name="message">The message to be styled.</param>
    static member Italic (message: string): string =
        $"\u001b[3m%s{message}\u001b[0m"


    /// <summary>Underlines the given message.</summary>
    /// <param name="message">The message to be styled.</param>
    static member Underline (message: string): string =
        $"\u001b[4m%s{message}\u001b[0m"


    /// <summary>Returns an emoji representation of a number.</summary>
    /// <param name="number">The number to be converted to an emoji (0-10).</param>
    static member GetNumberEmoji (number: int): string =
        let lookup: string array = [| "0️⃣"; "1️⃣"; "2️⃣"; "3️⃣"; "4️⃣"; "5️⃣"; "6️⃣"; "7️⃣"; "8️⃣"; "9️⃣"; "🔟" |]

        if 0 <= number && number <= 10 then
            lookup[number]
        else
            "#️⃣"


    /// <summary>Returns the ordinal indicator (e.g., "1st", "2nd", "3rd") for a given number.</summary>
    /// <param name="number">The number to be converted to an ordinal string.</param>
    static member GetOrdinalIndicator (number: int): string =
        let suffix: string =
            match number % 100 with
            | 11 | 12 | 13 -> "th"
            | _ ->
                match number % 10 with
                | 1 -> "st"
                | 2 -> "nd"
                | 3 -> "rd"
                | _ -> "th"

        $"%d{number}%s{suffix}"