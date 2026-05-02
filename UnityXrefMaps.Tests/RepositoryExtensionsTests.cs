using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        var signature = new Signature("test", "test@test.com", DateTimeOffset.UtcNow);
        File.WriteAllText(Path.Combine(_tempPath, "file.txt"), "content");
        _repository.Index.Add("file.txt");
        _repository.Index.Write();
        var commit = _repository.Commit("Initial commit", signature, signature);
        _repository.Tags.Add("6000.0.1f1", commit.Sha, signature, "Unity 6000.0.1f1 release");

        // Act
        IEnumerable<string> tags = _repository.GetTags();

        // Assert
        Assert.Contains("6000.0.1f1", tags);
    }

    [Fact]
    public void GetTags_WithLightweightTag_ReturnsTagName()
    {
        // Arrange: lightweight tag (tag.Target is the Commit directly, while-loop never iterates)
        var signature = new Signature("test", "test@test.com", DateTimeOffset.UtcNow);
        File.WriteAllText(Path.Combine(_tempPath, "file.txt"), "content");
        _repository.Index.Add("file.txt");
        _repository.Index.Write();
        var commit = _repository.Commit("Initial commit", signature, signature);
        _repository.Tags.Add("2023.1.0f1", commit);

        // Act
        IEnumerable<string> tags = _repository.GetTags();

        // Assert
        Assert.Contains("2023.1.0f1", tags);
    }

    [Fact]
    public void GetTags_WithNestedAnnotatedTag_ReturnsTagName()
    {
        // Arrange: outer annotated tag → inner TagAnnotation → Commit (while-loop iterates twice)
        var signature = new Signature("test", "test@test.com", DateTimeOffset.UtcNow);
        File.WriteAllText(Path.Combine(_tempPath, "file.txt"), "content");
        _repository.Index.Add("file.txt");
        _repository.Index.Write();
        var commit = _repository.Commit("Initial commit", signature, signature);
        var innerTag = _repository.Tags.Add("inner", commit.Sha, signature, "Inner annotated tag");
        _repository.Tags.Add("outer", innerTag.Target.Sha, signature, "Outer tag pointing to inner annotation");

        // Act
        IEnumerable<string> tags = _repository.GetTags();

        // Assert
        Assert.Contains("outer", tags);
    }

    [Fact]
    public void GetTags_WithTagNotPointingToCommit_IsStillReturned()
    {
        // Arrange: tag pointing to a blob (not a Commit); GetTaggedCommit returns null,
        // so the tag sorts last with DateTimeOffset.MinValue but still appears in output.
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("blob content"));
        var blob = _repository.ObjectDatabase.CreateBlob(stream);
        _repository.Tags.Add("blob-tag", blob);

        // Act
        IEnumerable<string> tags = _repository.GetTags();

        // Assert
        Assert.Contains("blob-tag", tags);
    }

    public void Dispose()
    {
        _repository.Dispose();
        try { Directory.Delete(_tempPath, recursive: true); } catch { }
    }
}
