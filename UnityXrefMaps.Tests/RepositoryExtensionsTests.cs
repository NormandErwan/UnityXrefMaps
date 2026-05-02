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
        var (commit, signature) = CreateCommit();
        _repository.Tags.Add("6000.0.1f1", commit.Sha, signature, "Unity 6000.0.1f1 release");

        Assert.Contains("6000.0.1f1", _repository.GetTags());
    }

    [Fact]
    public void GetTags_WithLightweightTag_ReturnsTagName()
    {
        // tag.Target is the Commit directly; while-loop in GetTaggedCommit never iterates
        var (commit, _) = CreateCommit();
        _repository.Tags.Add("2023.1.0f1", commit);

        Assert.Contains("2023.1.0f1", _repository.GetTags());
    }

    [Fact]
    public void GetTags_WithNestedAnnotatedTag_ReturnsTagName()
    {
        // outer annotated tag → inner TagAnnotation → Commit; while-loop iterates twice
        var (commit, signature) = CreateCommit();
        var innerTag = _repository.Tags.Add("inner", commit.Sha, signature, "Inner annotated tag");
        _repository.Tags.Add("outer", innerTag.Target.Sha, signature, "Outer tag pointing to inner annotation");

        Assert.Contains("outer", _repository.GetTags());
    }

    [Fact]
    public void GetTags_WithTagNotPointingToCommit_IsStillReturned()
    {
        // GetTaggedCommit returns null for a blob target; tag sorts last but still appears in output
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("blob content"));
        var blob = _repository.ObjectDatabase.CreateBlob(stream);
        _repository.Tags.Add("blob-tag", blob);

        Assert.Contains("blob-tag", _repository.GetTags());
    }

    public void Dispose()
    {
        _repository.Dispose();
        try { Directory.Delete(_tempPath, recursive: true); } catch { }
    }

    private (Commit commit, Signature signature) CreateCommit()
    {
        var signature = new Signature("test", "test@test.com", DateTimeOffset.UtcNow);
        File.WriteAllText(Path.Combine(_tempPath, "file.txt"), "content");
        _repository.Index.Add("file.txt");
        _repository.Index.Write();
        return (_repository.Commit("Initial commit", signature, signature), signature);
    }
}
