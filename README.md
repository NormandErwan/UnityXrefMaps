# Unity API references for DocFX

> Automatically add clickable links to the Unity API on a DocFX documentation

Generates references of the Unity API to use with DocFX (the
[cross reference maps](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#cross-reference-between-projects)).
DocFX will set clickable all the references of the Unity API on your documentation.

## Usage

1. Make sure you have setup a DocFX documentation. You can follow the
   [DocFxForUnity](https://github.com/NormandErwan/DocFxForUnity) instructions otherwise.

2. Add this line to your `docfx.json`:

    - If you want to reference the latest stable version of Unity:

        ```diff
        "build": {
            "xref": [
        +        "https://normanderwan.github.io/UnityXrefMaps/xrefmap.yml"
            ],
        }
        ```

    - If you want to reference a specific version of Unity:

        ```diff
        "build": {
            "xref": [
        +        "https://normanderwan.github.io/UnityXrefMaps/<version>/xrefmap.yml"
            ],
        }
        ```

      where `<version>` is a Unity version in the form of `YYYY.x` (*e.g.* 2018.4, 2019.3, 2020.1).

   - If you prefer relying in a offline file:

        ```diff
        "build": {
            "xref": [
        +        "UnityXrefMap.yml"
            ],
        }
        ```

      where `UnityXrefMap.yml` has been downloaded from one of the link above and placed next to your `docfx.json`.

3. Generate your documentation!



## Contribute

- To run this program:

    1. Install Visual Studio 2022.
    2. Install [.NET 7.0](https://dotnet.microsoft.com/download/dotnet) SDK.
    3. Install [DocFX](https://github.com/dotnet/docfx/) `2.x`.
    4. Clone this repository on your computer.
    5. Open a terminal on the cloned repository and run:

        ```sh
        dotnet run --project UnityXrefMaps.csproj
        ```

- For any question or comment, please [open a new issue](https://github.com/NormandErwan/UnityXrefMaps/issues/new).

- If you'd like to contribute, please [fork the repository](https://github.com/NormandErwan/UnityXrefMaps/fork) and use
a feature branch. Pull requests are warmly welcome!

## Disclaimer

This repository is not sponsored by or affiliated with Unity Technologies or its affiliates.
“Unity” is a trademark or registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.
