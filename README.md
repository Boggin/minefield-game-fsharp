# Minefield Game

## Introduction

The game can be played from the terminal:  
`dotnet fsi .\minefield.fsx`

## The Brief

An engineering company is using the Minefield game as a screen for their software engineering hires.

"In the game a player navigates from one side of a chessboard grid to the other whilst trying to avoid hidden mines. The player has a number of lives, losing one each time a mine is hit, and the final score is the number of moves taken in order to reach the other side of the board.  The command line / console interface should be simple, allowing the player to input move direction (up, down, left, right) and the game to show the resulting position (e.g. C2 in chess board terminology) along with number of lives left and number of moves taken."

They aren't looking for the game to be developed so much as to give a framework to show that the software engineers can apply a full test harness with dependency injection, etc.. This naturally leads to a lot of "Enterprise FizzBuzz" code that is quite horrible to see. The company in question asks the developers to post their efforts on Github in public repos so if you want to you can easily find examples of just how terrible this code looks.

## Why F#?

F# has a number of features that makes it much easier to model a domain, with events, in a safe manner. This code illustrates these features in places.

## Records

Elements like the Player, the Board and the Mines can be created as F# Records. The whole of the Game can be a Record, too.

```fsharp
type Game =
    { Player: Player
      Mines: Mine list
      Board: Board
      History: Moved list }
```

## Types

The state changes can be modelled as Discriminated Unions. For instance, the player's move will either be safe, result in a bang, or be off the board (i.e. not allowed).

```fsharp
type Moved =
    | Moved of Player
    | Exploded of Player
    | OutOfBounds of Player
```

The whole game state can be easily tracked with another Discriminated Union (DU):

```fsharp
type GameState =
    | Active of Game
    | Won of Game
    | Lost of Game
```

These states are things that would be tracked with flags in your code, like `isGameOver` or `hasWon`. The advantage with the type system is we can use the compiler to check that we've handled all the cases.

Once we've thought through a few Records and DU Types with which to model our domain we have a clear framework to hang our code on.

When we want to update the game with the player's move we can use the GameState type:

```fsharp
let update (gameState: GameState) (direction: Direction) : GameState =
    let hasDied player = player.Lives = 0
    let hasEscaped board (player: Player) = player.Position = board.Limit

    match gameState with
    | Active game ->
        let game' = move game direction

        if game'.Player |> hasDied then Lost game'
        elif game'.Player |> hasEscaped game'.Board then Won game'
        else Active game'
    | Won _ -> gameState
    | Lost _ -> gameState
```

 Note that Won and Lost here aren't doing anything but returning the game state. They wouldn't be called if the game was won or lost but the compiler is checking for them. We could just ignore these other elements of the DU but that's giving up an advantage of the language. So why have the Won and Lost cases at all?

 ```fsharp
let display gameState=
    match gameState with
    | Active game -> display' game
    | Won game ->
        display' game
        printfn "You Won!"
    | Lost game ->
        display' game
        printfn "You Lost!"
```

The domain logic isn't coupled to the UI logic but the UI, such as it is, can still use the different cases.

This code example also shows the simple power of pattern matching. We can rest easy knowing that all of the cases must be handled or the compiler will flag the error. We can also simply decompose the type to use the sub-types, for instance, to get the Game from the GameState.

```fsharp
type Row = Row of int
type Col = Col of int

type Position = { x: Row y: Col }
```

It may be taking it a little far but I've even created types for a column and the row for each position on the board. Now we have to explicitly use row and column types when we create a position. It's a good example of how the type system can help us guard against silly errors.

## Immutability

When we need to update the game state, say when the player has exploded and lost a life, we have to create a new game state:

```fsharp
    elif isMine position then
        Exploded
            { player with
                Lives = player.Lives - 1
                Position = position }
```

We have a guarantee that no part of our code can reach into the Lives field and change it without our being aware of it. You can also see from the given code example above that in using the `with` keyword we can easily create a new record with only the values updated that we explicitly want changed.

## Recursion

It's the basis of a great deal of game code to have the game loop. If this were in an imperative language we'd have a `while` loop that would run until the game was over. In F# we can use recursion to achieve the same thing. We can even have inner loops by having recursive functions inside other recursive functions.

```fsharp
      let rec getDirection () =
          try
              let key = Console.ReadKey().KeyChar

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
```

In the above code we even have our own custom exception, InvalidDirection, that we can throw and catch to handle an error. The recursion, in this case, can continue.

```fsharp
exception InvalidDirection of string
```

