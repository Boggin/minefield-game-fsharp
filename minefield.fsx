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

let mines =
    [ { Position =
          { x = (Row 1)
            y = (Col 1) } }
      { Position =
          { x = (Row 2)
            y = (Col 2) } } ]

let player =
    { Lives = 3
      Position =
        { x = (Row 0)
          y = (Col 0) } }

let board =
    { Limit =
        { x = (Row 8)
          y = (Col 8) } }

let moved (player: Player) (position: Position) mines =
    let isMine (position: Position) =
        mines |> List.exists (fun mine -> mine.Position = position)

    let isOutOfBounds (position: Position) =
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

let nextPosition (position: Position) (direction: Direction) : Position =
    let addCol (Col col) (Col n) = Col(col + n)
    let addRow (Row row) (Row n) = Row(row + n)

    match direction with
    | Left -> { position with x = addRow position.x (Row -1) }
    | Down -> { position with y = addCol position.y (Col -1) }
    | Up -> { position with y = addCol position.y (Col 1) }
    | Right -> { position with x = addRow position.x (Row 1) }

let move (game: Game) (direction: Direction) : Game =
    let player' =
        nextPosition game.Player.Position direction |> moved game.Player <| game.Mines

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
    | OutOfBounds _ -> game

let setup: GameState =
    Active
        { Player = player
          Mines = mines
          Board = board
          History = [] }

let update (gameState: GameState) (direction: Direction) : GameState =
    let hasDied player = player.Lives = 0
    let hasEscaped board (player: Player) = player.Position = board.Limit

    match gameState with
    | Active game ->
        let game' = move game direction

        if game'.Player |> hasDied then Lost game'
        elif game'.Player |> hasEscaped game'.Board then Won game'
        else Active game'
    | Won _
    | Lost _ -> gameState
