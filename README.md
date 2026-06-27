# MapGame

A technology sandbox and experimental project focused on developing a **Grand Strategy** game set in the 20th century. It is based on C#/WPF, intentionally bypassing pre-built game engines in favor of native framework capabilities.

The current phase focuses on the visual infrastructure, low-level rendering, and smooth map navigation.

---

## Key Features & Current State

* **Native WPF 3D Viewport:** Built entirely on top of WPF's native 3D graphics capabilities without relying on external engines (like Unity).
* **Dynamic Terrain Generation from Heightmaps:** Translates 2D grayscale heightmap textures into a 3D triangle mesh, calculating surface elevation dynamically.
* **Bitmap-Based Region Parsing:** Parses color-coded bitmap (BMP) files to segment the map into distinct areas.
* **Smooth Camera System:** Implements a custom camera controller handling translation and zooming. Movement utilizes custom acceleration physics that scale dynamically based on the camera's position.

---

## Architecture & Tech Stack

The architecture is designed from the ground up to isolate game logic from presentation rules.

* **Language:** C# (.NET modern features)
* **UI & Rendering:** Windows Presentation Foundation (WPF) for windows management, input handling, and native 3D rendering.
* **Design Pattern:** MVVM (Model-View-ViewModel), ensuring that core game state data (countries, province statistics, armies) remains separate from the Viewport3D presentation layer.

---

## Roadmap & Future Development

As the architecture matures, the project will expand into traditional Grand Strategy mechanics:

1. **Connecting ViewModels to Map Regions:** Binding the parsed bitmap provinces to logical ViewModels, allowing user interaction and real-time visual updates.
2. **Scripted Historical Database:** Implementing a text/script parser to load historical data, demographics, economies, and initial country borders for a 20th-century setup.
3. **Rendering & Mesh Optimization:** Optimizing the terrain mesh generation and memory allocation to efficiently handle large-scale world maps and textures.
4. **Game Loop & Tick System:** Introducing an asynchronous game loop to process background economic calculations, AI decision-making, and military movements smoothly without locking the UI.
