#
![](/Resources//challenge_logo.png)

WIP project for the r/roguelikedev 2025 complete tutorial

https://old.reddit.com/r/roguelikedev/wiki/python_tutorial_series

# RayLikeWasm

.Net 8.0 webasssembly starter project using raylib-cs nuget.

I've followed [DotnetRayLikeWasm](https://github.com/stanoddly/DotnetRayLikeWasm) example project and some official Microsoft documentation.

> [!IMPORTANT]
> Please read the instructions below for building and publishing the project, as this may affect its functionality and cause unexpected errors.

## Setup

You must have .Net 8.0 installed before start.

Then install wasm toolset:

```
dotnet workload install wasm-tools
```

## Build

> [!WARNING]
> Do not use Visual Studio publication, it may cause some strange errors!

Just call this command from the root directory of the solution:
```
dotnet publish -c Release
```

## Run

You could use whatever web-server you want to serve published files.

OR

You could also use `dotnet serve` for this purpose:

If it's not installed, you need to install it with this command:
```
dotnet tool install --global dotnet-serve
```

And then just call this command to start web server for your build:
```
dotnet serve --mime .wasm=application/wasm --mime .js=text/javascript --mime .json=application/json --directory RayLikeWasm\bin\Release\net8.0\browser-wasm\AppBundle\
```

While server is running you can use publish command to update your files without any need to restart server.

## Notes

This project includes webassembly build of raylib native 5.5 (`raylib.a` file), because it is not included with raylib-cs nuget.

Raylib-cs may still have some webassembly compatibility issues that have been mentioned [here](https://github.com/stanoddly/DotnetRayLikeWasm/issues/11) and [here](https://github.com/stanoddly/DotnetRayLikeWasm/issues/4).

This project is not perfect, so I would welcome your suggestions and PR requests.

## Thanks

- to [Ray](https://github.com/raysan5) and all [raylib](https://github.com/raysan5/raylib) contributors for such a wonderful lib
- to [ChrisDill](https://github.com/ChrisDill) for [raylib C# bindings](https://github.com/ChrisDill/Raylib-cs)
- to [stanoddly](https://github.com/stanoddly) for dotnet webassembly [example project](https://github.com/stanoddly/DotnetRayLikeWasm)
