# Unity API references for DocFX [![Build Status](https://img.shields.io/github/actions/workflow/status/NormandErwan/UnityXrefMaps/ci.yml?branch=main)](https://github.com/NormandErwan/UnityXrefMaps/actions/workflows/ci.yml) [![NuGet Package](https://img.shields.io/nuget/v/UnityXrefMaps)](https://www.nuget.org/packages/UnityXrefMaps)

> Automatically add clickable links to the Unity API on a DocFX documentation

Generates references of the Unity API to use with DocFX (the
[cross reference maps](https://dotnet.github.io/docfx/docs/links-and-cross-references.html#cross-reference-to-net-basic-class-library)).
DocFX will set clickable all the references of the Unity API on your documentation.

## Basic usage

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

## Advanced usage

Install tool with: `dotnet tool install --global UnityXrefMaps`.

### Generate documentation for a specific version of the Unity editor

You need to provide at least:

- A `docfx.json` file. You can base it on the [docfx.json in the repository](UnityXrefMaps/docfx.json).
- The version of the Unity editor.
- Trim `UnityEngine` and `UnityEditor` because Unity omits these namespaces in its documentation links.

Example:

```bash
unityxrefmaps \
    --repositoryPath xref/Unity/Repository \
    --repositoryTags 6000.0.1f1 \
    --trimNamespaces UnityEditor \
    --trimNamespaces UnityEngine
```

### Generate documentation for a specific version of a Unity package

You need to provide at least:

- A `docfx.json` file. You can base it on the [docfx.json in the repository](UnityXrefMaps/docfx.json).
- The package version.
- The API URL, which will be different for each package.

Example:

```bash
unityxrefmaps \
    --repositoryUrl "https://github.com/needle-mirror/com.unity.inputsystem.git" \
    --repositoryTags '1.0.0' \
    --apiUrl "https://docs.unity3d.com/Packages/com.unity.inputsystem@{0}/api/"
```

### Testing that links are generated correctly

Providing an `xrefmap.yml` file will check each `href` element to see if it is a valid URL.

Example:

```bash
unityXrefMaps test \
    --xrefPath xrefmap.yml
```

## Contribute

- To run this program:

    1. Install [.NET 9.0](https://dotnet.microsoft.com/download/dotnet) SDK.
    2. Install [DocFX](https://www.nuget.org/packages/docfx).
    3. Clone this repository on your computer.
    4. Open a terminal on the cloned repository and run:

        ```sh
        dotnet run --project UnityXrefMaps/UnityXrefMaps.csproj
        ```

- For any question or comment, please [open a new issue](https://github.com/NormandErwan/UnityXrefMaps/issues/new).

- If you'd like to contribute, please [fork the repository](https://github.com/NormandErwan/UnityXrefMaps/fork) and use
a feature branch. Pull requests are warmly welcome!

## Disclaimer

This repository is not sponsored by or affiliated with Unity Technologies or its affiliates.
“Unity” is a trademark or registered trademark of Unity Technologies or its affiliates in the U.S. and elsewhere.
