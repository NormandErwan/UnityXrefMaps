# Unity API references for DocFX

> Automatically add clickable links to the Unity API on a DocFX documentation

This repository generates references of the Unity API to use with DocFX (the so-called
[cross reference maps](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#cross-reference-between-projects)).
It automatically add clickable links to the Unity API on your DocFX documentation.

## Usage

1. Make sure you have setup a DocFX documentation.
   You can follow the [DocFxForUnity](https://github.com/NormandErwan/DocFxForUnity) instructions otherwise.

2. Add these lines to your `docfx.json`:

    ```diff
     "build": {
         "xref": [
    +        "https://normanderwan.github.io/UnityXrefMaps/Unity/xrefmap.yml"
         ],
     }
    ```

3. Generate your documentation!

## Contribute

- To run this program:

    1. Install [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core) SDK.
    2. Clone this repository on your computer.
    2. Open a terminal on the cloned repository and run:

        ```
        dotnet run
        ```

- For any question or comment, please [open a new issue](https://github.com/NormandErwan/UnityXrefMaps/issues/new).

- If you'd like to contribute, please [fork the repository](https://github.com/NormandErwan/UnityXrefMaps/fork) and use a
feature branch. Pull requests are warmly welcome!

## Disclaimer

This repository is not sponsored by or affiliated with Unity Technologies or its affiliates.
“Unity” is a trademark or registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.