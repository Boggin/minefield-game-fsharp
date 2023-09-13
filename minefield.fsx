open System

type Position = Position of x: int * y: int

type Player =
    { Lives: int
      Position: Position }

type Board = { Limit: Position }

type Mine = { Position: Position }

type Moved =
    | Moved of Player
    | Exploded of Player
    | OutOfBounds of Player

type Game =
    { Player: Player
      Mines: Mine list
      Board: Board
      History: Moved list }

type GameState =
    | Active of Game
    | Won of Game
    | Lost of Game

type Direction =
    | Up
    | Down
    | Left
    | Right

let player =
    { Lives = 3
      Position = Position(x = 0, y = 0) }

// 8x8 chessboard.
let board = { Limit = Position(x = 7, y = 7) }

let placeMines (Position(x = limitX; y = limitY)) (n: int) =
    let random = Random()

    let rec placeMines' (mines: Mine list) =
        if mines.Length = n then
            mines
        else
            let row = random.Next(0, limitX + 1)
            let col = random.Next(0, limitY + 1)

            let position = Position(x = row, y = col)

            let mine = { Position = position }

            if mines |> List.exists (fun mine -> mine.Position = position) then
                placeMines' mines
            else
                placeMines' (mine :: mines)

    placeMines' []

let setup =
    Active
        { Player = player
          Mines = placeMines board.Limit 12
          Board = board
          History = [] }

let nextPosition (Position(x = row; y = col)) direction =
    match direction with
    | Left -> Position(x = row - 1, y = col)
    | Down -> Position(x = row, y = col - 1)
    | Up -> Position(x = row, y = col + 1)
    | Right -> Position(x = row + 1, y = col)

let moved game position =
    let isMine position =
        game.Mines |> List.exists (fun mine -> mine.Position = position)

    let isOutOfBounds
        (Position(x = row; y = col))
        (Position(x = limitX; y = limitY))
        =
        row < 0 || row > limitX || col < 0 || col > limitY

    if isOutOfBounds position game.Board.Limit then
        OutOfBounds player
    elif isMine position then
        Exploded
            { player with
                Lives = player.Lives - 1
                Position = position }
    else
        Moved { player with Position = position }

let move game direction =
    let player' = direction |> nextPosition player.Position |> moved game

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

let display gameState =
    let getGame =
        function
        | Active game
        | Won game
        | Lost game -> game

    let game = getGame gameState

    if game.History.Length > 0 then
        let lastMove = game.History |> List.head

        match lastMove with
        | Moved _ -> printfn "You moved."
        | Exploded _ -> printfn "You exploded!"
        | OutOfBounds _ -> printfn "You went out of bounds!"

    let getChessNotation (Position(x = row; y = col)) =
        sprintf "%c%i" (char (col + 65)) (row + 1)

    printfn
        "Lives: %i, Moves: %i, Position: %s"
        game.Player.Lives
        game.History.Length
        (getChessNotation game.Player.Position)

    match gameState with
    | Active _ -> ()
    | Won _ -> printfn "You Won!"
    | Lost _ -> printfn "You Lost!"

exception InvalidDirection of string

let start =
    printfn
        $"""
    Cross the Minefield.
    You start in the bottom, left-hand corner.
    You must exit from the top, right-hand corner.
    Left: a. Down: s. Up: w. Right: d."""

    let rec play gameState =

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

        let gameState' = getDirection () |> update gameState
        display gameState'

        match gameState' with
        | Active _ -> play gameState'
        | Won _
        | Lost _ -> ()

    play setup
    printfn "Game Over"

start
