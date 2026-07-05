<div id="top"></div>

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]

<!-- PROJECT LOGO -->
<br />
<div align="center">

<h3 align="center">Othello Engine</h3>
  <a href="https://github.com/chrkj/OthelloEngine">
    <img src="/Example.png" width="700" height="366">
  </a>
  <p align="center">
    An Othello engine with minimax, multithreaded MCTS and GPU-accelerated rollouts
    <br />
    <a href="https://github.com/chrkj/OthelloEngine"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://github.com/chrkj/OthelloEngine/issues">Report Bug</a>
    ·
    <a href="https://github.com/chrkj/OthelloEngine/issues">Request Feature</a>
  </p>
</div>

<!-- ABOUT THE PROJECT -->
## About The Project

This is an implementation of Othello (also known as Reversi), a strategy board game for two players played on an 8 x 8 board.
I originally created the project to learn more about game AI. It has since grown into a small workbench for comparing search algorithms against each other: any combination of Human, Minimax, MCTS or Random play can be matched up, watched live with per-engine diagnostics, and benchmarked over automated batches of games.

<p align="right">(<a href="#top">back to top</a>)</p>

## Features

* Human vs. AI, AI vs. AI or Human vs. Human play
* Four selectable engines per color: Human, Minimax, MCTS and Random
* Per-engine settings in the UI: search depth, time limit, iteration cap, move ordering, iterative deepening and transposition table toggles, MCTS variant selection
* Live search diagnostics: search time, positions evaluated, branches pruned, tree size, simulations run and win prediction
* Automated match simulation — run N games back-to-back and track wins/draws per engine
* Auto-move toggle, or step through AI moves manually with Space
* Legal move highlighting, last-move highlighting and a color-coded in-game console log

<p align="right">(<a href="#top">back to top</a>)</p>

## Search Engines

### Minimax
A classic [minimax](https://en.wikipedia.org/wiki/Minimax) search with alpha-beta pruning and a positional (cell weight) evaluation function. Optional features, toggleable from the UI:

* **Iterative deepening** with best-move-first ordering between iterations
* **Move ordering** by positional weight to improve pruning
* **Transposition table** for caching evaluated positions (with a configurable memory cap)

### Monte Carlo Tree Search
A [Monte Carlo Tree Search](https://en.wikipedia.org/wiki/Monte_Carlo_tree_search) engine with UCT selection and four interchangeable execution strategies:

* **Sequential** — single-threaded baseline
* **Root parallel** — iterations distributed over all cores with a shared tree
* **Tree parallel** — one dedicated task per root child, each searching its own subtree
* **GPU parallel** — playout simulation runs on the GPU via a compute shader. When a node is expanded, all of its children are simulated in a single dispatch: one thread group per child board, 128 random rollouts per group, results reduced in group-shared memory. Batching the whole expansion into one dispatch amortizes the CPU↔GPU round-trip latency that would otherwise dominate.

Tree statistics are updated with interlocked operations so the parallel variants back-propagate race-free, and the search tree is reused between moves.

<p align="right">(<a href="#top">back to top</a>)</p>

## Board Representation

The board is stored as two 64-bit bitboards (`ulong`), one per color — 16 bytes per position, which keeps MCTS memory pressure low and makes copies trivial. On top of that:

* Legal move generation is a branch-free shift-and-mask flood fill across all 8 directions
* Piece counting uses a SWAR popcount (and the `countbits` intrinsic on the GPU)
* The same bitboard algorithms are mirrored in HLSL so CPU and GPU rollouts share identical game logic

<p align="right">(<a href="#top">back to top</a>)</p>

## Project Structure

```
Assets/
├── Core/   Othello.Core — pure C# game engine: board, bitboards, moves (no Unity dependencies)
├── AI/     Othello.AI   — search engines: MiniMax, Mcts (+ compute shader), RandomPlay, SearchResult
├── App/    Othello.App  — Unity orchestration: GameManager, player types, game loop
└── UI/     Othello.UI   — board rendering, menus, console log
```

Dependencies point downward only: the engine layers know nothing about Unity's UI, and engines report their results through a `SearchResult` rather than shared state.

<p align="right">(<a href="#top">back to top</a>)</p>

## Getting Started

1. Clone the repository
   ```sh
   git clone https://github.com/chrkj/OthelloEngine.git
   ```
2. Open the project in Unity **2022.3 LTS** (developed with 2022.3.19f1)
3. Open `Assets/Scenes/GameScene.unity` and press Play
4. Pick an engine for each color, adjust the settings and start a new game

The GPU MCTS variant requires a platform with compute shader support.

<p align="right">(<a href="#top">back to top</a>)</p>

### Built With

Frameworks/libraries used for the project:

* [Unity](https://unity.com/) (2022.3 LTS)
* HLSL compute shaders for GPU playouts
* [TextMesh Pro](https://docs.unity3d.com/Manual/com.unity.textmeshpro.html) for the UI

<!-- ROADMAP -->
## Roadmap

Engine:
- ✅ Bitboard board representation with flood-fill move generation
- ✅ Iterative deepening, move ordering and transposition table for MiniMax
- ✅ Multithreaded MCTS (root parallel and tree parallel)
- ✅ GPU-accelerated MCTS rollouts with batched dispatches
- ⬜ Weighted backpropagation of GPU rollout counts
- ⬜ Mobility term in the minimax evaluation function
- ⬜ Unit tests (perft, evaluation, CPU/GPU rollout parity)

UI:
- ✅ Better UI
- ✅ Support for color coded console log
- ✅ Implement search algorithm diagnostics console

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- MARKDOWN LINKS & IMAGES -->
[contributors-shield]: https://img.shields.io/github/contributors/chrkj/OthelloEngine.svg?style=for-the-badge
[contributors-url]: https://github.com/chrkj/OthelloEngine/graphs/contributors

[forks-shield]: https://img.shields.io/github/forks/chrkj/OthelloEngine.svg?style=for-the-badge
[forks-url]: https://github.com/chrkj/OthelloEngine/network/members

[stars-shield]: https://img.shields.io/github/stars/chrkj/OthelloEngine.svg?style=for-the-badge
[stars-url]: https://github.com/chrkj/OthelloEngine/stargazers

[issues-shield]: https://img.shields.io/github/issues/chrkj/OthelloEngine.svg?style=for-the-badge
[issues-url]: https://github.com/chrkj/OthelloEngine/issues

[license-shield]: https://img.shields.io/github/license/chrkj/OthelloEngine.svg?style=for-the-badge&
[license-url]: https://github.com/chrkj/OthelloEngine/blob/master/LICENSE


[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://www.linkedin.com/in/christian-kjaer/
