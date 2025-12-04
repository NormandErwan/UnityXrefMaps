namespace UnityXrefMaps;

internal static class Constants
{
    /// <summary>
    /// Gets the default URL of the online API documentation of Unity.
    /// </summary>
    public const string DefaultUnityApiUrl = "https://docs.unity3d.com/{0}/Documentation/ScriptReference/";

    /// <summary>
    /// The default path of the Unity repository.
    /// </summary>
    public const string DefaultUnityRepositoryPath = "UnityCsReference";

    /// <summary>
    /// The default URL of the Unity repository.
    /// </summary>
    public const string DefaultUnityRepositoryUrl = "https://github.com/Unity-Technologies/UnityCsReference.git";

    /// <summary>
    /// The default branch of the Unity repository.
    /// </summary>
    public const string DefaultUnityRepositoryBranch = "master";

    // https://github.com/dotnet/docfx/blob/1c4e9ff4a2d236206eee04066847a98343c6a3f7/src/Docfx.Build/XRefMaps/XRefArchive.cs#L14
    /// <summary>
    /// The default xref map filename.
    /// </summary>
    public const string DefaultXrefMapFileName = "xrefmap.yml";

    /// <summary>
    /// The default path where to copy the xref maps.
    /// </summary>
    public const string DefaultXrefMapsPath = $"_site/{{0}}/{DefaultXrefMapFileName}";

    /// <summary>
    /// The default DocFX config file path.
    /// </summary>
    public const string DefaultDocFxConfigurationFilePath = "docfx.json";

    /// <summary>
    /// The default regular expression to check if it is a package.
    /// </summary>
    public const string DefaultPackageRegex = @"https://docs.unity3d.com/Packages/";
}
