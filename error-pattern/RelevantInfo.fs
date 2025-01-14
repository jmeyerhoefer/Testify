module RelevantInfo


open System.IO


/// <summary>The relevant tasks for GdP18 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP18: TaskInfo list= [
    // TODO
    TaskInfo ("GdP18", "sheetId", "assignmentId", "assignmentTitle", "projectFileName", "relevantFileName")
]


/// <summary>The relevant tasks for GdP19 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP19: TaskInfo list = [
    // TODO
    TaskInfo ("GdP19", "sheetId", "assignmentId", "assignmentTitle", "projectFileName", "relevantFileName")
]


/// <summary>The relevant tasks for GdP20 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP20: TaskInfo list = [
    // TODO
    TaskInfo ("GdP20", "sheetId", "assignmentId", "assignmentTitle", "projectFileName", "relevantFileName")
]


/// <summary>The relevant tasks for GdP21 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP21: TaskInfo list = [
    // TODO
    TaskInfo ("GdP21", "sheetId", "assignmentId", "assignmentTitle", "projectFileName", "relevantFileName")
]


/// <summary>The relevant tasks for GdP22 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP22: TaskInfo list = [
    // TODO
    TaskInfo ("GdP22", "sheetId", "assignmentId", "assignmentTitle", "projectFileName", "relevantFileName")
]


/// <summary>The relevant tasks for GdP23 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP23: TaskInfo list = [
    TaskInfo ("GdP23", "02", "4", "Programmieren mit Zahlen", "Zahlen.fsproj", "Zahlen.fs")
    TaskInfo ("GdP23", "03", "2", "Peano Entwurfsmuster", "Peano.fsproj", "Peano.fs")
    TaskInfo ("GdP23", "03", "3", "Leibniz Entwurfsmuster", "Leibniz.fsproj", "Leibniz.fs")
    TaskInfo ("GdP23", "04", "2", "Kalenderdaten", "Dates.fsproj", "Dates.fs")
    TaskInfo ("GdP23", "04", "3", "Listen natürlicher Zahlen", "Nats.fsproj", "Nats.fs")
    TaskInfo ("GdP23", "05", "2", "Prioritätswarteschlange", "PriorityQueue.fsproj", "PriorityQueue.fs")
    TaskInfo ("GdP23", "05", "3", "Ausdrücke vereinfachen", "Simplify.fsproj", "Simplify.fs")
    TaskInfo ("GdP23", "06", "3", "Heaps", "Heaps.fsproj", "Heaps.fs")
    TaskInfo ("GdP23", "07", "2", "Endliche Abbildungen 1", "Map.fsproj", "MapSortedList.fs")
    TaskInfo ("GdP23", "07", "3", "Endliche Abbildungen 2", "Map.fsproj", "MapPartialFunction.fs")
    // TaskInfo ("GdP23", "08", "2", "Datentypen", "Datatypes.fsproj", "Datatypes.fs")
    TaskInfo ("GdP23", "08", "3", "Reguläre Ausdrücke", "RegExp.fsproj", "RegExp.fs")
    TaskInfo ("GdP23", "09", "2", "Black Jack", "BlackJack.fsproj", "BlackJack.fs")
    TaskInfo ("GdP23", "10", "4", "Veränderbare Listen", "Lists.fsproj", "Lists.fs")
    TaskInfo ("GdP23", "11", "4", "Arrays und Zustand", "Arrays.fsproj", "Arrays.fs")
    TaskInfo ("GdP23", "12", "2", "Warteschlangen", "Queues.fsproj", "Queues.fs")
]


/// <summary>The relevant tasks for GdP24 for analysis stored as <c>TaskInfo</c>.</summary>
let relevantTasksGdP24: TaskInfo list = [
    // TODO
    TaskInfo ("GdP24", "sheetId", "assignmentId", "assignmentTitle", "projectFileName", "relevantFileName")
]


/// <summary>All relevant tasks for analysis stored as <c>TaskInfo</c></summary>
let allRelevantTasks: TaskInfo list =
    relevantTasksGdP18
    @ relevantTasksGdP19
    @ relevantTasksGdP20
    @ relevantTasksGdP21
    @ relevantTasksGdP22
    @ relevantTasksGdP23
    // @ relevantTasksGdP24


/// <summary>
/// TODO
/// </summary>
/// <param name="exerciseId">ID of the exercise.</param>
let getStacktracePath (exerciseId: string): string =
    Path.Combine (RootPath, "data", "Exercises", exerciseId, "Stacktrace")


/// <summary>
/// TODO
/// </summary>
/// <param name="exerciseId">ID of the exercise.</param>
let getStatisticsPath (exerciseId: string): string =
    Path.Combine (RootPath, "data", "Exercises", exerciseId, "Statistics")


/// <summary>
/// TODO
/// </summary>
/// <param name="exerciseId">TODO</param>
let getRelevantTasks (exerciseId: string): TaskInfo list =
    match exerciseId with
        | "GdP18" -> relevantTasksGdP18
        | "GdP19" -> relevantTasksGdP19
        | "GdP20" -> relevantTasksGdP20
        | "GdP21" -> relevantTasksGdP21
        | "GdP22" -> relevantTasksGdP22
        | "GdP23" -> relevantTasksGdP23
        | "GdP24" -> relevantTasksGdP24
        | "All" -> allRelevantTasks
        | _ -> []



// EOF