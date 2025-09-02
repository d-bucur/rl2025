# RaygueLike Challenge 2025
![](/Resources//challenge_logo.png)

WIP project for the [r/roguelikedev 2025 complete tutorial](https://old.reddit.com/r/roguelikedev/wiki/python_tutorial_series)

Playable versions and devlog: https://sirdorius.itch.io/rayguelike-2025

## Demo
![](https://img.itch.zone/aW1nLzIzMDEyMjQ3LnBuZw==/original/us69f5.png)

[Screencast from 26-08-25 20:02:12.webm](https://github.com/user-attachments/assets/a435c675-6d40-40d2-91c6-ef19cfa5752f)

## Dev
### Desktop version
```
dotnet run --project RayLikeDesktop
```
### Web version
```
dotnet publish RayLikeWasm -c Debug
dotnet serve --mime .wasm=application/wasm --mime .js=text/javascript --mime .json=application/json --directory RayLikeWasm/bin/Debug/net8.0/browser-wasm/AppBundle
```

## Credits
- https://sethbb.itch.io/32rogues for the sprites
- https://kaylousberg.itch.io/block-bits for the 3D tiles
- https://github.com/friflo/Friflo.Engine.ECS for the ECS framework
- https://github.com/raysan5/raylib - for the cross platform graphics library
- https://github.com/Kiriller12/RaylibWasm for wasm project template
