using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp;

namespace UnityXrefMaps.Tests;

public sealed class RepositoryExtensionsTests : IDisposable
{
    private readonly string _tempPath;
    private readonly Repository _repository;

    public RepositoryExtensionsTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);
        Repository.Init(_tempPath);
        _repository = new Repository(_tempPath);
    }

    /// <summary>
    /// Annotated tags (created with a message) have a TagAnnotation as their Target, not a Commit.
    /// GetTags() was casting Target directly to Commit via (tag.Target as Commit)!, which returned
    /// null for annotated tags and then threw NullReferenceException on .Author.When.
    /// Unity's UnityCsReference repository uses annotated tags.
    /// </summary>
    [Fact]
    public void GetTags_WithAnnotatedTag_ReturnsTagName()
    {
        // Arrange: one commit + one annotated tag (tag.Target is TagAnnotation, not Commit)
        File.WriteAllText(Path.Combine(_tempPath, "file.txt"), "content");
        Commands.Stage(_repository, "*");
        var signature = new Signature("test", "test@test.com", DateTimeOffset.UtcNow);
        Commit commit = _repository.Commit("Initial commit", signature, signature);
        _repository.Tags.Add("6000.0.1f1", commit, signature, "Unity 6000.0.1f1 release");

        // Act
        IEnumerable<string> tags = _repository.GetTags();

        // Assert
        Assert.Contains("6000.0.1f1", tags);
    }

    public void Dispose()
    {
        _repository.Dispose();
        try { Directory.Delete(_tempPath, recursive: true); } catch { }
    }
}
