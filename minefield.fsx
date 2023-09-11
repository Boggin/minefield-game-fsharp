open System

type Row = Row of int
type Col = Col of int

type Position =
    { x: Row
      y: Col }

type Player =
    { Lives: int
      Position: Position }

type Board = { Limit: Position }

type Mine = { Position: Position }

type Moved =
    | Moved of Player
    | Exploded of Player
    | OutOfBounds of Player

type Direction =
    | Up
    | Down
    | Left
    | Right

type Game =
    { Player: Player
      Mines: Mine list
      Board: Board
      History: Moved list }

type GameState =
    | Active of Game
    | Won of Game
    | Lost of Game

let player =
    { Lives = 3
      Position =
        { x = (Row 0)
          y = (Col 0) } }

// 8x8 chessboard.
let board =
    { Limit =
        { x = (Row 7)
          y = (Col 7) } }

let placeMines (board: Board) (n: int) =
    let addRow (Row row) = row + 1
    let addCol (Col col) = col + 1
    let random = Random()

    let rec placeMines' (mines: Mine list) =
        if mines.Length = n then
            mines
        else
            let row = random.Next(0, addRow board.Limit.x)
            let col = random.Next(0, addCol board.Limit.y)

            let position =
                { x = (Row row)
                  y = (Col col) }

            let mine = { Position = position }

            if mines |> List.exists (fun mine -> mine.Position = position) then
                placeMines' mines
            else
                placeMines' (mine :: mines)

    placeMines' []

let setup =
    Active
        { Player = player
          Mines = placeMines board 12
          Board = board
          History = [] }

let nextPosition position direction =
    let addCol (Col col) (Col n) = Col(col + n)
    let addRow (Row row) (Row n) = Row(row + n)

    match direction with
    | Left -> { position with x = addRow position.x (Row -1) }
    | Down -> { position with y = addCol position.y (Col -1) }
    | Up -> { position with y = addCol position.y (Col 1) }
    | Right -> { position with x = addRow position.x (Row 1) }

let moved player position mines =
    let isMine position =
        mines |> List.exists (fun mine -> mine.Position = position)

    let isOutOfBounds position =
        position.x < (Row 0)
        || position.x > board.Limit.x
        || position.y < (Col 0)
        || position.y > board.Limit.y

    if isOutOfBounds position then
        OutOfBounds player
    elif isMine position then
        Exploded
            { player with
                Lives = player.Lives - 1
                Position = position }
    else
        Moved { player with Position = position }

let move game direction =
    let player = game.Player

    let player' =
        direction |> nextPosition player.Position |> moved player <| game.Mines

    match player' with
    | Moved player ->
        { game with
            Player = player
            History = player' :: game.History }
    | Exploded player ->
        // Remove the mine that exploded.
        { game with
            Player = player
            Mines =
                game.Mines
                |> List.filter (fun mine -> mine.Position <> player.Position)
            History = player' :: game.History }
    | OutOfBounds _ -> { game with History = player' :: game.History }

let update (gameState: GameState) (direction: Direction) : GameState =
    let hasDied player = player.Lives = 0
    let hasEscaped board (player: Player) = player.Position = board.Limit

    match gameState with
    | Active game ->
        let game' = move game direction

        if game'.Player |> hasDied then Lost game'
        elif game'.Player |> hasEscaped game'.Board then Won game'
        else Active game'
    | _ -> gameState

let display' game =
    if game.History.Length > 0 then
        let lastMove = game.History |> List.head

        match lastMove with
        | Moved _ -> printfn "You moved."
        | Exploded _ -> printfn "You exploded!"
        | OutOfBounds _ -> printfn "You went out of bounds!"

    let addRow (Row row) = row + 1
    let getChessNotation (Col col) = char (col + 65)

    printfn
        "Lives: %i, Moves: %i, Position: %O%i"
        game.Player.Lives
        game.History.Length
        (getChessNotation game.Player.Position.y)
        (addRow game.Player.Position.x)

let display gameState =
    match gameState with
    | Active game -> display' game
    | Won game ->
        display' game
        printfn "You Won!"
    | Lost game ->
        display' game
        printfn "You Lost!"

exception InvalidDirection of string

let start =
    printfn
        $"""
    Cross the Minefield.
    You start in the bottom, left-hand corner.
    You must exit from the top, right-hand corner.
    Left: a. Down: s. Up: w. Right: d."""

    let rec play game =

        let rec getDirection () =
            try
                let key = Console.ReadKey().KeyChar
                printfn ""

                match key with
                | 'w' -> Up
                | 'a' -> Left
                | 's' -> Down
                | 'd' -> Right
                | _ -> raise (InvalidDirection "Invalid direction")
            with e ->
                printfn "%s. Try again!" e.Message
                getDirection ()

        let game' = getDirection () |> update game

        match game' with
        | Active _ ->
            display game'
            play game'
        | Won _
        | Lost _ -> display game'

    play setup
    printfn "Game Over"

start
