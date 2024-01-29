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
    A simple othello engine
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
This is an implementation of the game Othello (also known as Reversi) strategy board game for two players, played on an 8 x 8 board.
I originally created the project to learn more about game AI's of which the game currently supports two.
Firstly the [MiniMax algorithm](https://en.wikipedia.org/wiki/Minimax) and secondly the [Monte Carlo Tree Search Algorithm](https://en.wikipedia.org/wiki/Monte_Carlo_tree_search). The board representation uses two ulong integers (128-bit) to store the current board state. This is done to reduce the amount of memory used by MCTS.
<p align="right">(<a href="#top">back to top</a>)</p>


### Built With

Frameworks/libraries used for the project:

* [Unity](https://unity.com/)

<!-- ROADMAP -->
## Roadmap

Optimization:
- ✅ Implement multithreading for MCTS
- ✅Implement iterative deepening for MiniMax

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
